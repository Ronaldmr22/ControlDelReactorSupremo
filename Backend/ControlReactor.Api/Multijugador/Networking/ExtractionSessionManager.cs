
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

    public void StopAll(ClientConnection connection)
    {
        lock (_sync)
        {
            _activeSessions.Remove(connection.PlayerId);

            // Clear IDs associated with this disconnected connection.
            _usedExtractionIds.RemoveWhere(id =>
                id.StartsWith(connection.PlayerId + ":",
                    StringComparison.Ordinal));
        }
    }
}
