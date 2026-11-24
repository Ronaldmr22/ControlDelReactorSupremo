using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ControlReactor.Tests;

// WebSocket en memoria: guarda los mensajes que el servidor envía al cliente
// para poder verificar qué recibió cada jugador sin abrir conexiones reales.
internal sealed class WebSocketFalso : WebSocket
{
    private readonly List<string> _enviados = new();
    private WebSocketState _estado = WebSocketState.Open;

    public IReadOnlyList<JsonElement> Mensajes
    {
        get
        {
            lock (_enviados)
                return _enviados.Select(j => JsonDocument.Parse(j).RootElement.Clone()).ToList();
        }
    }

    public IEnumerable<string> Tipos => Mensajes.Select(m => m.GetProperty("type").GetString() ?? "");

    public IEnumerable<JsonElement> DeTipo(string tipo) =>
        Mensajes.Where(m => m.GetProperty("type").GetString() == tipo);

    public string? UltimoError() =>
        DeTipo("error").Select(m => m.GetProperty("message").GetString()).LastOrDefault();

    public void Limpiar()
    {
        lock (_enviados) _enviados.Clear();
    }

    public override WebSocketCloseStatus? CloseStatus => null;
    public override string? CloseStatusDescription => null;
    public override WebSocketState State => _estado;
    public override string? SubProtocol => null;

    public override void Abort() => _estado = WebSocketState.Aborted;

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        _estado = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        => CloseAsync(closeStatus, statusDescription, cancellationToken);

    public override void Dispose() => _estado = WebSocketState.Closed;

    public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        => Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true));

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        lock (_enviados)
            _enviados.Add(Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count));
        return Task.CompletedTask;
    }
}
