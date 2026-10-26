using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ControlReactor.Api.Multijugador.Players;
using ControlReactor.Api.Multijugador.Protocol;

namespace ControlReactor.Api.Multijugador.Networking;

public sealed class ClientConnection
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public WebSocket Socket { get; }

    public string PlayerId { get; }
    // Identificadores reales obtenidos después de validar al usuario.
    public int? IdUsuario { get; set; }

    public int? IdJugadorPartida { get; set; }

    public string PlayerName { get; set; } = "Jugador";

    public string? RoomCode { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public ClientConnection(WebSocket socket, string playerId)
    {
        Socket = socket;
        PlayerId = playerId;
    }

    public PlayerState ToPlayerState()
    {
        return new PlayerState
        {
            PlayerId = PlayerId,
            PlayerName = PlayerName,
            X = X,
            Y = Y
        };
    }

    public async Task SendAsync(ServerMessage message)
    {
        await _sendLock.WaitAsync();

        try
        {
            if (Socket.State != WebSocketState.Open)
                return;

            string json = JsonSerializer.Serialize(
                message,
                JsonOptions
            );

            byte[] bytes = Encoding.UTF8.GetBytes(json);

            await Socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
        finally
        {
            _sendLock.Release();
        }
    }
}