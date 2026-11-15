using ControlReactor.Negocio.Interfaces;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.Multijugador.Protocol;

namespace ControlReactor.Api.ServicioSegundoPlano;

public sealed class MonitorPuertasArena : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitorPuertasArena> _logger;
    private readonly ConnectionGroupManager _groupManager;
    private readonly Dictionary<int, CicloPuertasArena> _ciclos = new();

    public MonitorPuertasArena(
        IServiceScopeFactory scopeFactory,
        ILogger<MonitorPuertasArena> logger,
        ConnectionGroupManager groupManager)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _groupManager = groupManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogWarning("[Puertas] Monitor iniciado.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repositorio = scope.ServiceProvider
                        .GetRequiredService<IPartidaRepositorio>();

                    var partidas = await repositorio.ObtenerPartidasEnCursoAsync();
                    var activas = new HashSet<int>();
                    var ahora = DateTimeOffset.UtcNow;

                    foreach (var partida in partidas)
                    {
                        int id = partida.IdPartida;
                        activas.Add(id);

                        if (!_ciclos.TryGetValue(id, out var ciclo))
                        {
                            _ciclos[id] = new CicloPuertasArena(ahora);
                            _logger.LogInformation(
                                "[Puertas] Partida {Id}: ciclo iniciado.", id);
                            continue;
                        }

                        if (ciclo.Revisar(ahora) is bool cerrar)
                        {
                            _logger.LogInformation(
                                "[Puertas] Partida {Id}: solicitar {Accion}.",
                                id, cerrar ? "cierre" : "apertura");
                            var codigoSala = partida.CodigoSala;
                            if (string.IsNullOrWhiteSpace(codigoSala))
                                continue;

                            var mensaje = new ServerMessage
                            {
                                Type = "doors_command",
                                RoomCode = codigoSala,
                                SolicitarCierrePuertas = cerrar
                            };

                            var conexiones = _groupManager.ObtenerConexionesSala(codigoSala);

                            if (cerrar && partida.IdEscenario == 1)
                            {
                                _logger.LogInformation(
                                "[Puertas] Sala {Sala}: posiciones al cerrar: {Posiciones}",
                                codigoSala,
                                string.Join(" | ", conexiones.Select(j => $"X={j.X:F3}, Y={j.Y:F3}")));
                                var ocupadas = ZonasPuertasArena.Mapa1
                                    .Where(zona => conexiones.Any(j => zona.OcupadaPorJugador(j.X, j.Y)))
                                    .Select(zona => zona.Id)
                                    .ToArray();

                                _logger.LogInformation(
                                    "[Puertas] Sala {Sala}: puertas ocupadas al pedir cierre: {Puertas}",
                                    codigoSala,
                                    ocupadas.Length == 0 ? "ninguna" : string.Join(", ", ocupadas));
                            }

                            foreach (var conexion in conexiones)
                            {
                                
                                await conexion.SendAsync(mensaje);
                            }
                        }

                        if (partida.IdEscenario == 1 && !string.IsNullOrWhiteSpace(partida.CodigoSala))
                        {
                            var clientesMapa1 = _groupManager.ObtenerConexionesSala(partida.CodigoSala);
                            var cerradas = ciclo.ActualizarPuertasMapa1(zona =>
                                clientesMapa1.Any(j => zona.OcupadaPorJugador(j.X, j.Y)));

                            var estadoPuertas = new ServerMessage
                            {
                                Type = "doors_state",
                                RoomCode = partida.CodigoSala,
                                PuertasCerradas = cerradas
                            };

                            foreach (var cliente in clientesMapa1)
                                await cliente.SendAsync(estadoPuertas);
                        }
                    }

                    foreach (int id in _ciclos.Keys.ToArray())
                    {
                        if (!activas.Contains(id))
                            _ciclos.Remove(id);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error al revisar el ciclo de puertas.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // El servidor se está apagando.
        }
    }
}
