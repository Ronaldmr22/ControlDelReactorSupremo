using System.Diagnostics;
using ControlReactor.Api.Multijugador.Networking;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ControlReactor.Api.Multijugador.Players;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Api.Multijugador.Configuration;


namespace ControlReactor.Api.Multijugador.Networking;


public sealed class WebSocketHandler
{
    private readonly ConnectionGroupManager _groupManager;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly ExtractionSessionManager _extractionManager;

    public WebSocketHandler(ConnectionGroupManager groupManager, IServiceScopeFactory scopeFactory, ExtractionSessionManager extractionManager)
    {
        _groupManager = groupManager;
        _scopeFactory = scopeFactory;
        _extractionManager = extractionManager;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(
                "Se requiere una conexion WebSocket.");
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();

        // Identificador temporal de esta conexion.
        // Posteriormente podemos vincularlo al ID real del jugador de la API.
        var connection = new ClientConnection(
            socket,
            Guid.NewGuid().ToString());

        try
        {
            await connection.SendAsync(new ServerMessage
            {
                Type = "connected",
                PlayerId = connection.PlayerId,
                Message = "Conexion WebSocket establecida."
            });

            await ReceiveMessagesAsync(context, connection);
        }
        catch (WebSocketException)
        {
            // El cliente puede desconectarse abruptamente.
        }
        finally
        {
            await RemoveConnectionAsync(connection);
        }
    }

    private async Task ReceiveMessagesAsync(
        HttpContext context,
        ClientConnection connection)
    {
        var buffer = new byte[4096];

        while (connection.Socket.State == WebSocketState.Open &&
               !context.RequestAborted.IsCancellationRequested)
        {
            using var messageStream = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await connection.Socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    context.RequestAborted);

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                if (result.MessageType != WebSocketMessageType.Text)
                {
                    await SendErrorAsync(
                        connection,
                        "Solo se admiten mensajes de texto JSON.");
                    return;
                }

                messageStream.Write(buffer, 0, result.Count);

                // Evita aceptar mensajes excesivamente grandes.
                if (messageStream.Length > 16 * 1024)
                {
                    await SendErrorAsync(
                        connection,
                        "El mensaje supera el tamaño permitido.");
                    return;
                }
            }
            while (!result.EndOfMessage);

            messageStream.Position = 0;

            ClientMessage? message;

            try
            {
                message = await JsonSerializer.DeserializeAsync<ClientMessage>(
                    messageStream,
                    JsonOptions,
                    context.RequestAborted);
            }
            catch (JsonException)
            {
                await SendErrorAsync(connection, "JSON no valido.");
                continue;
            }

            if (message is null)
            {
                await SendErrorAsync(connection, "Mensaje vacio.");
                continue;
            }

            await ProcessMessageAsync(connection, message);
        }
    }

    private async Task ProcessMessageAsync(
        ClientConnection connection,
        ClientMessage message)
    {
        switch (message.Type.Trim().ToLowerInvariant())
        {
            case "subscribe_room":
                await SubscribeToRoomAsync(connection, message);
                break;

            case "move":
                await RelayMovementAsync(connection, message);
                break;

            case "extract_start":
                await HandleExtractionStartAsync(connection, message);
                break;

            case "extract_stop":
                await HandleExtractionStopAsync(connection, message);
                break;

            default:
                await SendErrorAsync(
                    connection,
                    "Tipo de mensaje desconocido.");
                break;
        }
    }

    private async Task SubscribeToRoomAsync(
        ClientConnection connection,
        ClientMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.RoomCode))
        {
            await SendErrorAsync(
                connection,
                "Debes indicar el codigo de sala.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(message.PlayerName))
        {
            connection.PlayerName = message.PlayerName.Trim();
        }

        
        if (!message.IdUsuario.HasValue || message.IdUsuario.Value <= 0)
        {
            await SendErrorAsync(
                connection,
                "Debes indicar un usuario valido.");
            return;
        }

        string codigoSala = message.RoomCode.Trim().ToUpperInvariant();

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ControlReactorDbContext>();

        var partida = await db.Partidas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CodigoSala == codigoSala);

        if (partida == null)
        {
            await SendErrorAsync(connection, "La sala no existe.");
            return;
        }

        if (partida.Estado != "ESPERANDO" &&
            partida.Estado != "EN_CURSO")
        {
            await SendErrorAsync(
                connection,
                "La partida no permite conexiones.");
            return;
        }

        var participante = await db.JugadoresPartida
            .AsNoTracking()
            .FirstOrDefaultAsync(j =>
                j.IdPartida == partida.IdPartida &&
                j.IdUsuario == message.IdUsuario.Value &&
                j.EnSala);

        if (participante == null)
        {
            await SendErrorAsync(
                connection,
                "El usuario no pertenece a esta sala.");
            return;
        }

        // Guardamos la relacion con PostgreSQL.
        connection.IdUsuario = participante.IdUsuario;
        connection.IdJugadorPartida = participante.IdJugadorPartida;


        bool success = _groupManager.Subscribe(
            connection,
            message.RoomCode,
            out List<ClientConnection> existingConnections,
            out List<PlayerState> players,
            out string error);

        if (!success)
        {
            await SendErrorAsync(connection, error);
            return;
        }

        await connection.SendAsync(new ServerMessage
        {
            Type = "room_subscribed",
            RoomCode = connection.RoomCode,
            PlayerId = connection.PlayerId,
            PlayerName = connection.PlayerName,
        });

        // Informar al jugador nuevo quiénes ya estaban en la sala.
        foreach (var existing in existingConnections)
        {
            await connection.SendAsync(new ServerMessage
            {
                Type = "player_joined",
                RoomCode = connection.RoomCode,
                PlayerId = existing.PlayerId,
                PlayerName = existing.PlayerName,
                IdUsuario = existing.IdUsuario,
                X = existing.X,
                Y = existing.Y
            });
        }

        // Informar a los jugadores que ya estaban conectados.
        foreach (var existing in existingConnections)
        {
            await existing.SendAsync(new ServerMessage
            {
                Type = "player_joined",
                RoomCode = connection.RoomCode,
                PlayerId = connection.PlayerId,
                PlayerName = connection.PlayerName,
                IdUsuario = connection.IdUsuario,
                X = connection.X,
                Y = connection.Y
            });
        }
    }

    private async Task RelayMovementAsync(
        ClientConnection connection,
        ClientMessage message)
    {
        if (!message.X.HasValue || !message.Y.HasValue)
        {
            await SendErrorAsync(
                connection,
                "El movimiento debe incluir X e Y.");
            return;
        }

        float x = message.X.Value;
        float y = message.Y.Value;

        if (!float.IsFinite(x) || !float.IsFinite(y))
        {
            await SendErrorAsync(
                connection,
                "Las coordenadas no son validas.");
            return;
        }
        if (!connection.IdJugadorPartida.HasValue)
        {
            await SendErrorAsync(
                connection,
                "El jugador no tiene una participacion valida.");
            return;
        }

        bool success = _groupManager.TryUpdatePosition(
            connection,
            x,
            y,
            out PlayerState? player,
            out List<ClientConnection> recipients);

        if (!success || player is null)
        {
            // Ignorar movimientos repetidos o no aceptados.
            return;
        }

        var movementMessage = new ServerMessage
        {
            Type = "player_moved",
            RoomCode = connection.RoomCode,
            PlayerId = player.PlayerId,
            PlayerName = player.PlayerName,
            X = player.X,
            Y = player.Y
        };

        // Retransmitir el movimiento a los demas jugadores.
        foreach (var recipient in recipients)
        {
            await recipient.SendAsync(movementMessage);
        }
    }


    private async Task RemoveConnectionAsync(
        ClientConnection connection)
    {
        _extractionManager.StopAll(connection);

        bool removed = _groupManager.RemovePlayer(
            connection,
            out PlayerState? player,
            out List<ClientConnection> remainingPlayers,
            out string? roomCode);

        if (!removed || player is null)
            return;

        var leaveMessage = new ServerMessage
        {
            Type = "player_left",
            RoomCode = roomCode,
            PlayerId = player.PlayerId,
            PlayerName = player.PlayerName
        };

        foreach (var recipient in remainingPlayers)
        {
            try
            {
                await recipient.SendAsync(leaveMessage);
            }
            catch (WebSocketException ex)
            {
                Debug.WriteLine($"Error al notificar la desconexión del jugador: {ex.Message}");
            }
        }
    }


    private static Task SendErrorAsync(
        ClientConnection connection,
        string message)
    {
        return connection.SendAsync(new ServerMessage
        {
            Type = "error",
            Message = message
        });
    }


    private async Task HandleExtractionStartAsync(
        ClientConnection connection,
        ClientMessage message)
    {
        if (message.ReactorId != "central" ||
            !Guid.TryParseExact(message.ExtractionId, "N", out _))
        {
            await SendErrorAsync(
                connection, "Solicitud de extracción inválida.");
            return;
        }

        if (connection.RoomCode is not string roomCode ||
            connection.IdUsuario is not int userId ||
            connection.IdJugadorPartida is not int playerMatchId)
        {
            await SendErrorAsync(
                connection, "El jugador no está identificado.");
            return;
        }

        var roomConnections = _groupManager.ObtenerConexionesSala(roomCode);

        if (!roomConnections.Any(c => ReferenceEquals(c, connection)))
        {
            await SendErrorAsync(
                connection, "El jugador no está conectado a la sala.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ControlReactorDbContext>();

        var match = await db.Partidas
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CodigoSala == roomCode);

        if (match == null || match.Estado != "EN_CURSO")
        {
            await SendErrorAsync(
                connection, "La partida no está en curso.");
            return;
        }

        bool isParticipant = await db.JugadoresPartida
            .AsNoTracking()
            .AnyAsync(j =>
                j.IdJugadorPartida == playerMatchId &&
                j.IdUsuario == userId &&
                j.IdPartida == match.IdPartida &&
                j.EnSala);

        if (!isParticipant)
        {
            await SendErrorAsync(
                connection, "El jugador no pertenece a la partida.");
            return;
        }

        if (!ReactorConfiguration.TryGet(
            match.IdEscenario, out ReactorData reactor))
        {
            await SendErrorAsync(
                connection, "No existe un reactor configurado para este escenario.");
            return;
        }

        if (!connection.HasReceivedPosition)
        {
            await SendErrorAsync(
                connection, "Todavía no se conoce tu posición.");
            return;
        }

        float deltaX = connection.X - reactor.X;
        float deltaY = connection.Y - reactor.Y;

        float distanceSquared =
            deltaX * deltaX +
            deltaY * deltaY;

        float radiusSquared = reactor.Radius * reactor.Radius;

        if (distanceSquared > radiusSquared)
        {
            await SendErrorAsync(
                connection, "Estás demasiado lejos del reactor.");
            return;
        }


        var result = _extractionManager.TryStart(
            connection,
            message.ExtractionId!
        );

        if (result == ExtractionStartResult.Duplicate)
        {
            await SendErrorAsync(
                connection, "Solicitud de extracción duplicada.");
            return;
        }

        if (result == ExtractionStartResult.AlreadyActive)
        {
            await SendErrorAsync(
                connection, "Ya existe una extracción activa.");
            return;
        }

        await connection.SendAsync(new ServerMessage
        {
            Type = "extract_started",
            Message = "Inicio de extracción registrado."
        });
    }


    private async Task HandleExtractionStopAsync(
        ClientConnection connection,
        ClientMessage message)
    {
        if (connection.RoomCode == null ||
            connection.IdJugadorPartida == null)
        {
            await SendErrorAsync(
                connection, "El jugador no está identificado.");
            return;
        }

        if (message.ReactorId != "central" ||
            !Guid.TryParseExact(message.ExtractionId, "N", out _))
        {
            await SendErrorAsync(
                connection, "Identificador de extracción inválido.");
            return;
        }


        bool stopped = _extractionManager.TryStop(
            connection,
            message.ExtractionId!
        );

        if (!stopped)
        {
            await SendErrorAsync(
                connection,
                "No existe una extracción activa con ese identificador.");
            return;
        }

        await connection.SendAsync(new ServerMessage
        {
            Type = "extract_stopped",
            Message = "Extracción detenida."
        });

    }
}
