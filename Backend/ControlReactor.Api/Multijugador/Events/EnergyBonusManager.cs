using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.ServicioSegundoPlano;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Api.Multijugador.Events;

public sealed record EnergyBonusState(
    int MatchId,
    string RoomCode,
    string BonusId,
    float X,
    float Y,
    DateTime ExpiresAtUtc);

public sealed record EnergyBonusChange(bool Spawned, EnergyBonusState Bonus);

// Un único estado de bonificación por partida, compartido entre todos los WebSockets.
public sealed class EnergyBonusManager
{
    public const int BonusPoints = 10;
    public const int DurationSeconds = 10;
    public const int AppearancesPerMatch = 3;

    private const float CollectionRadius = 0.8f;
    private const float MapMargin = 0.7f;

    private sealed class ActiveBonus
    {
        public required EnergyBonusState State { get; init; }
        public bool Reserved { get; set; }
    }

    private readonly object _sync = new();
    private readonly Dictionary<int, ActiveBonus> _active = new();
    private readonly Dictionary<int, int> _spawnCount = new();
    private readonly Dictionary<int, List<(float X, float Y)>> _possiblePositions = new();

    public EnergyBonusManager(string mapsDirectory)
    {
        for (int sceneId = 1; sceneId <= 3; sceneId++)
        {
            string path = Path.Combine(mapsDirectory, $"ColisionesMapa{sceneId}.json");
            var map = CargadorColisionesMapa.Cargar(path);

            if (map.IdEscenario != sceneId)
                throw new InvalidDataException($"Mapa de colisiones incorrecto: {sceneId}");

            var vertices = map.Obstaculos.SelectMany(o => o.Vertices).ToList();
            if (vertices.Count == 0)
                throw new InvalidDataException($"Mapa {sceneId} sin límites de colisión.");

            float minX = vertices.Min(v => v.X) + MapMargin;
            float maxX = vertices.Max(v => v.X) - MapMargin;
            float minY = vertices.Min(v => v.Y) + MapMargin;
            float maxY = vertices.Max(v => v.Y) - MapMargin;

            var candidates = new List<(float X, float Y)>();

            for (float x = minX; x <= maxX; x += 0.45f)
            for (float y = minY; y <= maxY; y += 0.45f)
            {
                if (DetectorColisionesMapa.CuerpoEnObstaculo(x, y, map))
                    continue;

                if (ZonasPuertasArena.ObtenerPorEscenario(sceneId)
                    .Any(door => door.Contiene(x, y, 0.65f, 0.65f)))
                    continue;

                if (ReactorConfiguration.TryGet(sceneId, out var reactor))
                {
                    float dx = x - reactor.X;
                    float dy = y - reactor.Y;
                    float minDistance = reactor.Radius + 0.6f;

                    if (dx * dx + dy * dy < minDistance * minDistance)
                        continue;
                }

                candidates.Add((x, y));
            }

            if (candidates.Count == 0)
                throw new InvalidDataException($"Mapa {sceneId} sin lugares libres para la pastilla.");

            _possiblePositions.Add(sceneId, candidates);
        }
    }

    public EnergyBonusChange? Advance(
        Partida match,
        IReadOnlyList<ClientConnection> connections,
        DateTime nowUtc)
    {
        if (match.Estado != "EN_CURSO" || string.IsNullOrWhiteSpace(match.CodigoSala))
            return null;

        lock (_sync)
        {
            if (_active.TryGetValue(match.IdPartida, out var current))
            {
                if (current.Reserved || current.State.ExpiresAtUtc > nowUtc)
                    return null;

                _active.Remove(match.IdPartida);
                return new EnergyBonusChange(false, current.State);
            }

            int count = _spawnCount.GetValueOrDefault(match.IdPartida);
            if (count >= AppearancesPerMatch ||
                nowUtc < GetScheduledTime(match, count) ||
                !_possiblePositions.TryGetValue(match.IdEscenario, out var candidates))
                return null;

            // Nunca aparecer encima de un avatar conectado.
            (float X, float Y)? position = null;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                var candidate = candidates[Random.Shared.Next(candidates.Count)];

                bool nearPlayer = connections.Any(c =>
                    c.HasReceivedPosition &&
                    (c.X - candidate.X) * (c.X - candidate.X) +
                    (c.Y - candidate.Y) * (c.Y - candidate.Y) < 1.44f);

                if (!nearPlayer)
                {
                    position = candidate;
                    break;
                }
            }

            if (position == null)
                return null;

            var bonus = new EnergyBonusState(
                match.IdPartida,
                match.CodigoSala,
                Guid.NewGuid().ToString("N"),
                position.Value.X,
                position.Value.Y,
                nowUtc.AddSeconds(DurationSeconds));

            _active.Add(match.IdPartida, new ActiveBonus { State = bonus });
            _spawnCount[match.IdPartida] = count + 1;

            return new EnergyBonusChange(true, bonus);
        }
    }

    // Para el cliente que se conecta cuando ya existe una pastilla.
    public EnergyBonusState? GetActiveForRoom(string roomCode)
    {
        lock (_sync)
        {
            return _active.Values
                .Select(a => a.State)
                .FirstOrDefault(s =>
                    s.RoomCode.Equals(roomCode, StringComparison.OrdinalIgnoreCase) &&
                    s.ExpiresAtUtc > DateTime.UtcNow);
        }
    }

    public bool TryReserve(
        string roomCode,
        string bonusId,
        float x,
        float y,
        out EnergyBonusState? bonus)
    {
        lock (_sync)
        {
            var candidate = _active.Values.FirstOrDefault(a =>
                a.State.RoomCode.Equals(roomCode, StringComparison.OrdinalIgnoreCase) &&
                a.State.BonusId == bonusId);

            bonus = null;

            if (candidate == null || candidate.Reserved ||
                candidate.State.ExpiresAtUtc <= DateTime.UtcNow)
                return false;

            float dx = x - candidate.State.X;
            float dy = y - candidate.State.Y;

            if (!float.IsFinite(x) || !float.IsFinite(y) ||
                dx * dx + dy * dy > CollectionRadius * CollectionRadius)
                return false;

            candidate.Reserved = true;
            bonus = candidate.State;
            return true;
        }
    }

    public void Release(EnergyBonusState bonus)
    {
        lock (_sync)
        {
            if (_active.TryGetValue(bonus.MatchId, out var current) &&
                current.State.BonusId == bonus.BonusId)
                current.Reserved = false;
        }
    }

    public void Complete(EnergyBonusState bonus)
    {
        lock (_sync)
        {
            if (_active.TryGetValue(bonus.MatchId, out var current) &&
                current.State.BonusId == bonus.BonusId)
                _active.Remove(bonus.MatchId);
        }
    }

    public List<EnergyBonusState> RemoveFinishedMatches(HashSet<int> activeMatchIds)
    {
        lock (_sync)
        {
            var removed = _active
                .Where(pair => !activeMatchIds.Contains(pair.Key))
                .Select(pair => pair.Value.State)
                .ToList();

            foreach (var bonus in removed)
                _active.Remove(bonus.MatchId);

            foreach (int matchId in _spawnCount.Keys.ToList())
                if (!activeMatchIds.Contains(matchId))
                    _spawnCount.Remove(matchId);

            return removed;
        }
    }

    private static DateTime GetScheduledTime(Partida match, int appearance)
    {
        if (match.TipoFinalizacion == "TIEMPO" && match.LimiteTiempo > 0)
        {
            // El tiempo corre desde el inicio real de la partida.
            double[] fractions = { 0.15, 0.45, 0.75 };
            return match.FechaInicio.AddSeconds(
                match.LimiteTiempo.Value * fractions[appearance]);
        }

        // En partidas por puntos no existe una duración predeterminada.
        int[] seconds = { 5, 25, 45 };
        return match.FechaInicio.AddSeconds(seconds[appearance]);
    }
}
