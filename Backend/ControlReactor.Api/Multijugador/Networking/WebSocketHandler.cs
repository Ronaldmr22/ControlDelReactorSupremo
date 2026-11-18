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
using ControlReactor.Negocio.Guardianes;
using ControlReactor.Api.Multijugador.Events;
using System.Data;
using Npgsql;


namespace ControlReactor.Api.Multijugador.Networking;


public sealed class WebSocketHandler
{
    private readonly ConnectionGroupManager _groupManager;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

private readonly ExtractionSessionManager _extractionManager;
private readonly EnergyBonusManager _energyBonuses;
private readonly GuardianSurgeManager _guardianSurges;
private readonly ReactorOverloadManager _overloads;
private readonly CreativeEventsManager _creativeEvents;

public WebSocketHandler(
        ConnectionGroupManager groupManager,
        IServiceScopeFactory scopeFactory,
        ExtractionSessionManager extractionManager,
        EnergyBonusManager energyBonuses,
        GuardianSurgeManager guardianSurges,
        ReactorOverloadManager overloads,
        CreativeEventsManager creativeEvents)
    {
        _groupManager = groupManager;
        _scopeFactory = scopeFactory;
        _extractionManager = extractionManager;
        _energyBonuses = energyBonuses;
        _guardianSurges = guardianSurges;
        _overloads = overloads;
        _creativeEvents = creativeEvents;
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
            case "energy_bonus_collect":
                await CollectEnergyBonusAsync(connection, message);
                break;
            case "configure_guardians":
                await ConfigureGuardiansAsync(connection, message);
                break;
            default:
                await SendErrorAsync(
                    connection,
                    "Tipo de mensaje desconocido.");
                break;
        }
    }
    private async Task ConfigureGuardiansAsync(
        ClientConnection connection,
        ClientMessage message)
    {
        if (string.IsNullOrWhiteSpace(connection.RoomCode))
        {
            await SendErrorAsync(
                connection,
                "Debes pertenecer a una sala para configurar guardianes.");

            return;
        }

        if (message.PosicionesGuardianes == null ||
            message.PosicionesGuardianes.Count != 4)
        {
            await SendErrorAsync(
                connection,
                "Se necesitan exactamente cuatro posiciones.");

            return;
        }

        // Pendiente: validar el escenario y la autorización.
        // Todavía no creamos los guardianes.
        await SendErrorAsync(
            connection,
            "La configuración de guardianes aún no está habilitada.");
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
            codigoSala,
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

        await SendRoomStateAsync(
        connection,
        partida.IdPartida,
        partida.EnergiaRestante);

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

    private async Task CollectEnergyBonusAsync(
        ClientConnection connection,
        ClientMessage message)
    {
        if (!Guid.TryParseExact(message.BonusId, "N", out _))
        {
            await SendErrorAsync(connection, "Identificador de cápsula inválido.");
            return;
        }

        if (connection.RoomCode is not string roomCode ||
            connection.IdUsuario is not int userId ||
            connection.IdJugadorPartida is not int playerMatchId ||
            !_groupManager.ObtenerPosicionJugador(roomCode, connection.PlayerId,
                out float x, out float y))
        {
            await SendErrorAsync(connection, "No se pudo validar la posición del jugador.");
            return;
        }

        if (!_energyBonuses.TryReserve(roomCode, message.BonusId!, x, y,
            out EnergyBonusState? bonus) || bonus == null)
        {
            await SendErrorAsync(connection, "La cápsula ya no está disponible o estás lejos.");
            return;
        }

        bool awarded = false;

        try
        {
            // Serializable evita perder puntos cuando coincide con extracción.
            for (int attempt = 0; attempt < 3; attempt++)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider
                    .GetRequiredService<ControlReactorDbContext>();

                try
                {
                    await using var transaction = await db.Database
                        .BeginTransactionAsync(IsolationLevel.Serializable);

                    var match = await db.Partidas
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.IdPartida == bonus.MatchId);

                    var player = await db.JugadoresPartida.FirstOrDefaultAsync(j =>
                        j.IdJugadorPartida == playerMatchId &&
                        j.IdUsuario == userId &&
                        j.IdPartida == bonus.MatchId &&
                        j.EnSala);

                    if (match == null || match.Estado != "EN_CURSO" ||
                        match.CodigoSala != roomCode || player == null)
                    {
                        await SendErrorAsync(connection,
                            "La partida o el participante ya no están activos.");
                        return;
                    }

                    player.Puntaje += EnergyBonusManager.BonusPoints;
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    awarded = true;
                    _energyBonuses.Complete(bonus);

                    var scoreUpdated = new ServerMessage
                    {
                        Type = "score_updated",
                        RoomCode = roomCode,
                        IdUsuario = player.IdUsuario,
                        IdJugadorPartida = player.IdJugadorPartida,
                        Puntaje = player.Puntaje
                    };

                    var collected = new ServerMessage
                    {
                        Type = "energy_bonus_collected",
                        RoomCode = roomCode,
                        BonusId = bonus.BonusId,
                        IdUsuario = player.IdUsuario,
                        Puntaje = player.Puntaje,
                        Message = "Cápsula recogida: +10 puntos."
                    };

                    foreach (var recipient in _groupManager.ObtenerConexionesSala(roomCode))
                    {
                        try
                        {
                            await recipient.SendAsync(scoreUpdated);
                            await recipient.SendAsync(collected);
                        }
                        catch (WebSocketException)
                        {
                            // Un cliente desconectado no impide actualizar a los demás.
                        }
                    }

                    return;
                }
                catch (Exception ex) when (attempt < 2 &&
                    (ex is PostgresException { SqlState: "40001" } ||
                     ex.InnerException is PostgresException { SqlState: "40001" }))
                {
                    await Task.Delay(50 * (attempt + 1));
                }
            }

            await SendErrorAsync(connection, "No se pudo registrar la cápsula. Intentá nuevamente.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al registrar la cápsula de energía: {ex.Message}");

            if (!awarded)
                await SendErrorAsync(connection, "No se pudo registrar la cápsula. Intentá nuevamente.");
        }
        finally
        {
            if (!awarded)
                _energyBonuses.Release(bonus);
        }
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


        if (match.EnergiaRestante <= 0 ||
            match.ReactorRechargeReadyAtUtc != null)
        {
            await SendErrorAsync(
                connection,
                "El reactor está recargando. Intentá nuevamente en unos segundos.");
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

        if (!float.IsFinite(connection.X) ||
            !float.IsFinite(connection.Y))
        {
            await SendErrorAsync(
                connection, "La posición del jugador no es válida.");
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


        // Impide iniciar una extracción durante una sobrecarga. Este candado
        // también lo usa el worker que activa el evento.
        ExtractionStartResult result;
        bool overloaded;

        using (await _overloads.EnterRoomGateAsync(roomCode, CancellationToken.None))
        {
            overloaded = _overloads.IsOverloaded(roomCode);
            result = overloaded
                ? ExtractionStartResult.Duplicate
                : _extractionManager.TryStart(connection, message.ExtractionId!);
        }

        if (overloaded)
        {
            await SendErrorAsync(connection,
                "El reactor está en sobrecarga. Esperá a que termine el evento.");
            return;
        }

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


    private async Task SendRoomStateAsync(
        ClientConnection connection,
        int matchId,
        int remainingEnergy)
    {
        await connection.SendAsync(new ServerMessage
        {
            Type = "reactor_state",
            RoomCode = connection.RoomCode,
            EnergiaRestante = remainingEnergy
        });

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ControlReactorDbContext>();

        var scores = await db.JugadoresPartida
            .AsNoTracking()
            .Where(p => p.IdPartida == matchId && p.EnSala)
            .Select(p => new
            {
                p.IdJugadorPartida,
                p.IdUsuario,
                p.Puntaje
            })
            .ToListAsync();

        foreach (var player in scores)
        {
            await connection.SendAsync(new ServerMessage
            {
                Type = "score_updated",
                RoomCode = connection.RoomCode,
                IdJugadorPartida = player.IdJugadorPartida,
                IdUsuario = player.IdUsuario,
                Puntaje = player.Puntaje
            });
        }

        // Reconstruir la pastilla para un jugador que se conectó tarde.
        if (connection.RoomCode is string roomCode)
        {
            var bonus = _energyBonuses.GetActiveForRoom(roomCode);
            if (bonus != null)
            {
                int remaining = Math.Max(0, (int)Math.Ceiling(
                    (bonus.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds));

                if (remaining > 0)
                {
                    await connection.SendAsync(new ServerMessage
                    {
                        Type = "energy_bonus_spawned",
                        RoomCode = roomCode,
                        BonusId = bonus.BonusId,
                        X = bonus.X,
                        Y = bonus.Y,
                        BonusDuration = remaining,
                        Message = "¡Hay una cápsula de energía adicional!"
                    });
                }
            }

            // Una conexión tardía debe ver el mismo evento que los demás.
            var surge = _guardianSurges.GetActiveForRoom(roomCode);
            if (surge != null)
            {
                int remaining = Math.Max(0, (int)Math.Ceiling(
                    (surge.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds));

                if (remaining > 0)
                {
                    await connection.SendAsync(new ServerMessage
                    {
                        Type = "guardian_surge_started",
                        RoomCode = roomCode,
                        GuardianSurgeDuration = remaining,
                        Message = "¡Hay 5 guardianes adicionales en la arena!"
                    });
                }
            }

            // Resynchronizar el reactor si el jugador se une durante el evento.
            var overload = _overloads.GetActiveForRoom(roomCode);
            if (overload != null)
            {
                int remaining = Math.Max(0, (int)Math.Ceiling(
                    (overload.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds));

                if (remaining > 0)
                {
                    await connection.SendAsync(new ServerMessage
                    {
                        Type = "reactor_overload_started",
                        RoomCode = roomCode,
                        OverloadDuration = remaining,
                        Message = "El reactor está en sobrecarga. No entrega puntos."
                    });
                }
            }

            // CA-22.4: quien entra tarde también ve las arenas o el x2 activo.
            var creative = _creativeEvents.GetActiveForRoom(roomCode);
            if (creative != null)
            {
                int remaining = Math.Max(0, (int)Math.Ceiling(
                    (creative.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds));

                if (remaining > 0)
                {
                    await connection.SendAsync(new ServerMessage
                    {
                        Type = creative.Kind == CreativeEventKind.Sand
                            ? "creative_sand_started" : "reactor_double_started",
                        RoomCode = roomCode,
                        CreativeDuration = remaining,
                        SandZones = creative.Kind == CreativeEventKind.Sand
                            ? creative.SandZones.ToList() : null,
                        Message = "Evento creativo CA-22.4 activo."
                    });
                }
            }
        }
    }

}
