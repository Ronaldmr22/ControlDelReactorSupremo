using ControlReactor.Api.Multijugador.Players;
namespace ControlReactor.Api.Multijugador.Networking;

public sealed class ConnectionGroupManager
{
    private readonly object _sync = new();

    // Grupos temporales de conexiones WebSocket.
    private readonly Dictionary<string, ConnectionGroup> _groups =
        new(StringComparer.OrdinalIgnoreCase);

    public bool Subscribe(
        ClientConnection connection,
        string roomCode,
        out List<ClientConnection> existingConnections,
        out List<PlayerState> players,
        out string error)
    {
        lock (_sync)
        {
            existingConnections = new();
            players = new();
            error = string.Empty;

            string code = roomCode.Trim().ToUpperInvariant();

            if (code.Length == 0 || code.Length > 50)
            {
                error = "El codigo de sala no es valido.";
                return false;
            }

            if (connection.RoomCode is not null)
            {
                error = "Esta conexion ya esta suscrita a una sala.";
                return false;
            }

            if (!_groups.TryGetValue(code, out var group))
            {
                group = new ConnectionGroup(code);
                _groups.Add(code, group);
            }

            existingConnections = group.Connections.Values.ToList();

            // Comprobar que la conexion tenga un usuario validado.
            if (!connection.IdUsuario.HasValue ||
                !connection.IdJugadorPartida.HasValue)
            {
                error = "El jugador no esta identificado.";
                return false;
            }

            // Evitar que el mismo usuario ocupe dos conexiones.
            if (group.Connections.Values.Any(player =>
                player.IdUsuario == connection.IdUsuario))
            {
                error = "Este usuario ya esta conectado a la sala.";
                return false;
            }

            // Maximo 3 jugadores conectados.
            if (group.Connections.Count >= 3)
            {
                error = "La sala ya tiene 3 jugadores conectados.";
                return false;
            }

            if (group.Connections.ContainsKey(connection.PlayerId))
            {
                error = "Esta conexion ya esta registrada en el grupo.";
                return false;
            }

            group.Connections.Add(connection.PlayerId, connection);
            connection.RoomCode = code;

            players = group.Connections.Values
                .Select(player => player.ToPlayerState())
                .ToList();

            return true;
        }
    }

    public bool TryUpdatePosition(
        ClientConnection connection,
        float x,
        float y,
        out PlayerState? updatedPlayer,
        out List<ClientConnection> recipients)
    {
        lock (_sync)
        {
            updatedPlayer = null;
            recipients = new();

            if (connection.RoomCode is null ||
                !_groups.TryGetValue(connection.RoomCode, out var group) ||
                !group.Connections.ContainsKey(connection.PlayerId))
            {
                return false;
            }

            // Impedir movimientos mientras el jugador esté congelado.
            if (connection.EstaCongelado())
            {
                return false;
            }


            // Comprobar que la posicion realmente cambió.
            if (connection.X == x && connection.Y == y)
            {
                connection.UltimaActualizacionPosicion = DateTime.UtcNow;
                return false;
            }

            connection.X = x;
            connection.Y = y;
            connection.UltimaActualizacionPosicion = DateTime.UtcNow;
            Console.WriteLine(
                $"POSICION RECIBIDA BACKEND: " +
                $"X={connection.X:F2}, Y={connection.Y:F2}"
            );

            updatedPlayer = connection.ToPlayerState();

            recipients = group.Connections.Values
                .Where(player => player.PlayerId != connection.PlayerId)
                .ToList();

            return true;
        }
    }

    public bool RemovePlayer(
        ClientConnection connection,
        out PlayerState? removedPlayer,
        out List<ClientConnection> remainingPlayers,
        out string? removedRoomCode)
    {
        lock (_sync)
        {
            removedPlayer = null;
            remainingPlayers = new();
            removedRoomCode = connection.RoomCode;

            if (removedRoomCode is null ||
                !_groups.TryGetValue(removedRoomCode, out var group))
            {
                connection.RoomCode = null;
                return false;
            }

            if (!group.Connections.Remove(
                    connection.PlayerId, out var removedConnection))
            {
                connection.RoomCode = null;
                return false;
            }

            removedPlayer = removedConnection.ToPlayerState();
            connection.RoomCode = null;

            remainingPlayers = group.Connections.Values.ToList();

            if (group.Connections.Count == 0)
            {
                _groups.Remove(removedRoomCode);
            }

            return true;
        }
    }
    public List<ClientConnection> ObtenerJugadoresDisponibles(
        string codigoSala)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(codigoSala))
            {
                return new List<ClientConnection>();
            }

            string codigo = codigoSala.Trim().ToUpperInvariant();

            if (!_groups.TryGetValue(codigo, out var grupo))
            {
                return new List<ClientConnection>();
            }

            Console.WriteLine(
                $"Sala {codigo}: conexiones registradas = {grupo.Connections.Count}"
            );

            foreach (var jugador in grupo.Connections.Values)
            {
                Console.WriteLine(
                    $"Jugador {jugador.PlayerId} | " +
                    $"IdJugadorPartida: {jugador.IdJugadorPartida} | " +
                    $"Congelado: {jugador.EstaCongelado()}"
                );
            }

            DateTime ahora = DateTime.UtcNow;

            return grupo.Connections.Values
                .Where(jugador =>
                    jugador.IdJugadorPartida.HasValue &&
                    !jugador.EstaCongelado() &&
                    (ahora - jugador.UltimaActualizacionPosicion)
                        .TotalSeconds <= 1.0)
                .ToList();

            
        }
    }

    public bool CongelarJugador(
        string codigoSala,
        string playerId)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(codigoSala))
            {
                return false;
            }

            string codigo = codigoSala.Trim().ToUpperInvariant();

            if (!_groups.TryGetValue(codigo, out var grupo))
            {
                return false;
            }

            if (!grupo.Connections.TryGetValue(
                playerId, out var jugador))
            {
                return false;
            }

            if (jugador.EstaCongelado())
            {
                return false;
            }

            jugador.CongeladoHasta = DateTime.UtcNow.AddSeconds(2);

            return true;
        }
    }
    
    public List<ClientConnection> ObtenerConexionesSala(string roomCode)
    {
        lock (_sync)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                return new List<ClientConnection>();
            }

            string code = roomCode.Trim().ToUpperInvariant();

            if (!_groups.TryGetValue(code, out var group))
            {
                return new List<ClientConnection>();
            }

            return group.Connections.Values.ToList();
        }
    }

    public bool ObtenerPosicionJugador(
        string codigoSala,
        string playerId,
        out float x,
        out float y)
    {
        lock (_sync)
        {
            x = 0;
            y = 0;

            if (string.IsNullOrWhiteSpace(codigoSala))
                return false;

            string codigo = codigoSala.Trim().ToUpperInvariant();

            if (!_groups.TryGetValue(codigo, out var grupo))
                return false;

            if (!grupo.Connections.TryGetValue(
                playerId, out var jugador))
                return false;

            if (jugador.EstaCongelado())
                return false;

            // No aceptar posiciones demasiado antiguas.
            if ((DateTime.UtcNow -
                jugador.UltimaActualizacionPosicion).TotalSeconds > 1.0)
                return false;

            x = jugador.X;
            y = jugador.Y;

            return true;
        }
    }


}
