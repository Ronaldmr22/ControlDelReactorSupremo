using ControlReactor.Api.Multijugador.Networking;

namespace ControlReactor.Api.Multijugador.Networking;

public sealed class ConnectionGroup
{
    public string Code { get; }

    public Dictionary<string, ClientConnection> Connections { get; } =
        new(StringComparer.Ordinal);

    public ConnectionGroup(string code)
    {
        Code = code;
    }
}