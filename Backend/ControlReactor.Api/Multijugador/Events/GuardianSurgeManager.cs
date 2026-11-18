using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.ServicioSegundoPlano;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Guardianes;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Api.Multijugador.Events;

public sealed record GuardianSurgeState(
    int MatchId,
    string RoomCode,
    DateTime ExpiresAtUtc,
    IReadOnlyList<int> GuardianIds);

public sealed record GuardianSurgeChange(bool Started, GuardianSurgeState State);

// CA-22.2: una sola oleada de cinco guardianes por partida, decidida en el servidor.
public sealed class GuardianSurgeManager
{
    public const int ExtraGuardians = 5;
    public const int DurationSeconds = 10;

    // Momento máximo de activación: 20 s después de iniciar la partida.
    // En partidas por tiempo cortas, se activa al alcanzar el 20 % del límite.
    public const int ActivationDelaySeconds = 20;

    private const float MapMargin = 1f;
    private const float MinimumSeparation = 1.7f;

    private readonly object _sync = new();
    private readonly Dictionary<int, GuardianSurgeState> _active = new();
    private readonly HashSet<int> _activated = new();
    private readonly Dictionary<int, List<(float X, float Y)>> _possiblePositions = new();

    public GuardianSurgeManager(string mapsDirectory)
    {
        for (int sceneId = 1; sceneId <= 3; sceneId++)
        {
            var map = CargadorColisionesMapa.Cargar(
                Path.Combine(mapsDirectory, $"ColisionesMapa{sceneId}.json"));

            if (map.IdEscenario != sceneId)
                throw new InvalidDataException($"Mapa de colisiones incorrecto: {sceneId}.");

            var vertices = map.Obstaculos.SelectMany(o => o.Vertices).ToList();
            if (vertices.Count == 0)
                throw new InvalidDataException($"Mapa {sceneId} sin límites de colisión.");

            float minX = vertices.Min(v => v.X) + MapMargin;
            float maxX = vertices.Max(v => v.X) - MapMargin;
            float minY = vertices.Min(v => v.Y) + MapMargin;
            float maxY = vertices.Max(v => v.Y) - MapMargin;

            var candidates = new List<(float X, float Y)>();

            // Misma granularidad de muestreo que el evento de energía.
            for (float x = minX; x <= maxX; x += 0.45f)
            for (float y = minY; y <= maxY; y += 0.45f)
            {
                if (DetectorColisionesMapa.CuerpoEnObstaculo(x, y, map))
                    continue;

                if (ZonasPuertasArena.ObtenerPorEscenario(sceneId)
                    .Any(door => door.Contiene(x, y, 0.8f, 0.8f)))
                    continue;

                if (ReactorConfiguration.TryGet(sceneId, out var reactor))
                {
                    float dx = x - reactor.X;
                    float dy = y - reactor.Y;
                    float minimum = reactor.Radius + 1.0f;

                    if (dx * dx + dy * dy < minimum * minimum)
                        continue;
                }

                candidates.Add((x, y));
            }

            if (candidates.Count < ExtraGuardians)
                throw new InvalidDataException(
                    $"Mapa {sceneId} sin espacio libre para cinco guardianes extra.");

            _possiblePositions.Add(sceneId, candidates);
        }
    }

    public GuardianSurgeChange? Advance(
        Partida match,
        AdministradorGuardianes administrator,
        IReadOnlyList<ClientConnection> connections,
        DateTime nowUtc)
    {
        if (match.Estado != "EN_CURSO" || string.IsNullOrWhiteSpace(match.CodigoSala))
            return null;

        lock (_sync)
        {
            if (_active.TryGetValue(match.IdPartida, out var current))
            {
                if (nowUtc < current.ExpiresAtUtc)
                    return null;

                administrator.EliminarGuardianesTemporales(current.GuardianIds);
                _active.Remove(match.IdPartida);
                return new GuardianSurgeChange(false, current);
            }

            // Una activación por partida; nunca vuelven a crearse tras expirar.
            if (_activated.Contains(match.IdPartida) ||
                nowUtc < GetScheduledTime(match) ||
                !_possiblePositions.TryGetValue(match.IdEscenario, out var candidates))
                return null;

            // No iniciar la oleada si la partida por tiempo ya está terminando.
            if (match.TipoFinalizacion == "TIEMPO" &&
                match.LimiteTiempo.GetValueOrDefault() > 0 &&
                nowUtc >= match.FechaInicio.AddSeconds(match.LimiteTiempo!.Value))
                return null;

            var positions = ChoosePositions(candidates, administrator, connections);
            if (positions.Count != ExtraGuardians)
                return null;

            var ids = administrator.AgregarGuardianesTemporales(positions);
            var state = new GuardianSurgeState(
                match.IdPartida,
                match.CodigoSala,
                nowUtc.AddSeconds(DurationSeconds),
                ids);

            _active.Add(match.IdPartida, state);
            _activated.Add(match.IdPartida);
            return new GuardianSurgeChange(true, state);
        }
    }

    // Permite avisar también a un jugador que entra durante la oleada.
    public GuardianSurgeState? GetActiveForRoom(string roomCode)
    {
        lock (_sync)
        {
            return _active.Values.FirstOrDefault(s =>
                s.RoomCode.Equals(roomCode, StringComparison.OrdinalIgnoreCase) &&
                s.ExpiresAtUtc > DateTime.UtcNow);
        }
    }

    // CA-22.8: cuando termina la partida, se retiran sus guardianes.
    public List<GuardianSurgeState> RemoveFinishedMatches(
        HashSet<int> activeMatchIds,
        AdministradorGuardianesPorSala administrators)
    {
        lock (_sync)
        {
            var finished = _active.Values
                .Where(s => !activeMatchIds.Contains(s.MatchId))
                .ToList();

            foreach (var state in finished)
            {
                administrators.ObtenerExistente(state.RoomCode)?
                    .EliminarGuardianesTemporales(state.GuardianIds);

                _active.Remove(state.MatchId);
            }

            _activated.RemoveWhere(id => !activeMatchIds.Contains(id));
            return finished;
        }
    }

    private static DateTime GetScheduledTime(Partida match)
    {
        double delay = ActivationDelaySeconds;

        if (match.TipoFinalizacion == "TIEMPO" &&
            match.LimiteTiempo.GetValueOrDefault() > 0)
        {
            delay = Math.Min(ActivationDelaySeconds, match.LimiteTiempo!.Value * 0.2);
        }

        return match.FechaInicio.AddSeconds(delay);
    }

    private static List<(float X, float Y)> ChoosePositions(
        List<(float X, float Y)> candidates,
        AdministradorGuardianes administrator,
        IReadOnlyList<ClientConnection> connections)
    {
        var positions = new List<(float X, float Y)>(ExtraGuardians);
        var occupied = administrator.ObtenerGuardianes()
            .Select(g => (g.X, g.Y))
            .ToList();

        // Priorizamos zonas próximas a jugadores, para que la oleada sea visible;
        // nunca encima de jugadores, guardianes ni otros nuevos.
        bool hasPlayerPosition = connections.Any(c => c.HasReceivedPosition);

        for (int i = 0; i < ExtraGuardians; i++)
        {
            bool found = false;

            for (int attempt = 0; attempt < 500; attempt++)
            {
                var candidate = candidates[Random.Shared.Next(candidates.Count)];

                if (occupied.Any(p => SquaredDistance(candidate, p) <
                    MinimumSeparation * MinimumSeparation))
                    continue;

                var playerDistances = connections
                    .Where(c => c.HasReceivedPosition)
                    .Select(c => SquaredDistance(candidate, (c.X, c.Y)))
                    .ToList();

                if (playerDistances.Any(d => d < 2.25f * 2.25f))
                    continue;

                // Los últimos intentos aceptan cualquier punto válido del mapa.
                if (hasPlayerPosition && attempt < 350 &&
                    !playerDistances.Any(d => d <= 12f * 12f))
                    continue;

                positions.Add(candidate);
                occupied.Add(candidate);
                found = true;
                break;
            }

            if (!found)
                return new List<(float X, float Y)>();
        }

        return positions;
    }

    private static float SquaredDistance(
        (float X, float Y) a,
        (float X, float Y) b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
