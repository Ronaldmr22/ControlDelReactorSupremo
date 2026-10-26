using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.Multijugador.Protocol;

namespace ControlReactor.Api.ServicioSegundoPlano
{
    public class MonitorPartidas : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MonitorPartidas> _logger;
        private readonly ConnectionGroupManager _groupManager;

        public MonitorPartidas(
            IServiceScopeFactory scopeFactory,
            ILogger<MonitorPartidas> logger,
            ConnectionGroupManager groupManager)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _groupManager = groupManager;
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
                            
                        var repositorioJugadores = scope.ServiceProvider
                            .GetRequiredService<IJugadorPartidaRepositorio>();

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

                                // Consultar nuevamente el estado de la partida.
                                var partidaActualizada = await repositorio
                                    .ObtenerPorIdAsync(partida.IdPartida);

                                // Solo notificar si realmente finalizo.
                                // Si hubo empate, la partida sigue EN_CURSO.
                                if (partidaActualizada?.Estado == "FINALIZADA" && !string.IsNullOrWhiteSpace(partida.CodigoSala))
                                {
                                    var conexiones = _groupManager
                                        .ObtenerConexionesSala(partida.CodigoSala);

                                    var jugadores = await repositorioJugadores.ObtenerPorPartidaAsync(partida.IdPartida);

                                    var ganador = jugadores.FirstOrDefault(j => j.EsGanador);

                                    var mensaje = new ServerMessage
                                    {
                                        Type = "game_finished",
                                        RoomCode = partida.CodigoSala,
                                        IdJugadorPartida = ganador?.IdJugadorPartida,
                                        IdUsuario = ganador?.IdUsuario,
                                        Puntaje = ganador?.Puntaje
                                    };

                                    foreach (var conexion in conexiones)
                                    {
                                        await conexion.SendAsync(mensaje);
                                    }
                                }
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