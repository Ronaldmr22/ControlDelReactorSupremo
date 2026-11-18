using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Api.ServicioSegundoPlano;

// Genera exactamente tres apariciones mientras la partida siga activa.
public sealed class EnergyBonusWorker : BackgroundService
{
    private readonly EnergyBonusManager _bonuses;
    private readonly ConnectionGroupManager _groups;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnergyBonusWorker> _logger;

    public EnergyBonusWorker(
        EnergyBonusManager bonuses,
        ConnectionGroupManager groups,
        IServiceScopeFactory scopeFactory,
        ILogger<EnergyBonusWorker> logger)
    {
        _bonuses = bonuses;
        _groups = groups;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

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

                    var activeIds = matches.Select(p => p.IdPartida).ToHashSet();

                    foreach (var removed in _bonuses.RemoveFinishedMatches(activeIds))
                        await BroadcastAsync(removed.RoomCode, removed, false);

                    DateTime now = DateTime.UtcNow;

                    foreach (var match in matches)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        var connections = _groups.ObtenerConexionesSala(match.CodigoSala!);
                        if (connections.Count == 0)
                            continue;

                        var change = _bonuses.Advance(match, connections, now);
                        if (change != null)
                            await BroadcastAsync(change.Bonus.RoomCode, change.Bonus, change.Spawned);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error en el evento de energía adicional.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Fin normal del servicio.
        }
    }

    private async Task BroadcastAsync(
        string roomCode,
        EnergyBonusState bonus,
        bool spawned)
    {
        var message = new ServerMessage
        {
            Type = spawned ? "energy_bonus_spawned" : "energy_bonus_removed",
            RoomCode = roomCode,
            BonusId = bonus.BonusId,
            X = bonus.X,
            Y = bonus.Y,
            BonusDuration = spawned ? EnergyBonusManager.DurationSeconds : null,
            Message = spawned
                ? "¡Apareció una cápsula de energía adicional!"
                : "La cápsula de energía desapareció."
        };

        foreach (var connection in _groups.ObtenerConexionesSala(roomCode))
        {
            try
            {
                await connection.SendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo comunicar el evento a la sala {Sala}", roomCode);
            }
        }
    }
}
