using ControlReactor.Negocio.Guardianes;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Api.ServicioSegundoPlano
{
    public class MonitorGuardianes : BackgroundService
    {
        private readonly AdministradorGuardianesPorSala _administrador;
        private readonly ILogger<MonitorGuardianes> _logger;
        private readonly ConnectionGroupManager _groupManager;
        private readonly IServiceScopeFactory _scopeFactory;

    public MonitorGuardianes(
        AdministradorGuardianesPorSala administrador,
        ConnectionGroupManager groupManager,
        IServiceScopeFactory scopeFactory,
        ILogger<MonitorGuardianes> logger)
    {
        _administrador = administrador;
        _groupManager = groupManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }


        private ClientConnection? BuscarJugadorMasCercano(
            Guardian guardian,
            string codigoSala)
        {
            var jugadores = _groupManager.ObtenerJugadoresDisponibles(codigoSala);

            ClientConnection? jugadorMasCercano = null;

            float distanciaMinima = float.MaxValue;

            foreach (var jugador in jugadores)
            {
                float distancia = guardian.CalcularDistancia(
                    jugador.X,
                    jugador.Y
                );

                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    jugadorMasCercano = jugador;
                }
            }

            return jugadorMasCercano;
        }

        private void ActualizarEstadoGuardian(
            Guardian guardian,
            string codigoSala)
        {
            var jugador = BuscarJugadorMasCercano(guardian,codigoSala);

            // Si no hay jugadores disponibles, patrulla.
            if (jugador == null)
            {
                if (guardian.EstadoActual is not EstadoPatrulla)
                {
                    guardian.CambiarEstado(new EstadoPatrulla());
                }

                return;
            }

            // Si hay un jugador disponible, lo persigue.
            if (guardian.EstadoActual is not EstadoPersecucion)
            {
                guardian.CambiarEstado(new EstadoPersecucion());
            }

            // Actualizar la posición del objetivo.
            if (guardian.EstadoActual is EstadoPersecucion persecucion)
            {
                persecucion.ObjetivoX = jugador.X;
                persecucion.ObjetivoY = jugador.Y;
            }
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
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var repositorio = scope.ServiceProvider
                            .GetRequiredService<IPartidaRepositorio>();

                        var partidas = await repositorio
                            .ObtenerPartidasEnCursoAsync();

                        foreach (var partida in partidas)
                        {
                            if (stoppingToken.IsCancellationRequested)
                            {
                                break;
                            }

                            if (string.IsNullOrWhiteSpace(partida.CodigoSala))
                            {
                                continue;
                            }

                            // Aquí actualizaremos los guardianes
                            // de esta partida.
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error al actualizar los guardianes.");
                    }
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Monitor de guardianes detenido.");
            }
        }
    }
}
