using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Api.ServicioSegundoPlano;

public sealed class CreativeEventsWorker : BackgroundService
{
    private readonly CreativeEventsManager _events;
    private readonly ReactorOverloadManager _overloads;
    private readonly ConnectionGroupManager _groups;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CreativeEventsWorker> _logger;

    public CreativeEventsWorker(CreativeEventsManager events,
        ReactorOverloadManager overloads,
        ConnectionGroupManager groups,
        IServiceScopeFactory scopeFactory,
        ILogger<CreativeEventsWorker> logger)
    {
        _events = events;
        _overloads = overloads;
        _groups = groups;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(300));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ControlReactorDbContext>();
                    var matches = await db.Partidas.AsNoTracking()
                        .Where(p => p.Estado == "EN_CURSO" && p.CodigoSala != null)
                        .ToListAsync(stoppingToken);

                    foreach (var ended in _events.RemoveFinishedMatches(
                        matches.Select(m => m.IdPartida).ToHashSet()))
                        await BroadcastAsync(new CreativeEventChange(false, ended));

                    foreach (var match in matches)
                    {
                        string roomCode = match.CodigoSala!;
                        var connections = _groups.ObtenerConexionesSala(roomCode);
                        if (connections.Count == 0)
                            continue;

                        CreativeEventChange? change;
                        // Misma puerta que la transacción que asigna puntos de extracción.
                        using (await _overloads.EnterRoomGateAsync(roomCode, stoppingToken))
                        {
                            change = _events.Advance(match, connections, DateTime.UtcNow);
                        }

                        if (change == null)
                            continue;

                        _logger.LogWarning(
                            "CA-22.4 | Sala {Room} | {Kind} {State}",
                            roomCode, change.State.Kind,
                            change.Started ? "INICIADO" : "FINALIZADO");

                        await BroadcastAsync(change);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en eventos creativos CA-22.4.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal.
        }
    }

    private async Task BroadcastAsync(CreativeEventChange change)
    {
        var state = change.State;
        int remaining = change.Started
            ? Math.Max(1, (int)Math.Ceiling((state.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds))
            : 0;

        var message = new ServerMessage
        {
            Type = state.Kind == CreativeEventKind.Sand
                ? (change.Started ? "creative_sand_started" : "creative_sand_ended")
                : (change.Started ? "reactor_double_started" : "reactor_double_ended"),
            RoomCode = state.RoomCode,
            CreativeDuration = change.Started ? remaining : null,
            SandZones = change.Started && state.Kind == CreativeEventKind.Sand
                ? state.SandZones.ToList() : null,
            Message = state.Kind == CreativeEventKind.Sand
                ? (change.Started ? "¡Cuatro arenas movedizas por 20 segundos!" : "Terminaron las arenas movedizas.")
                : (change.Started ? "¡El reactor otorga puntos dobles por 15 segundos!" : "Terminó el multiplicador x2.")
        };

        foreach (var connection in _groups.ObtenerConexionesSala(state.RoomCode))
        {
            try
            {
                await connection.SendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error de envío CA-22.4 a sala {Room}", state.RoomCode);
            }
        }
    }
}
