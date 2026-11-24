using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Negocio.Guardianes;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Tests;

// Pruebas UNITARIAS que completan los criterios del Sprint 3 sin cobertura previa.
public class PruebasSprint3Complementarias
{
    private static ClientConnection Suscrito(ConnectionGroupManager grupos, string playerId, int idUsuario,
        string sala = "SALA01")
    {
        var conexion = new ClientConnection(null!, playerId)
        {
            IdUsuario = idUsuario,
            IdJugadorPartida = idUsuario
        };
        Assert.True(grupos.Subscribe(conexion, sala, out _, out _, out string error), error);
        return conexion;
    }

    // ---------- US-20 Puertas y trampas ----------

    // CA-20.1, CA-20.8, CA-20.9 y CA-23.6 en los tres escenarios: tres trampas de tipos
    // distintos, fuera de los obstáculos y lejos del reactor.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void US20_Positiva_CadaEscenario_TresTrampasDistintasEnZonaTransitable(int escenario)
    {
        string mapas = AyudasPruebas.RutaMapas();
        var mapa = CargadorColisionesMapa.Cargar(Path.Combine(mapas, $"ColisionesMapa{escenario}.json"));
        ReactorConfiguration.TryGet(escenario, out var reactor);
        var partida = AyudasPruebas.PartidaEnCurso(10 + escenario, DateTime.UtcNow, idEscenario: escenario);

        var estado = new TrapManager(mapas).GetOrCreate(partida, AyudasPruebas.SinConexiones);

        Assert.NotNull(estado);
        Assert.Equal(3, estado!.Traps.Count);
        Assert.Equal(3, estado.Traps.Select(t => t.Kind).Distinct().Count());
        Assert.All(estado.Traps, t =>
        {
            Assert.False(DetectorColisionesMapa.PuntoEnObstaculo(t.X, t.Y, mapa));
            float distancia = MathF.Sqrt((t.X - reactor.X) * (t.X - reactor.X) + (t.Y - reactor.Y) * (t.Y - reactor.Y));
            Assert.True(distancia >= reactor.Radius);
        });
    }

    // CA-20.9: las trampas se colocan aleatoriamente (no siempre en el mismo sitio).
    [Fact]
    public void US20_Positiva_TrampasAleatorias_CambianEntrePartidas()
    {
        var gestor = new TrapManager(AyudasPruebas.RutaMapas());
        var posiciones = new HashSet<(float, float)>();

        for (int i = 0; i < 10; i++)
        {
            var estado = gestor.GetOrCreate(
                AyudasPruebas.PartidaEnCurso(100 + i, DateTime.UtcNow, codigoSala: "S" + i),
                AyudasPruebas.SinConexiones)!;
            posiciones.Add((estado.Traps[0].X, estado.Traps[0].Y));
        }

        Assert.True(posiciones.Count > 1);
    }

    // CA-22.8 / regresión: al terminar la partida se liberan las trampas.
    [Fact]
    public void US20_Positiva_PartidaTerminada_SeRetiranLasTrampas()
    {
        var gestor = new TrapManager(AyudasPruebas.RutaMapas());
        gestor.GetOrCreate(AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow), AyudasPruebas.SinConexiones);

        gestor.RemoveFinishedMatches(new HashSet<int>());

        Assert.Null(gestor.GetForRoom("123456"));
    }

    // ---------- US-21 Estados temporales y objetivo de los guardianes ----------

    // CA-21.4, CA-21.5, CA-21.7: congelar a un jugador conectado.
    [Fact]
    public void US21_Positiva_CongelarJugador_LoMarcaCongeladoDosSegundos()
    {
        var grupos = new ConnectionGroupManager();
        var jugador = Suscrito(grupos, "p1", 1);

        bool congelado = grupos.CongelarJugador("sala01", "p1");

        Assert.True(congelado);
        Assert.True(jugador.EstaCongelado());
        Assert.InRange((jugador.CongeladoHasta!.Value - DateTime.UtcNow).TotalSeconds, 1.5, 2.1);
    }

    [Fact]
    public void US21_Negativa_CongelarJugadorInexistente_NoHaceNada()
    {
        var grupos = new ConnectionGroupManager();
        Suscrito(grupos, "p1", 1);

        Assert.False(grupos.CongelarJugador("SALA01", "noexiste"));
        Assert.False(grupos.CongelarJugador("OTRA", "p1"));
        Assert.False(grupos.CongelarJugador("", "p1"));
    }

    // CA-21.11: el guardián busca a otro jugador si el más cercano ya está congelado.
    [Fact]
    public void US21_Positiva_JugadorCongelado_NoEsObjetivoDisponible()
    {
        var grupos = new ConnectionGroupManager();
        var p1 = Suscrito(grupos, "p1", 1);
        var p2 = Suscrito(grupos, "p2", 2);
        grupos.TryUpdatePosition(p1, 1f, 1f, out _, out _);
        grupos.TryUpdatePosition(p2, 5f, 5f, out _, out _);

        grupos.CongelarJugador("SALA01", "p1");
        var disponibles = grupos.ObtenerJugadoresDisponibles("SALA01");

        var objetivo = Assert.Single(disponibles);
        Assert.Equal("p2", objetivo.PlayerId);
        Assert.False(grupos.ObtenerPosicionJugador("SALA01", "p1", out _, out _));
    }

    // CA-21.6: al vencer el tiempo el jugador recupera el movimiento en el servidor.
    [Fact]
    public void US21_Positiva_CongelamientoVencido_PermiteMoverDeNuevo()
    {
        var grupos = new ConnectionGroupManager();
        var p1 = Suscrito(grupos, "p1", 1);
        p1.CongeladoHasta = DateTime.UtcNow.AddSeconds(-0.1);

        bool movio = grupos.TryUpdatePosition(p1, 3f, 3f, out var estado, out _);

        Assert.True(movio);
        Assert.Equal(3f, estado!.X);
    }

    // CA-21.1: los guardianes temporales se eliminan sin tocar los cuatro fijos.
    [Fact]
    public void US21_Positiva_EliminarTemporales_ConservaLosCuatroFijos()
    {
        var admin = new AdministradorGuardianes();
        admin.CrearGuardianes(new (float, float)[] { (0, 0), (1, 0), (0, 1), (1, 1) });
        var temporales = admin.AgregarGuardianesTemporales(new (float, float)[] { (2, 2), (3, 3), (4, 4), (5, 5), (6, 6) });

        admin.EliminarGuardianesTemporales(temporales);

        Assert.Equal(new[] { 1, 2, 3, 4 }, admin.ObtenerGuardianes().Select(g => g.Id).ToArray());
    }

    [Fact]
    public void US22_Negativa_OleadaConCantidadIncorrecta_SeRechaza()
    {
        var admin = new AdministradorGuardianes();
        admin.CrearGuardianes(new (float, float)[] { (0, 0), (1, 0), (0, 1), (1, 1) });

        Assert.Throws<ArgumentException>(() =>
            admin.AgregarGuardianesTemporales(new (float, float)[] { (2, 2) }));
        Assert.Equal(4, admin.ObtenerGuardianes().Count);
    }

    // ---------- US-22 Eventos especiales ----------

    // CA-22.6: la sobrecarga solo afecta a la sala de su partida.
    [Fact]
    public void US22_Negativa_Sobrecarga_NoAfectaOtraSala()
    {
        var gestor = new ReactorOverloadManager();
        gestor.Advance(AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow.AddSeconds(-100)), DateTime.UtcNow);

        Assert.True(gestor.IsOverloaded("123456"));
        Assert.False(gestor.IsOverloaded("999999"));
    }

    // CA-22.8: al terminar la partida se cancela la sobrecarga activa.
    [Fact]
    public void US22_Positiva_PartidaTerminada_CancelaSobrecargaActiva()
    {
        var gestor = new ReactorOverloadManager();
        gestor.Advance(AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow.AddSeconds(-100)), DateTime.UtcNow);

        var terminadas = gestor.RemoveFinishedMatches(new HashSet<int>());

        Assert.Single(terminadas);
        Assert.False(gestor.IsOverloaded("123456"));
    }

    // CA-22.7: la energía adicional desaparece al vencer y ya no puede recogerse.
    [Fact]
    public void US22_Positiva_EnergiaAdicional_ExpiraYNoPuedeRecogerse()
    {
        var gestor = new EnergyBonusManager(AyudasPruebas.RutaMapas());
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-46));
        var bonus = gestor.Advance(partida, AyudasPruebas.SinConexiones, ahora)!.Bonus;

        var fin = gestor.Advance(partida, AyudasPruebas.SinConexiones,
            ahora.AddSeconds(EnergyBonusManager.DurationSeconds + 1));

        Assert.NotNull(fin);
        Assert.False(fin!.Spawned);
        Assert.False(gestor.TryReserve("123456", bonus.BonusId, bonus.X, bonus.Y, out _));
    }

    // CA-22.9: una reserva liberada (error al guardar) puede volver a intentarse una sola vez.
    [Fact]
    public void US22_Positiva_EnergiaAdicional_ReservaLiberadaPermiteReintentoUnico()
    {
        var gestor = new EnergyBonusManager(AyudasPruebas.RutaMapas());
        var ahora = DateTime.UtcNow;
        var bonus = gestor.Advance(AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-46)),
            AyudasPruebas.SinConexiones, ahora)!.Bonus;

        gestor.TryReserve("123456", bonus.BonusId, bonus.X, bonus.Y, out var reservado);
        gestor.Release(reservado!);
        bool reintento = gestor.TryReserve("123456", bonus.BonusId, bonus.X, bonus.Y, out reservado);
        gestor.Complete(reservado!);

        Assert.True(reintento);
        Assert.Null(gestor.GetActiveForRoom("123456"));
    }

    // CA-22.7 (doble puntaje creativo): al terminar se restaura el puntaje normal.
    [Fact]
    public void US22_Positiva_DoblePuntaje_TerminaYRestauraPuntajeNormal()
    {
        var gestor = new CreativeEventsManager(AyudasPruebas.RutaMapas());
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-200));
        var t = ahora;

        gestor.Advance(partida, AyudasPruebas.SinConexiones, t);                 // arena inicia
        t = t.AddSeconds(CreativeEventsManager.SandDurationSeconds + 1);
        gestor.Advance(partida, AyudasPruebas.SinConexiones, t);                 // arena termina
        gestor.Advance(partida, AyudasPruebas.SinConexiones, t);                 // doble inicia
        Assert.True(gestor.IsDoublePoints("123456"));
        Assert.False(gestor.IsDoublePoints("999999"));

        var fin = gestor.Advance(partida, AyudasPruebas.SinConexiones,
            t.AddSeconds(CreativeEventsManager.DoubleDurationSeconds + 1));

        Assert.Equal(CreativeEventKind.DoublePoints, fin!.State.Kind);
        Assert.False(fin.Started);
        Assert.False(gestor.IsDoublePoints("123456"));
    }

    // CA-22.8: ningún evento arranca cuando ya se agotó el tiempo de la partida.
    [Fact]
    public void US22_Negativa_TiempoAgotado_NoIniciaNingunEvento()
    {
        string mapas = AyudasPruebas.RutaMapas();
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-400));
        var guardianes = new AdministradorGuardianesPorSala(mapas).ObtenerOCrear("123456", 1);

        Assert.Null(new ReactorOverloadManager().Advance(partida, ahora));
        Assert.Null(new CreativeEventsManager(mapas).Advance(partida, AyudasPruebas.SinConexiones, ahora));
        Assert.Null(new GuardianSurgeManager(mapas).Advance(partida, guardianes, AyudasPruebas.SinConexiones, ahora));
        Assert.Equal(4, guardianes.ObtenerGuardianes().Count);
    }

    // ---------- US-23 Escenarios ----------

    // CA-23.1, CA-23.2: cada escenario tiene su propio mapa de colisiones y reactor distinto.
    [Fact]
    public void US23_Positiva_EscenariosConMapasYReactoresDistintos()
    {
        string mapas = AyudasPruebas.RutaMapas();
        var reactores = new List<(float, float)>();

        for (int e = 1; e <= 3; e++)
        {
            var mapa = CargadorColisionesMapa.Cargar(Path.Combine(mapas, $"ColisionesMapa{e}.json"));
            Assert.Equal(e, mapa.IdEscenario);
            Assert.NotEmpty(mapa.Obstaculos);
            Assert.True(ReactorConfiguration.TryGet(e, out var r));
            reactores.Add((r.X, r.Y));
        }

        Assert.Equal(3, reactores.Distinct().Count());
    }

    // CA-23.4: dos salas con distinto escenario no comparten guardianes.
    [Fact]
    public void US23_Positiva_SalasDistintas_TienenGuardianesIndependientes()
    {
        var porSala = new AdministradorGuardianesPorSala(AyudasPruebas.RutaMapas());

        var a = porSala.ObtenerOCrear("SALAA", 1);
        var b = porSala.ObtenerOCrear("SALAB", 2);

        Assert.NotSame(a, b);
    }

    // ---------- US-31 Reglas de conexión a la sala ----------

    [Fact]
    public void US31_Negativa_CuartoJugador_SeRechaza()
    {
        var grupos = new ConnectionGroupManager();
        Suscrito(grupos, "p1", 1);
        Suscrito(grupos, "p2", 2);
        Suscrito(grupos, "p3", 3);
        var cuarto = new ClientConnection(null!, "p4") { IdUsuario = 4, IdJugadorPartida = 4 };

        bool ok = grupos.Subscribe(cuarto, "SALA01", out _, out _, out string error);

        Assert.False(ok);
        Assert.Equal("La sala ya tiene 3 jugadores conectados.", error);
    }

    // CA-30.7, CA-31.4: el mismo usuario no puede ocupar dos conexiones (p. ej. PC y móvil).
    [Fact]
    public void US31_Negativa_MismoUsuarioDosConexiones_SeRechaza()
    {
        var grupos = new ConnectionGroupManager();
        Suscrito(grupos, "pc", 1);
        var movil = new ClientConnection(null!, "movil") { IdUsuario = 1, IdJugadorPartida = 1 };

        bool ok = grupos.Subscribe(movil, "SALA01", out _, out _, out string error);

        Assert.False(ok);
        Assert.Equal("Este usuario ya esta conectado a la sala.", error);
    }

    [Fact]
    public void US31_Negativa_ConexionSinIdentidad_SeRechaza()
    {
        var grupos = new ConnectionGroupManager();
        var anonima = new ClientConnection(null!, "anon");

        bool ok = grupos.Subscribe(anonima, "SALA01", out _, out _, out string error);

        Assert.False(ok);
        Assert.Equal("El jugador no esta identificado.", error);
        Assert.Null(anonima.RoomCode);
    }

    // Regresión: al salir un jugador, los demás siguen en la sala.
    [Fact]
    public void US31_Positiva_JugadorSale_LosDemasPermanecen()
    {
        var grupos = new ConnectionGroupManager();
        var p1 = Suscrito(grupos, "p1", 1);
        Suscrito(grupos, "p2", 2);

        bool ok = grupos.RemovePlayer(p1, out var salio, out var restantes, out var sala);

        Assert.True(ok);
        Assert.Equal("p1", salio!.PlayerId);
        Assert.Equal("SALA01", sala);
        Assert.Equal("p2", Assert.Single(restantes).PlayerId);
    }
}
