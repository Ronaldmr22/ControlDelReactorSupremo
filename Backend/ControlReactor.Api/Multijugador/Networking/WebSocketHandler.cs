using ControlReactor.Api.Multijugador.Networking;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ControlReactor.Api.Multijugador.Players;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Negocio.Servicios;



namespace ControlReactor.Api.Multijugador.Networking;


public sealed class WebSocketHandler
{
    private readonly ConnectionGroupManager _groupManager;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public WebSocketHandler(ConnectionGroupManager groupManager, IServiceScopeFactory scopeFactory)
    {
        _groupManager = groupManager;
        _scopeFactory = scopeFactory;
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
            case "action":
                await RelayActionAsync(connection);
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

        using var scope = _scopeFactory.CreateScope();

        var servicioPuntuacion = scope.ServiceProvider
            .GetRequiredService<ServicioPuntuacion>();

        int? nuevoPuntaje = await servicioPuntuacion.RegistrarMovimiento(
            connection.IdJugadorPartida.Value);

        if (!nuevoPuntaje.HasValue)
        {
            await SendErrorAsync(
                connection,
                "No se pudo registrar el movimiento.");
            return;
        }
        // Mensaje con el puntaje actualizado.
        var mensajePuntaje = new ServerMessage
        {
            Type = "score_updated",
            RoomCode = connection.RoomCode,
            PlayerId = connection.PlayerId,
            IdUsuario = connection.IdUsuario,
            IdJugadorPartida = connection.IdJugadorPartida.Value,
            Puntaje = nuevoPuntaje.Value
        };

        // Enviar el puntaje al jugador que se movió.
        await connection.SendAsync(mensajePuntaje);

        // Enviar el puntaje a los demás jugadores de la sala.
        foreach (var recipient in recipients)
        {
            await recipient.SendAsync(mensajePuntaje);
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

    private async Task RelayActionAsync(ClientConnection connection)
    {
        // Verificar que el jugador pertenezca a una partida.
        if (!connection.IdJugadorPartida.HasValue ||
            string.IsNullOrEmpty(connection.RoomCode))
        {
            await SendErrorAsync(
                connection,
                "El jugador no pertenece a una partida válida.");
            return;
        }

        // Obtener las conexiones de la sala.
        List<ClientConnection> conexiones =
            _groupManager.ObtenerConexionesSala(connection.RoomCode);

        // Confirmar que el emisor sigue registrado en esa sala.
        if (!conexiones.Any(c => ReferenceEquals(c, connection)))
        {
            await SendErrorAsync(
                connection,
                "La conexión no está registrada en la sala.");
            return;
        }

        // Crear el evento que recibirá el otro jugador.
        var mensajeAccion = new ServerMessage
        {
            Type = "player_action",
            RoomCode = connection.RoomCode,
            PlayerId = connection.PlayerId,
            PlayerName = connection.PlayerName
        };

        // Enviar la acción solamente a los otros jugadores.
        foreach (var destinatario in conexiones)
        {
            if (destinatario.PlayerId == connection.PlayerId)
                continue;

            await destinatario.SendAsync(mensajeAccion);
        }
    }

    private async Task RemoveConnectionAsync(
        ClientConnection connection)
    {
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
            catch (WebSocketException)
            {
                // Una conexion restante tambien puede haberse cerrado.
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
}
