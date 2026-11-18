
using System.Data;
using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Api.Multijugador.Networking;

public sealed class ExtractionProcessor : BackgroundService
{
    private const int MaxEnergy = 100;

    private static readonly TimeSpan RechargeDuration =
        TimeSpan.FromSeconds(15);

    private readonly ExtractionSessionManager _sessions;
    private readonly ConnectionGroupManager _groups;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExtractionProcessor> _logger;
    private readonly ReactorOverloadManager _overloads;
    private readonly CreativeEventsManager _creativeEvents;

    private readonly Dictionary<string, DateTimeOffset> _lastAward = new();

    public ExtractionProcessor(
        ExtractionSessionManager sessions,
        ConnectionGroupManager groups,
        IServiceScopeFactory scopeFactory,
        ILogger<ExtractionProcessor> logger,
        ReactorOverloadManager overloads,
        CreativeEventsManager creativeEvents)
    {
        _sessions = sessions;
        _groups = groups;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _overloads = overloads;
        _creativeEvents = creativeEvents;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(100));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var session in _sessions.GetActiveSessions())
                {
                    try
                    {
                        await ProcessSessionAsync(session, stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error al procesar extracción de {PlayerId}",
                            session.PlayerId);
                    }
                }

                try
                {
                    await ProcessRechargesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error procesando recargas");
                }

                var activeKeys = _sessions.GetActiveSessions()
                    .Select(GetSessionKey)
                    .ToHashSet();

                foreach (string key in _lastAward.Keys.ToList())
                {
                    if (!activeKeys.Contains(key))
                        _lastAward.Remove(key);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task ProcessSessionAsync(
        ExtractionSession session,
        CancellationToken cancellationToken)
    {
        if (!_sessions.IsActive(session))
            return;

        string key = GetSessionKey(session);

        DateTimeOffset lastAward =
            _lastAward.TryGetValue(key, out var previous)
                ? previous
                : session.StartedAt;

        if (DateTimeOffset.UtcNow - lastAward <
            TimeSpan.FromSeconds(1))
            return;

        var connections =
            _groups.ObtenerConexionesSala(session.RoomCode);

        ClientConnection? connection = connections.FirstOrDefault(
            c => c.PlayerId == session.PlayerId);

        if (connection == null)
        {
            _sessions.TryStopSession(session);
            return;
        }

        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ControlReactorDbContext>();

        string? invalidReason = null;
        int matchId = 0;
        int playerMatchId = 0;
        int userId = 0;
        int score = 0;
        int remainingEnergy = 0;
        bool depleted = false;

        using (await _overloads.EnterRoomGateAsync(
            session.RoomCode, cancellationToken))
        {
            if (_overloads.IsOverloaded(session.RoomCode))
            {
                invalidReason = "El reactor está en sobrecarga.";
            }
            else
            {
        await using (var transaction =
            await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken))
        {
            var player = await db.JugadoresPartida
                .FirstOrDefaultAsync(
                    p => p.IdJugadorPartida == session.PlayerMatchId,
                    cancellationToken);

            if (player == null ||
                !player.EnSala ||
                connection.IdJugadorPartida != player.IdJugadorPartida ||
                connection.IdUsuario != player.IdUsuario)
            {
                invalidReason = "Jugador no válido.";
            }
            else
            {
                var match = await db.Partidas
                    .FirstOrDefaultAsync(
                        p => p.IdPartida == player.IdPartida,
                        cancellationToken);

                if (match == null ||
                    match.Estado != "EN_CURSO" ||
                    match.CodigoSala != session.RoomCode)
                {
                    invalidReason = "La partida no está activa.";
                }
                else if (!ReactorConfiguration.TryGet(
                    match.IdEscenario,
                    out ReactorData reactor))
                {
                    invalidReason = "Reactor no configurado.";
                }
                else
                {
                    float dx = connection.X - reactor.X;
                    float dy = connection.Y - reactor.Y;

                    bool positionValid =
                        connection.HasReceivedPosition &&
                        float.IsFinite(connection.X) &&
                        float.IsFinite(connection.Y) &&
                        dx * dx + dy * dy <=
                        reactor.Radius * reactor.Radius;

                    if (!positionValid)
                    {
                        invalidReason = "Te alejaste del reactor.";
                    }
                    else if (match.EnergiaRestante <= 0 ||
                             match.ReactorRechargeReadyAtUtc != null)
                    {
                        invalidReason = "El reactor está recargando.";
                    }
                    else if (
                        match.TipoFinalizacion == "PUNTOS" &&
                        match.PuntajeObjetivo.HasValue &&
                        player.Puntaje >= match.PuntajeObjetivo.Value)
                    {
                        invalidReason = "Alcanzaste el puntaje objetivo.";
                    }
                    else if (!_sessions.IsActive(session))
                    {
                        return;
                    }
                    else
                    {
                        match.EnergiaRestante--;
                        // CA-22.4: doble puntaje solo por extracción del reactor.
                        // CA-22.3 conserva prioridad: arriba ya bloqueamos por sobrecarga.
                        player.Puntaje += _creativeEvents.IsDoublePoints(session.RoomCode)
                            ? CreativeEventsManager.DoublePointsMultiplier : 1;

                        if (match.EnergiaRestante == 0)
                        {
                            depleted = true;
                            match.ReactorRechargeReadyAtUtc =
                                DateTime.UtcNow.Add(RechargeDuration);
                        }

                        await db.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        matchId = match.IdPartida;
                        playerMatchId = player.IdJugadorPartida;
                        userId = player.IdUsuario;
                        score = player.Puntaje;
                        remainingEnergy = match.EnergiaRestante;
                    }
                }
            }
        }
            }
        }

        if (invalidReason != null)
        {
            await StopInvalidSessionAsync(
                session, connection, invalidReason);
            return;
        }

        if (matchId == 0)
            return;

        _lastAward[key] = DateTimeOffset.UtcNow;

        await BroadcastAsync(
            session.RoomCode,
            new ServerMessage
            {
                Type = "extraction_updated",
                RoomCode = session.RoomCode,
                IdJugadorPartida = playerMatchId,
                IdUsuario = userId,
                Puntaje = score,
                EnergiaRestante = remainingEnergy
            },
            cancellationToken);

        if (depleted)
        {
            var stoppedSessions =
                _sessions.StopRoom(session.RoomCode);

            foreach (var stopped in stoppedSessions)
            {
                var recipient = connections.FirstOrDefault(
                    c => c.PlayerId == stopped.PlayerId);

                if (recipient != null)
                {
                    await SendSafelyAsync(
                        recipient,
                        new ServerMessage
                        {
                            Type = "extract_stopped",
                            RoomCode = session.RoomCode,
                            Message = "El reactor está recargando."
                        });
                }
            }
        }

        _logger.LogInformation(
            "Partida {MatchId}: jugador {PlayerId}, puntos {Score}, energía {Energy}",
            matchId,
            session.PlayerId,
            score,
            remainingEnergy);
    }

    private async Task ProcessRechargesAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<ControlReactorDbContext>();

        DateTime now = DateTime.UtcNow;

        var readyMatches = await db.Partidas
            .AsNoTracking()
            .Where(p =>
                p.Estado == "EN_CURSO" &&
                p.EnergiaRestante == 0 &&
                p.ReactorRechargeReadyAtUtc != null &&
                p.ReactorRechargeReadyAtUtc <= now)
            .Select(p => new
            {
                p.IdPartida,
                p.CodigoSala
            })
            .ToListAsync(cancellationToken);

        foreach (var match in readyMatches)
        {
            int updated = await db.Partidas
                .Where(p =>
                    p.IdPartida == match.IdPartida &&
                    p.Estado == "EN_CURSO" &&
                    p.EnergiaRestante == 0 &&
                    p.ReactorRechargeReadyAtUtc != null &&
                    p.ReactorRechargeReadyAtUtc <= now)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            p => p.EnergiaRestante, MaxEnergy)
                        .SetProperty(
                            p => p.ReactorRechargeReadyAtUtc,
                            (DateTime?)null),
                    cancellationToken);

            if (updated != 1 ||
                string.IsNullOrWhiteSpace(match.CodigoSala))
                continue;

            await BroadcastAsync(
                match.CodigoSala,
                new ServerMessage
                {
                    Type = "reactor_recharged",
                    RoomCode = match.CodigoSala,
                    EnergiaRestante = MaxEnergy,
                    Message = "El reactor volvió a estar disponible."
                },
                cancellationToken);

            _logger.LogInformation(
                "Reactor recargado en partida {MatchId}",
                match.IdPartida);
        }
    }

    private async Task StopInvalidSessionAsync(
        ExtractionSession session,
        ClientConnection connection,
        string reason)
    {
        if (!_sessions.TryStopSession(session))
            return;

        await SendSafelyAsync(
            connection,
            new ServerMessage
            {
                Type = "extract_stopped",
                RoomCode = session.RoomCode,
                Message = reason
            });
    }

    private async Task BroadcastAsync(
        string roomCode,
        ServerMessage message,
        CancellationToken cancellationToken)
    {
        foreach (var connection in
            _groups.ObtenerConexionesSala(roomCode))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendSafelyAsync(connection, message);
        }
    }

    private async Task SendSafelyAsync(
        ClientConnection connection,
        ServerMessage message)
    {
        try
        {
            await connection.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "No se pudo enviar mensaje al jugador");
        }
    }

    private static string GetSessionKey(
        ExtractionSession session)
    {
        return $"{session.PlayerId}:{session.ExtractionId}";
    }
}
