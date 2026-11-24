using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ControlReactor.Api.Multijugador.Players;
using ControlReactor.Api.Multijugador.Protocol;
using System.Diagnostics;

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
    public DateTime? CongeladoHasta { get; set; }
    public DateTime UltimaActualizacionPosicion { get; set; } = DateTime.MinValue;

    public bool HasReceivedPosition { get; set; }

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
        var reloj = Stopwatch.StartNew();

        await _sendLock.WaitAsync();

        long esperaLock = reloj.ElapsedMilliseconds;

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

            long tiempoTotal = reloj.ElapsedMilliseconds;

            // Registrar únicamente los mensajes importantes
            // o los envíos que tardan demasiado.
            if (message.Type == "player_frozen" ||
                tiempoTotal > 200)
            {
                Console.WriteLine(
                    $"ENVIO WEBSOCKET | " +
                    $"Tipo: {message.Type} | " +
                    $"Espera lock: {esperaLock} ms | " +
                    $"Tiempo total: {tiempoTotal} ms"
                );
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }
    public bool EstaCongelado()
    {
        return CongeladoHasta.HasValue && DateTime.UtcNow < CongeladoHasta.Value;
    }
}