using BattleArena.Server.Networking;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BattleArena.Server.Players;
using BattleArena.Server.Protocol;

namespace BattleArena.Server.Networking;

public sealed class WebSocketHandler
{
    private readonly ConnectionGroupManager _groupManager;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public WebSocketHandler(ConnectionGroupManager groupManager)
    {
        _groupManager = groupManager;
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

        // Informar a los jugadores que ya estaban conectados.
        foreach (var existing in existingConnections)
        {
            await existing.SendAsync(new ServerMessage
            {
                Type = "player_joined",
                RoomCode = connection.RoomCode,
                PlayerId = connection.PlayerId,
                PlayerName = connection.PlayerName,
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

        bool success = _groupManager.TryUpdatePosition(
            connection,
            x,
            y,
            out PlayerState? player,
            out List<ClientConnection> recipients);

        if (!success || player is null)
        {
            await SendErrorAsync(
                connection,
                "Primero debes suscribirte a una sala.");
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
