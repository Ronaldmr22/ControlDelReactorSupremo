using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;

namespace ControlReactor.Api.Servicios
{
    public class MonitorPartidas : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MonitorPartidas> _logger;

        public MonitorPartidas(
            IServiceScopeFactory scopeFactory,
            ILogger<MonitorPartidas> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(2));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var repositorio = scope.ServiceProvider
                            .GetRequiredService<IPartidaRepositorio>();

                        var servicioGanador = scope.ServiceProvider
                            .GetRequiredService<ServicioGanador>();

                        var partidas = await repositorio
                            .ObtenerPartidasEnCursoAsync();

                        foreach (var partida in partidas)
                        {
                            if (stoppingToken.IsCancellationRequested)
                                break;

                            if (await servicioGanador
                                .DebeFinalizarAsync(partida.IdPartida))
                            {
                                string resultado = await servicioGanador
                                    .FinalizarPartidaAsync(partida.IdPartida);

                                _logger.LogInformation(
                                    "Partida {IdPartida}: {Resultado}",
                                    partida.IdPartida,
                                    resultado);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error al revisar las partidas.");
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // El servidor se está apagando normalmente.
            }
        }
    }
}