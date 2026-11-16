
using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Api.Multijugador.Networking;

public sealed class ExtractionProcessor : BackgroundService
{
    private readonly ExtractionSessionManager _sessions;
    private readonly ReactorEnergyManager _energy;
    private readonly ConnectionGroupManager _groups;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExtractionProcessor> _logger;

    private readonly Dictionary<string, DateTimeOffset>
        _lastAward = new();

    public ExtractionProcessor(
        ExtractionSessionManager sessions,
        ReactorEnergyManager energy,
        ConnectionGroupManager groups,
        IServiceScopeFactory scopeFactory,
        ILogger<ExtractionProcessor> logger)
    {
        _sessions = sessions;
        _energy = energy;
        _groups = groups;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(100));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var session in _sessions.GetActiveSessions())
            {
                try
                {
                    await ProcessSessionAsync(
                        session, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error al procesar la extracción del jugador {PlayerId}",
                        session.PlayerId);
                }
            }

            // Remove timestamps for sessions that are no longer active.
            foreach (string key in _lastAward.Keys.ToList())
            {
                bool exists = _sessions.GetActiveSessions()
                    .Any(s => GetSessionKey(s) == key);

                if (!exists)
                    _lastAward.Remove(key);
            }
        }
    }

    private async Task ProcessSessionAsync(
        ExtractionSession session,
        CancellationToken cancellationToken)
    {
        if (!_sessions.IsActive(session))
            return;

        string key = GetSessionKey(session);

        DateTimeOffset lastTime =
            _lastAward.TryGetValue(key, out var previous)
                ? previous
                : session.StartedAt;

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if ((now - lastTime).TotalSeconds < 1)
            return;

        var connections =
            _groups.ObtenerConexionesSala(session.RoomCode);

        ClientConnection? connection = connections
            .FirstOrDefault(c => c.PlayerId == session.PlayerId);

        if (connection == null ||
            !connection.HasReceivedPosition)
            return;

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ControlReactorDbContext>();

        var player = await db.JugadoresPartida
            .FirstOrDefaultAsync(
                j => j.IdJugadorPartida == session.PlayerMatchId,
                cancellationToken);

        if (player == null || !player.EnSala)
            return;

        var match = await db.Partidas
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.IdPartida == player.IdPartida,
                cancellationToken);

        if (match == null ||
            match.Estado != "EN_CURSO" ||
            match.CodigoSala != session.RoomCode)
            return;

        if (connection.IdJugadorPartida != player.IdJugadorPartida ||
            connection.IdUsuario != player.IdUsuario)
            return;

        if (!ReactorConfiguration.TryGet(
            match.IdEscenario, out ReactorData reactor))
            return;

        float dx = connection.X - reactor.X;
        float dy = connection.Y - reactor.Y;

        if (dx * dx + dy * dy >
            reactor.Radius * reactor.Radius)
            return;

        if (!_energy.IsAvailable(match.IdPartida))
            return;

        if (!_sessions.IsActive(session))
            return;

        // Award at most one point for this processing interval.
        int newScore = player.Puntaje + 1;

        if (match.TipoFinalizacion == "PUNTOS" &&
            match.PuntajeObjetivo.HasValue)
        {
            newScore = Math.Min(
                newScore,
                match.PuntajeObjetivo.Value);
        }

        if (newScore == player.Puntaje)
            return;

        player.Puntaje = newScore;

        await db.SaveChangesAsync(cancellationToken);

        if (!_energy.TryConsumeEnergy(
            match.IdPartida, out int remainingEnergy))
        {
            _logger.LogError(
                "La energía se agotó después de registrar el punto en la partida {MatchId}",
                match.IdPartida);
            return;
        }

        _lastAward[key] = now;

        var update = new ServerMessage
        {
            Type = "score_updated",
            RoomCode = session.RoomCode,
            IdJugadorPartida = player.IdJugadorPartida,
            IdUsuario = player.IdUsuario,
            Puntaje = player.Puntaje
        };

        foreach (var recipient in connections)
        {
            await recipient.SendAsync(update);
        }

        _logger.LogInformation(
            "Extracción: jugador {PlayerId}, puntaje {Score}, energía restante {Energy}",
            session.PlayerId,
            player.Puntaje,
            remainingEnergy);
    }

    private static string GetSessionKey(
        ExtractionSession session)
    {
        return $"{session.PlayerId}:{session.ExtractionId}";
    }
}
