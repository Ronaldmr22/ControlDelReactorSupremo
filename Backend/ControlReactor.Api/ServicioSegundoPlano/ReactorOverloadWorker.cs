using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Api.ServicioSegundoPlano;

public sealed class ReactorOverloadWorker : BackgroundService
{
    private readonly ReactorOverloadManager _overloads;
    private readonly ExtractionSessionManager _sessions;
    private readonly ConnectionGroupManager _groups;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReactorOverloadWorker> _logger;

    public ReactorOverloadWorker(
        ReactorOverloadManager overloads,
        ExtractionSessionManager sessions,
        ConnectionGroupManager groups,
        IServiceScopeFactory scopeFactory,
        ILogger<ReactorOverloadWorker> logger)
    {
        _overloads = overloads;
        _sessions = sessions;
        _groups = groups;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider
                        .GetRequiredService<ControlReactorDbContext>();

                    var matches = await db.Partidas
                        .AsNoTracking()
                        .Where(p => p.Estado == "EN_CURSO" && p.CodigoSala != null)
                        .ToListAsync(stoppingToken);

                    var activeIds = matches.Select(m => m.IdPartida).ToHashSet();

                    foreach (var ended in _overloads.RemoveFinishedMatches(activeIds))
                        await BroadcastAsync(ended.RoomCode, false, 0);

                    foreach (var match in matches)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        string roomCode = match.CodigoSala!;
                        ReactorOverloadChange? change;
                        List<ExtractionSession> interrupted = new();

                        // Misma puerta de sincronización que la transacción de
                        // puntuación: ninguna extracción confirma puntos después
                        // de activar la sobrecarga.
                        using (await _overloads.EnterRoomGateAsync(
                            roomCode, stoppingToken))
                        {
                            change = _overloads.Advance(match, DateTime.UtcNow);

                            if (change?.Started == true)
                                interrupted = _sessions.StopRoom(roomCode);
                        }

                        if (change == null)
                            continue;

                        _logger.LogWarning(
                            "CA-22.3 | Sala {Sala} | Sobrecarga {Estado} | Hasta {Fin}",
                            roomCode, change.Started ? "INICIADA" : "FINALIZADA",
                            change.State.ExpiresAtUtc);

                        // Restablecer el estado visual en todos los clientes.
                        int remaining = change.Started
                            ? Math.Max(1, (int)Math.Ceiling(
                                (change.State.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds))
                            : 0;

                        await BroadcastAsync(roomCode, change.Started, remaining);

                        foreach (var session in interrupted)
                        {
                            var recipient = _groups.ObtenerConexionesSala(roomCode)
                                .FirstOrDefault(c => c.PlayerId == session.PlayerId);

                            if (recipient != null)
                                await SendSafelyAsync(recipient, new ServerMessage
                                {
                                    Type = "extract_stopped",
                                    RoomCode = roomCode,
                                    Message = "Extracción cancelada: reactor en sobrecarga."
                                });
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en la sobrecarga del reactor (CA-22.3).");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Fin normal del servicio.
        }
    }

    private async Task BroadcastAsync(string roomCode, bool started, int remainingSeconds)
    {
        var message = new ServerMessage
        {
            Type = started ? "reactor_overload_started" : "reactor_overload_ended",
            RoomCode = roomCode,
            OverloadDuration = started ? remainingSeconds : null,
            Message = started
                ? "¡Sobrecarga! El reactor no dará puntos durante 10 segundos."
                : "Terminó la sobrecarga. El reactor volvió a estar disponible."
        };

        foreach (var connection in _groups.ObtenerConexionesSala(roomCode))
            await SendSafelyAsync(connection, message);
    }

    private async Task SendSafelyAsync(ClientConnection connection, ServerMessage message)
    {
        try
        {
            await connection.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error de comunicación de sobrecarga al cliente.");
        }
    }
}
