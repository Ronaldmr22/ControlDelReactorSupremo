using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Api.Multijugador.Protocol;

namespace ControlReactor.Api.ServicioSegundoPlano;

public sealed class TrapsWorker : BackgroundService
{
    private readonly TrapManager _traps;
    private readonly ReactorOverloadManager _overloads;
    private readonly ConnectionGroupManager _groups;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrapsWorker> _logger;

    public TrapsWorker(
        TrapManager traps,
        ReactorOverloadManager overloads,
        ConnectionGroupManager groups,
        IServiceScopeFactory scopeFactory,
        ILogger<TrapsWorker> logger)
    {
        _traps = traps;
        _overloads = overloads;
        _groups = groups;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(300));

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
                        .Where(p =>
                            p.Estado == "EN_CURSO" &&
                            p.CodigoSala != null)
                        .ToListAsync(stoppingToken);

                    _traps.RemoveFinishedMatches(
                        matches.Select(m => m.IdPartida)
                            .ToHashSet());

                    foreach (var match in matches)
                    {
                        string roomCode = match.CodigoSala!;

                        var connections =
                            _groups.ObtenerConexionesSala(roomCode);

                        if (connections.Count == 0)
                            continue;

                        // Evita generar nuevamente las trampas.
                        if (_traps.GetForRoom(roomCode) != null)
                            continue;

                        TrapMatchState? state;

                        using (await _overloads.EnterRoomGateAsync(
                            roomCode, stoppingToken))
                        {
                            state = _traps.GetOrCreate(
                                match, connections);
                        }

                        if (state == null)
                            continue;

                        _logger.LogInformation(
                            "Trampas generadas en sala {Room}",
                            roomCode);

                        var mensaje = new ServerMessage
                        {
                            Type = "traps_spawned",
                            RoomCode = roomCode,
                            Traps = state.Traps.ToList(),
                            Message = "¡Las trampas aparecieron en la arena!"
                        };

                        foreach (var connection in _groups.ObtenerConexionesSala(roomCode))
                        {
                            try
                            {
                                await connection.SendAsync(mensaje);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(
                                    ex,
                                    "No se pudieron enviar las trampas a la sala {Room}",
                                    roomCode);
                            }
                        }
                    }
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
                        "Error al generar las trampas.");
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal.
        }
    }
}
