using ControlReactor.Negocio.Guardianes;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Api.Multijugador.Protocol;
using System.Diagnostics;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Api.Multijugador.Events;

namespace ControlReactor.Api.ServicioSegundoPlano
{
    public class MonitorGuardianes : BackgroundService
    {
        private readonly AdministradorGuardianesPorSala _administrador;
        private readonly ILogger<MonitorGuardianes> _logger;
        private readonly ConnectionGroupManager _groupManager;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly GuardianSurgeManager _surges;

    public MonitorGuardianes(
        AdministradorGuardianesPorSala administrador,
        ConnectionGroupManager groupManager,
        IServiceScopeFactory scopeFactory,
        ILogger<MonitorGuardianes> logger,
        GuardianSurgeManager surges)
    {
        _administrador = administrador;
        _groupManager = groupManager;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _surges = surges;
    }


    private ClientConnection? BuscarJugadorMasCercano(
        Guardian guardian,
        string codigoSala)
    {
        var jugadores = _groupManager.ObtenerJugadoresDisponibles(codigoSala);
        _logger.LogWarning(
            "Sala {Sala}: jugadores disponibles = {Cantidad}",
            codigoSala,
            jugadores.Count
        );
        ClientConnection? jugadorMasCercano = null;

        float distanciaMinima = float.MaxValue;
        const float RadioDeteccion = 3f;
        

        foreach (var jugador in jugadores)
        {
            float distancia = guardian.CalcularDistancia(
                jugador.X,
                jugador.Y
            );
            _logger.LogInformation(
                "Guardián {GuardianId}: ({GX}, {GY}) | Jugador {JugadorId}: ({JX}, {JY}) | Distancia: {Distancia}",
                guardian.Id,
                guardian.X,
                guardian.Y,
                jugador.PlayerId,
                jugador.X,
                jugador.Y,
                distancia
            );

            if (distancia <= RadioDeteccion && distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                jugadorMasCercano = jugador;
            }
        }

        return jugadorMasCercano;
    }

        
    private async Task ActualizarEstadoGuardianAsync(
        AdministradorGuardianes administradorSala,
        Guardian guardian,
        string codigoSala)
    {
        var jugador = BuscarJugadorMasCercano(
            guardian,
            codigoSala
        );

        // Si no encuentra jugadores, vuelve a patrullar.
        if (jugador == null)
        {
            if (guardian.EstadoActual is not EstadoPatrulla)
            {
                administradorSala.CambiarEstadoGuardian(
                    guardian,
                    new EstadoPatrulla(guardian.X, guardian.Y)
                );
            }

            return;
        }

        // Calcular la distancia al jugador.
        float distancia = guardian.CalcularDistancia(
            jugador.X,
            jugador.Y
        );

        const float DistanciaAtaque = 0.8f;
        double antiguedadPosicion =
            (DateTime.UtcNow - jugador.UltimaActualizacionPosicion)
            .TotalMilliseconds;

        
        // Si el guardián está suficientemente cerca, congela al jugador.
        if (distancia <= DistanciaAtaque)
        {
            _logger.LogWarning(
                "ATAQUE | Antigüedad posición jugador: {Tiempo:F0} ms",
                antiguedadPosicion
            );
            _logger.LogWarning(
            "CONGELAMIENTO BACKEND | " +
            "Guardian {GuardianId}: ({GX:F2}, {GY:F2}) | " +
            "Jugador {PlayerId}: ({JX:F2}, {JY:F2}) | " +
            "Distancia: {Distancia:F2}",
            guardian.Id,
            guardian.X,
            guardian.Y,
            jugador.PlayerId,
            jugador.X,
            jugador.Y,
            distancia
        );
            if (guardian.EstadoActual is not EstadoAtaque)
            {
                administradorSala.CambiarEstadoGuardian(
                    guardian,
                    new EstadoAtaque()
                );
            }

            bool congelado = _groupManager.CongelarJugador(
                codigoSala,
                jugador.PlayerId
            );

            if (congelado)
            {
                // Avisar inmediatamente al jugador que fue congelado.
                // Preparar la notificación de congelamiento.
                var mensajeCongelamiento = new ServerMessage
                {
                    Type = "player_frozen",
                    RoomCode = codigoSala,
                    PlayerId = jugador.PlayerId,
                    GuardianId = guardian.Id,
                    Message = "El guardián te ha congelado."
                };

                // Notificar primero al jugador afectado.
                await jugador.SendAsync(mensajeCongelamiento);

                // Notificar también a los demás jugadores de la sala.
                var conexionesCongelamiento =
                    _groupManager.ObtenerConexionesSala(codigoSala);

                foreach (var conexion in conexionesCongelamiento)
                {
                    if (conexion.PlayerId == jugador.PlayerId)
                        continue;

                    try
                    {
                        await conexion.SendAsync(mensajeCongelamiento);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "No se pudo comunicar el congelamiento al jugador {PlayerId}",
                            conexion.PlayerId
                        );
                    }
                }

                // Restar 10 puntos por el ataque.
                if (jugador.IdJugadorPartida.HasValue)
                {
                    using var scope = _scopeFactory.CreateScope();

                    var servicioPuntuacion = scope.ServiceProvider
                        .GetRequiredService<ServicioPuntuacion>();

                    int? puntajeActualizado =
                        await servicioPuntuacion.ModificarPuntaje(
                            jugador.IdJugadorPartida.Value,
                            -2
                        );
                    // Notificar a todos los jugadores el nuevo puntaje.
                    if (puntajeActualizado.HasValue && jugador.IdUsuario.HasValue)
                    {
                        var mensajePuntaje = new ServerMessage
                        {
                            Type = "score_updated",
                            RoomCode = codigoSala,
                            IdUsuario = jugador.IdUsuario.Value,
                            IdJugadorPartida = jugador.IdJugadorPartida.Value,
                            Puntaje = puntajeActualizado.Value
                        };

                        var conexiones = _groupManager.ObtenerConexionesSala(codigoSala);

                        foreach (var conexion in conexiones)
                        {
                            await conexion.SendAsync(mensajePuntaje);
                        }
                    }

                    _logger.LogInformation(
                        "Guardián {GuardianId} atacó al jugador {PlayerId}. Puntaje actualizado: {Puntaje}",
                        guardian.Id,
                        jugador.PlayerId,
                        puntajeActualizado
                    );
                }
            }

            return;
        }

        // Si todavía está lejos, continúa persiguiendo.
        if (guardian.EstadoActual is not EstadoPersecucion)
        {
            administradorSala.CambiarEstadoGuardian(
                guardian,
                new EstadoPersecucion()
            );
            _logger.LogWarning(
            "Guardián {IdGuardian} comenzó a perseguir al jugador {IdJugador} en la sala {Sala}",
            guardian.Id,
            jugador.PlayerId,
            codigoSala);
        }

        administradorSala.ActualizarObjetivoPersecucion(
            guardian,
            jugador.X,
            jugador.Y
        );
    }


        private async Task EnviarPosicionesGuardianesAsync(
            string codigoSala,
            AdministradorGuardianes administrador,
            CancellationToken cancellationToken)
        {
            // Obtener los jugadores conectados a la sala.
            var conexiones = _groupManager.ObtenerConexionesSala(codigoSala);

            if (conexiones.Count == 0)
            {
                return;
            }

            // Preparar el mensaje con las posiciones actuales.
            var mensaje = new ServerMessage
            {
                Type = "guardians_updated",
                RoomCode = codigoSala,

                Guardians = administrador.ObtenerGuardianes()
                    .Select(guardian => new PosicionGuardianServidor
                    {
                        Id = guardian.Id,
                        X = guardian.X,
                        Y = guardian.Y
                    })
                    .ToList()
            };
            // Enviar las posiciones a todos los jugadores.
            foreach (var conexion in conexiones)
            {
                var relojEnvio = Stopwatch.StartNew();

                await conexion.SendAsync(mensaje);

                relojEnvio.Stop();

                if (relojEnvio.ElapsedMilliseconds > 100)
                {
                    _logger.LogWarning(
                        "ENVIO GUARDIANES LENTO | Sala: {Sala} | Tiempo: {Tiempo} ms",
                        codigoSala,
                        relojEnvio.ElapsedMilliseconds
                    );
                }
            }
        }


        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(
                TimeSpan.FromMilliseconds(100));

            var reloj = Stopwatch.StartNew();
            double tiempoAnterior = reloj.Elapsed.TotalSeconds;

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    double tiempoActual = reloj.Elapsed.TotalSeconds;
                    double deltaReal = tiempoActual - tiempoAnterior;
                    tiempoAnterior = tiempoActual;

                    _logger.LogWarning(
                        "Tiempo real entre actualizaciones: {Tiempo:F3} segundos",
                        deltaReal
                    );
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();

                        var repositorio = scope.ServiceProvider
                            .GetRequiredService<IPartidaRepositorio>();

                        var partidas = await repositorio
                            .ObtenerPartidasEnCursoAsync();

                        // Retirar los guardianes temporales cuando termine su partida.
                        var idsActivas = partidas.Select(p => p.IdPartida).ToHashSet();
                        var finalizadas = _surges.RemoveFinishedMatches(
                            idsActivas, _administrador);

                        foreach (var oleada in finalizadas)
                            await ComunicarOleadaAsync(oleada, false);

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

                            // No procesar guardianes si no hay jugadores conectados.
                            var conexiones = _groupManager.ObtenerConexionesSala(
                                partida.CodigoSala
                            );

                            if (conexiones.Count == 0)
                            {
                                continue;
                            }

                            var administradorSala = _administrador.ObtenerOCrear(
                                partida.CodigoSala,
                                partida.IdEscenario
                            );

                            // El servidor activa y finaliza una única oleada por partida.
                            var cambioOleada = _surges.Advance(
                                partida, administradorSala, conexiones, DateTime.UtcNow);

                            if (cambioOleada != null)
                            {
                                _logger.LogInformation(
                                    "CA-22.2 | Sala {Sala} | Oleada {Estado} | Guardianes extra: {Cantidad}",
                                    partida.CodigoSala,
                                    cambioOleada.Started ? "INICIADA" : "FINALIZADA",
                                    cambioOleada.State.GuardianIds.Count);

                                await ComunicarOleadaAsync(
                                    cambioOleada.State, cambioOleada.Started);
                            }

                            // Actualizar el estado de cada guardián.
                            foreach (var guardian in administradorSala.ObtenerGuardianes())
                            {
                                await ActualizarEstadoGuardianAsync(
                                    administradorSala,
                                    guardian,
                                    partida.CodigoSala
                                );
                            }

                            // Mover los guardianes según su estado actual.
                            administradorSala.Actualizar(0.1f);

                            // Enviar las posiciones actualizadas a todos
                            // los jugadores conectados a esta sala.
                            await EnviarPosicionesGuardianesAsync(
                                partida.CodigoSala,
                                administradorSala,
                                stoppingToken
                            );
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

        private async Task ComunicarOleadaAsync(
            GuardianSurgeState state,
            bool started)
        {
            var mensaje = new ServerMessage
            {
                Type = started ? "guardian_surge_started" : "guardian_surge_ended",
                RoomCode = state.RoomCode,
                GuardianSurgeDuration = started
                    ? Math.Max(1, (int)Math.Ceiling(
                        (state.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds))
                    : null,
                Message = started
                    ? "¡Aparecieron 5 guardianes adicionales por 10 segundos!"
                    : "La oleada terminó. Los guardianes extra desaparecieron."
            };

            foreach (var conexion in _groupManager.ObtenerConexionesSala(state.RoomCode))
            {
                try
                {
                    await conexion.SendAsync(mensaje);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "No se pudo comunicar la oleada en la sala {Sala}", state.RoomCode);
                }
            }
        }
    }
}
