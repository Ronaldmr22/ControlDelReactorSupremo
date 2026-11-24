using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.ServicioSegundoPlano;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Api.Multijugador.Events;

public enum TrapKind
{
    ElectricCage = 0,
    InvertedControls = 1,
    FrozenTrap = 2
}

public sealed record TrapPosition(
    TrapKind Kind,
    float X,
    float Y
);

public sealed record TrapMatchState(
    int MatchId,
    string RoomCode,
    IReadOnlyList<TrapPosition> Traps
);

public sealed class TrapManager
{
    private const float HalfWidth = 0.5f;

    private readonly object _sync = new();

    private readonly Dictionary<int, List<TrapPosition>> _candidates = new();
    private readonly Dictionary<int, TrapMatchState> _matches = new();

    public TrapManager(string mapsDirectory)
    {
        for (int sceneId = 1; sceneId <= 3; sceneId++)
        {
            var map = CargadorColisionesMapa.Cargar(
                Path.Combine(
                    mapsDirectory,
                    $"ColisionesMapa{sceneId}.json"));

            if (map.IdEscenario != sceneId)
                throw new InvalidDataException(
                    $"Colisiones incorrectas para mapa {sceneId}.");

            var vertices = map.Obstaculos
                .SelectMany(o => o.Vertices)
                .ToList();

            if (vertices.Count == 0)
                throw new InvalidDataException(
                    $"Mapa {sceneId} sin vértices.");

            float minX = vertices.Min(v => v.X) + HalfWidth + 0.6f;
            float maxX = vertices.Max(v => v.X) - HalfWidth - 0.6f;

            float minY = vertices.Min(v => v.Y) + HalfWidth + 0.6f;
            float maxY = vertices.Max(v => v.Y) - HalfWidth - 0.6f;

            var positions = new List<TrapPosition>();

            for (float x = minX; x <= maxX; x += 0.5f)
            for (float y = minY; y <= maxY; y += 0.5f)
            {
                if (!PositionFits(sceneId, map, x, y))
                    continue;

                positions.Add(new TrapPosition(
                    TrapKind.InvertedControls, x, y));
            }

            if (positions.Count < 3)
                throw new InvalidDataException(
                    $"Mapa {sceneId}: no hay espacio para las trampas.");

            _candidates.Add(sceneId, positions);
        }
    }

    private static bool PositionFits(
        int sceneId,
        DatosColisionesMapa map,
        float x,
        float y)
    {
        for (float dx = -HalfWidth;
             dx <= HalfWidth + 0.001f;
             dx += 0.25f)
        {
            for (float dy = -HalfWidth;
                 dy <= HalfWidth + 0.001f;
                 dy += 0.25f)
            {
                if (DetectorColisionesMapa.PuntoEnObstaculo(
                    x + dx, y + dy, map))
                    return false;
            }
        }

        if (DetectorColisionesMapa.CuerpoEnObstaculo(x, y, map))
            return false;

        if (ZonasPuertasArena.ObtenerPorEscenario(sceneId)
            .Any(door => door.Contiene(x, y, 1.25f, 1.25f)))
            return false;

        if (ReactorConfiguration.TryGet(sceneId, out var reactor))
        {
            float rx = x - reactor.X;
            float ry = y - reactor.Y;

            float minimum = reactor.Radius + 1.75f;

            if (rx * rx + ry * ry < minimum * minimum)
                return false;
        }

        return true;
    }

    public TrapMatchState? GetOrCreate(
        Partida match,
        IReadOnlyList<ClientConnection> connections)
    {
        if (match.Estado != "EN_CURSO" ||
            string.IsNullOrWhiteSpace(match.CodigoSala))
            return null;

        lock (_sync)
        {
            if (_matches.TryGetValue(match.IdPartida, out var existing))
                return existing;

            if (!_candidates.TryGetValue(
                match.IdEscenario, out var candidates))
                return null;

            var chosen = new List<TrapPosition>();

            foreach (var candidate in candidates
                .OrderBy(_ => Random.Shared.Next()))
            {
                if (chosen.Any(p =>
                    DistanceSquared(
                        p.X, p.Y,
                        candidate.X, candidate.Y) < 9f))
                    continue;

                if (connections.Any(c =>
                    c.HasReceivedPosition &&
                    DistanceSquared(
                        c.X, c.Y,
                        candidate.X, candidate.Y) < 4.84f))
                    continue;

                TrapKind kind = chosen.Count switch
                {
                    0 => TrapKind.InvertedControls,
                    1 => TrapKind.ElectricCage,
                    2 => TrapKind.FrozenTrap,
                    _ => throw new InvalidOperationException(
                        "Cantidad de trampas no válida.")
                };

                chosen.Add(new TrapPosition(
                    kind, candidate.X, candidate.Y));

                if (chosen.Count == 3)
                    break;
            }

            if (chosen.Count != 3)
                return null;

            var state = new TrapMatchState(
                match.IdPartida,
                match.CodigoSala!,
                chosen);

            _matches.Add(match.IdPartida, state);

            return state;
        }
    }

    public TrapMatchState? GetForRoom(string roomCode)
    {
        lock (_sync)
        {
            return _matches.Values.FirstOrDefault(
                m => m.RoomCode.Equals(
                    roomCode,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    public void RemoveFinishedMatches(HashSet<int> activeMatchIds)
    {
        lock (_sync)
        {
            foreach (int id in _matches.Keys
                .Where(id => !activeMatchIds.Contains(id))
                .ToList())
            {
                _matches.Remove(id);
            }
        }
    }

    private static float DistanceSquared(
        float ax, float ay, float bx, float by)
    {
        float dx = ax - bx;
        float dy = ay - by;

        return dx * dx + dy * dy;
    }
}
