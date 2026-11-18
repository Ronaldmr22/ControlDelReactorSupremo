using System.Collections.Concurrent;
using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Api.Multijugador.Events;

public sealed record ReactorOverloadState(
    int MatchId,
    string RoomCode,
    DateTime ExpiresAtUtc);

public sealed record ReactorOverloadChange(
    bool Started,
    ReactorOverloadState State);

// CA-22.3: la autoridad del estado del reactor está en el backend.
public sealed class ReactorOverloadManager
{
    public const int DurationSeconds = 10;
    public const int ActivationsPerMatch = 2;

    private sealed class MatchPlan
    {
        public required int MatchId { get; init; }
        public required string RoomCode { get; init; }
        public required DateTime[] ScheduledStartsUtc { get; init; }
        public int NextActivation { get; set; }
        public DateTime? ActiveUntilUtc { get; set; }
    }

    private sealed class GateLease : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private bool _released;

        public GateLease(SemaphoreSlim semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            if (_released) return;
            _released = true;
            _semaphore.Release();
        }
    }

    private readonly object _sync = new();
    private readonly Dictionary<int, MatchPlan> _plans = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _roomGates =
        new(StringComparer.OrdinalIgnoreCase);

    // Compartimos el candado con el procesador de puntuación: una extracción
    // no puede confirmar puntos mientras se activa una sobrecarga.
    public async Task<IDisposable> EnterRoomGateAsync(
        string roomCode, CancellationToken cancellationToken)
    {
        string code = roomCode.Trim().ToUpperInvariant();
        var semaphore = _roomGates.GetOrAdd(
            code, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        return new GateLease(semaphore);
    }

    // Debe ejecutarse teniendo adquirido el candado de la sala.
    public ReactorOverloadChange? Advance(Partida match, DateTime nowUtc)
    {
        if (match.Estado != "EN_CURSO" || string.IsNullOrWhiteSpace(match.CodigoSala))
            return null;

        lock (_sync)
        {
            if (!_plans.TryGetValue(match.IdPartida, out var plan))
            {
                plan = new MatchPlan
                {
                    MatchId = match.IdPartida,
                    RoomCode = match.CodigoSala,
                    ScheduledStartsUtc = BuildSchedule(match)
                };
                _plans.Add(match.IdPartida, plan);
            }

            if (plan.ActiveUntilUtc is DateTime activeUntil)
            {
                if (nowUtc < activeUntil)
                    return null;

                plan.ActiveUntilUtc = null;
                return new ReactorOverloadChange(false,
                    new ReactorOverloadState(plan.MatchId, plan.RoomCode, activeUntil));
            }

            if (plan.NextActivation >= plan.ScheduledStartsUtc.Length ||
                nowUtc < plan.ScheduledStartsUtc[plan.NextActivation])
                return null;

            if (match.TipoFinalizacion == "TIEMPO" &&
                match.LimiteTiempo.GetValueOrDefault() > 0 &&
                nowUtc >= match.FechaInicio.AddSeconds(match.LimiteTiempo!.Value))
                return null;

            plan.NextActivation++;
            DateTime expiresAtUtc = nowUtc.AddSeconds(DurationSeconds);
            plan.ActiveUntilUtc = expiresAtUtc;

            return new ReactorOverloadChange(true,
                new ReactorOverloadState(plan.MatchId, plan.RoomCode, expiresAtUtc));
        }
    }

    public bool IsOverloaded(string roomCode)
    {
        lock (_sync)
        {
            return _plans.Values.Any(p =>
                p.RoomCode.Equals(roomCode, StringComparison.OrdinalIgnoreCase) &&
                p.ActiveUntilUtc is DateTime until &&
                until > DateTime.UtcNow);
        }
    }

    public ReactorOverloadState? GetActiveForRoom(string roomCode)
    {
        lock (_sync)
        {
            var active = _plans.Values.FirstOrDefault(p =>
                p.RoomCode.Equals(roomCode, StringComparison.OrdinalIgnoreCase) &&
                p.ActiveUntilUtc is DateTime until &&
                until > DateTime.UtcNow);

            if (active == null || active.ActiveUntilUtc is not DateTime expiration)
                return null;

            return new ReactorOverloadState(
                active.MatchId, active.RoomCode, expiration);
        }
    }

    // No dejar una sobrecarga activa cuando termina la partida.
    public List<ReactorOverloadState> RemoveFinishedMatches(HashSet<int> activeIds)
    {
        lock (_sync)
        {
            var finished = _plans.Values
                .Where(p => !activeIds.Contains(p.MatchId))
                .ToList();

            var ended = finished
                .Where(p => p.ActiveUntilUtc.HasValue)
                .Select(p => new ReactorOverloadState(
                    p.MatchId, p.RoomCode, p.ActiveUntilUtc!.Value))
                .ToList();

            foreach (var plan in finished)
                _plans.Remove(plan.MatchId);

            return ended;
        }
    }

    private static DateTime[] BuildSchedule(Partida match)
    {
        double first;
        double second;

        if (match.TipoFinalizacion == "TIEMPO" &&
            match.LimiteTiempo.GetValueOrDefault() > 0)
        {
            double total = match.LimiteTiempo!.Value;
            double maxStart = Math.Max(0, total - DurationSeconds);

            // Primera ventana: 10-25 %. Guardamos 20 s para poder realizar
            // una segunda activación completa sin superposición cuando sea viable.
            double firstMax = Math.Min(total * 0.25,
                Math.Max(0, total - 2 * DurationSeconds));
            double firstMin = Math.Min(total * 0.10, firstMax);
            first = RandomBetween(firstMin, firstMax);

            // Segunda ventana: 55-75 %, respetando diez segundos de separación.
            double secondMax = Math.Min(total * 0.75, maxStart);
            double secondMin = Math.Min(secondMax,
                Math.Max(total * 0.55, first + DurationSeconds));
            second = RandomBetween(secondMin, secondMax);
        }
        else
        {
            // Partidas por puntos: no hay un tiempo máximo conocido.
            first = RandomBetween(8, 15);
            second = RandomBetween(35, 50);
        }

        return new[]
        {
            match.FechaInicio.AddSeconds(first),
            match.FechaInicio.AddSeconds(second)
        };
    }

    private static double RandomBetween(double min, double max)
        => max <= min ? min : min + Random.Shared.NextDouble() * (max - min);
}
