using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.ServicioSegundoPlano;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Api.Multijugador.Events;

public enum CreativeEventKind { Sand, DoublePoints }

public sealed class SandZonePosition
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed record CreativeEventState(
    int MatchId, string RoomCode, CreativeEventKind Kind,
    DateTime ExpiresAtUtc, IReadOnlyList<SandZonePosition> SandZones);

public sealed record CreativeEventChange(bool Started, CreativeEventState State);

// CA-22.4. Los dos efectos comparten un plan por partida; no se solapan entre sí.
public sealed class CreativeEventsManager
{
    public const int SandCount = 4;
    public const int SandDurationSeconds = 20;
    public const int DoubleDurationSeconds = 15;
    public const int DoublePointsMultiplier = 2;

    private const float ZoneHalfWidth = 1.0f; // Zona de 2 x 2 unidades.

    private sealed class MatchPlan
    {
        public required int MatchId { get; init; }
        public required string RoomCode { get; init; }
        public required DateTime SandAtUtc { get; init; }
        public required DateTime DoubleAtUtc { get; init; }
        public bool SandStarted { get; set; }
        public bool DoubleStarted { get; set; }
        public CreativeEventState? Active { get; set; }
    }

    private readonly object _sync = new();
    private readonly Dictionary<int, MatchPlan> _plans = new();
    private readonly Dictionary<int, List<SandZonePosition>> _positions = new();

    public CreativeEventsManager(string mapsDirectory)
    {
        for (int sceneId = 1; sceneId <= 3; sceneId++)
        {
            var map = CargadorColisionesMapa.Cargar(
                Path.Combine(mapsDirectory, $"ColisionesMapa{sceneId}.json"));

            if (map.IdEscenario != sceneId)
                throw new InvalidDataException($"Colisiones incorrectas para mapa {sceneId}.");

            var vertices = map.Obstaculos.SelectMany(o => o.Vertices).ToList();
            if (vertices.Count == 0)
                throw new InvalidDataException($"Mapa {sceneId} sin vértices de colisión.");

            // Probamos el área completa del tile, no solo su punto central.
            float minX = vertices.Min(v => v.X) + ZoneHalfWidth + 0.6f;
            float maxX = vertices.Max(v => v.X) - ZoneHalfWidth - 0.6f;
            float minY = vertices.Min(v => v.Y) + ZoneHalfWidth + 0.6f;
            float maxY = vertices.Max(v => v.Y) - ZoneHalfWidth - 0.6f;
            var candidates = new List<SandZonePosition>();

            for (float x = minX; x <= maxX; x += 0.5f)
            for (float y = minY; y <= maxY; y += 0.5f)
            {
                if (!ZoneFits(sceneId, map, x, y))
                    continue;

                candidates.Add(new SandZonePosition { X = x, Y = y });
            }

            if (candidates.Count < SandCount)
                throw new InvalidDataException(
                    $"Mapa {sceneId}: no hay espacio para cuatro arenas movedizas.");

            _positions.Add(sceneId, candidates);
        }
    }

    private static bool ZoneFits(int sceneId, DatosColisionesMapa map, float x, float y)
    {
        // Muestreo denso de una superficie 2x2 para no pisar paredes.
        for (float dx = -ZoneHalfWidth; dx <= ZoneHalfWidth + 0.001f; dx += 0.25f)
        for (float dy = -ZoneHalfWidth; dy <= ZoneHalfWidth + 0.001f; dy += 0.25f)
            if (DetectorColisionesMapa.PuntoEnObstaculo(x + dx, y + dy, map))
                return false;

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

    // Llamar bajo ReactorOverloadManager.EnterRoomGateAsync, como ExtractionProcessor.
    public CreativeEventChange? Advance(
        Partida match, IReadOnlyList<ClientConnection> connections, DateTime nowUtc)
    {
        if (match.Estado != "EN_CURSO" || string.IsNullOrWhiteSpace(match.CodigoSala))
            return null;

        lock (_sync)
        {
            if (!_plans.TryGetValue(match.IdPartida, out var plan))
            {
                plan = BuildPlan(match);
                _plans.Add(match.IdPartida, plan);
            }

            if (plan.Active != null)
            {
                if (nowUtc < plan.Active.ExpiresAtUtc)
                    return null;

                var ended = plan.Active;
                plan.Active = null;
                return new CreativeEventChange(false, ended);
            }

            if (match.TipoFinalizacion == "TIEMPO" &&
                match.LimiteTiempo.GetValueOrDefault() > 0 &&
                nowUtc >= match.FechaInicio.AddSeconds(match.LimiteTiempo!.Value))
                return null;

            if (!plan.SandStarted && nowUtc >= plan.SandAtUtc)
            {
                var zones = PickFourPositions(match.IdEscenario, connections);
                if (zones == null)
                    return null;

                plan.SandStarted = true;
                plan.Active = new CreativeEventState(match.IdPartida, plan.RoomCode,
                    CreativeEventKind.Sand, nowUtc.AddSeconds(SandDurationSeconds), zones);
                return new CreativeEventChange(true, plan.Active);
            }

            if (!plan.DoubleStarted && nowUtc >= plan.DoubleAtUtc)
            {
                plan.DoubleStarted = true;
                plan.Active = new CreativeEventState(match.IdPartida, plan.RoomCode,
                    CreativeEventKind.DoublePoints,
                    nowUtc.AddSeconds(DoubleDurationSeconds), Array.Empty<SandZonePosition>());
                return new CreativeEventChange(true, plan.Active);
            }

            return null;
        }
    }

    private List<SandZonePosition>? PickFourPositions(
        int sceneId, IReadOnlyList<ClientConnection> connections)
    {
        if (!_positions.TryGetValue(sceneId, out var candidates))
            return null;

        var chosen = new List<SandZonePosition>();
        foreach (var position in candidates.OrderBy(_ => Random.Shared.Next()))
        {
            if (chosen.Any(p => DistanceSquared(p.X, p.Y, position.X, position.Y) < 9f))
                continue; // Sin solapamiento entre los cuadrados 2x2.

            if (connections.Any(c => c.HasReceivedPosition &&
                DistanceSquared(c.X, c.Y, position.X, position.Y) < 4.84f))
                continue; // Evita colocar una zona encima de un avatar.

            chosen.Add(position);
            if (chosen.Count == SandCount)
                return chosen;
        }

        return null;
    }

    private static float DistanceSquared(float ax, float ay, float bx, float by)
        => (ax - bx) * (ax - bx) + (ay - by) * (ay - by);

    public bool IsDoublePoints(string roomCode)
    {
        lock (_sync)
            return _plans.Values.Any(p =>
                p.RoomCode.Equals(roomCode, StringComparison.OrdinalIgnoreCase) &&
                p.Active is { Kind: CreativeEventKind.DoublePoints } active &&
                active.ExpiresAtUtc > DateTime.UtcNow);
    }

    public CreativeEventState? GetActiveForRoom(string roomCode)
    {
        lock (_sync)
            return _plans.Values
                .Select(p => p.Active)
                .FirstOrDefault(s => s != null &&
                    s.RoomCode.Equals(roomCode, StringComparison.OrdinalIgnoreCase) &&
                    s.ExpiresAtUtc > DateTime.UtcNow);
    }

    public List<CreativeEventState> RemoveFinishedMatches(HashSet<int> activeMatchIds)
    {
        lock (_sync)
        {
            var ended = _plans.Values
                .Where(p => !activeMatchIds.Contains(p.MatchId))
                .Select(p => p.Active)
                .OfType<CreativeEventState>()
                .ToList();

            foreach (int id in _plans.Keys.Where(id => !activeMatchIds.Contains(id)).ToList())
                _plans.Remove(id);

            return ended;
        }
    }

    private static MatchPlan BuildPlan(Partida match)
    {
        double sand, points;
        if (match.TipoFinalizacion == "TIEMPO" && match.LimiteTiempo.GetValueOrDefault() > 0)
        {
            double total = match.LimiteTiempo!.Value;
            sand = RandomBetween(total * 0.15, total * 0.25);
            points = RandomBetween(total * 0.48, total * 0.58);
        }
        else
        {
            sand = RandomBetween(10, 17);
            points = RandomBetween(40, 50);
        }

        return new MatchPlan
        {
            MatchId = match.IdPartida,
            RoomCode = match.CodigoSala!,
            SandAtUtc = match.FechaInicio.AddSeconds(sand),
            DoubleAtUtc = match.FechaInicio.AddSeconds(points)
        };
    }

    private static double RandomBetween(double min, double max)
        => min + Random.Shared.NextDouble() * (max - min);
}
