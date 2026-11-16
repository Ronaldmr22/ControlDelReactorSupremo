
namespace ControlReactor.Api.Multijugador.Networking;

public sealed record ExtractionSession(
    string PlayerId,
    int PlayerMatchId,
    string RoomCode,
    string ExtractionId,
    DateTimeOffset StartedAt
);

public enum ExtractionStartResult
{
    Started,
    AlreadyActive,
    Duplicate
}

public sealed class ExtractionSessionManager
{
    private readonly object _sync = new();

    private readonly Dictionary<string, ExtractionSession>
        _activeSessions = new();

    private readonly HashSet<string> _usedExtractionIds = new();

    public ExtractionStartResult TryStart(
        ClientConnection connection,
        string extractionId)
    {
        lock (_sync)
        {
            string requestKey =
                $"{connection.PlayerId}:{extractionId}";

            if (_usedExtractionIds.Contains(requestKey))
                return ExtractionStartResult.Duplicate;

            if (_activeSessions.ContainsKey(connection.PlayerId))
                return ExtractionStartResult.AlreadyActive;

            if (connection.RoomCode == null ||
                connection.IdJugadorPartida == null)
                return ExtractionStartResult.Duplicate;

            var session = new ExtractionSession(
                connection.PlayerId,
                connection.IdJugadorPartida.Value,
                connection.RoomCode,
                extractionId,
                DateTimeOffset.UtcNow
            );

            _activeSessions.Add(connection.PlayerId, session);
            _usedExtractionIds.Add(requestKey);

            return ExtractionStartResult.Started;
        }
    }

    public bool TryStop(
        ClientConnection connection,
        string extractionId)
    {
        lock (_sync)
        {
            if (!_activeSessions.TryGetValue(
                connection.PlayerId, out var session))
                return false;

            if (session.ExtractionId != extractionId)
                return false;

            return _activeSessions.Remove(connection.PlayerId);
        }
    }

    public bool TryStopSession(ExtractionSession session)
    {
        lock (_sync)
        {
            if (!_activeSessions.TryGetValue(
                session.PlayerId, out var current))
                return false;

            if (current.ExtractionId != session.ExtractionId)
                return false;

            return _activeSessions.Remove(session.PlayerId);
        }
    }

    public List<ExtractionSession> StopRoom(string roomCode)
    {
        lock (_sync)
        {
            var stopped = _activeSessions.Values
                .Where(s => s.RoomCode == roomCode)
                .ToList();

            foreach (var session in stopped)
                _activeSessions.Remove(session.PlayerId);

            return stopped;
        }
    }

    public void StopAll(ClientConnection connection)
    {
        lock (_sync)
        {
            _activeSessions.Remove(connection.PlayerId);
        }
    }

    public List<ExtractionSession> GetActiveSessions()
    {
        lock (_sync)
        {
            return _activeSessions.Values.ToList();
        }
    }

    public bool IsActive(ExtractionSession session)
    {
        lock (_sync)
        {
            return _activeSessions.TryGetValue(
                session.PlayerId,
                out var current
            ) &&
            current.ExtractionId == session.ExtractionId;
        }
    }
}
