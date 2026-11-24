using System.Reflection;
using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Api.Multijugador.Networking;
using ControlReactor.Api.Multijugador.Protocol;
using ControlReactor.Datos.Contexto;
using ControlReactor.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ControlReactor.Tests;

// Pruebas de INTEGRACIÓN del servidor de tiempo real (US-18, US-19, US-21, US-22, US-31).
// Se usa el WebSocketHandler real con sus gestores reales y EF Core InMemory.
// Los clientes son WebSocketFalso, que guardan lo que el servidor les envía.
public class PruebasIntegracionWebSocket
{
    private const string Sala = "ABC123";

    private readonly DbContextOptions<ControlReactorDbContext> _opciones;
    private readonly ConnectionGroupManager _grupos = new();
    private readonly ExtractionSessionManager _sesiones = new();
    private readonly ReactorOverloadManager _sobrecargas = new();
    private readonly WebSocketHandler _handler;
    private readonly MethodInfo _procesar;
    private readonly ReactorData _reactor;

    public PruebasIntegracionWebSocket()
    {
        _opciones = new DbContextOptionsBuilder<ControlReactorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var servicios = new ServiceCollection();
        servicios.AddScoped(_ => new ControlReactorDbContext(_opciones));
        var proveedor = servicios.BuildServiceProvider();

        string mapas = AyudasPruebas.RutaMapas();
        _handler = new WebSocketHandler(
            _grupos,
            proveedor.GetRequiredService<IServiceScopeFactory>(),
            _sesiones,
            new EnergyBonusManager(mapas),
            new GuardianSurgeManager(mapas),
            _sobrecargas,
            new CreativeEventsManager(mapas),
            new TrapManager(mapas));

        _procesar = typeof(WebSocketHandler).GetMethod(
            "ProcessMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException("WebSocketHandler.ProcessMessageAsync");

        ReactorConfiguration.TryGet(1, out _reactor);

        using var db = new ControlReactorDbContext(_opciones);
        db.Partidas.Add(new Partida
        {
            IdPartida = 1,
            CodigoSala = Sala,
            IdEscenario = 1,
            Estado = "EN_CURSO",
            TipoFinalizacion = "TIEMPO",
            LimiteTiempo = 300,
            FechaInicio = DateTime.UtcNow,
            EnergiaRestante = 100
        });
        db.JugadoresPartida.AddRange(
            new JugadorPartida { IdJugadorPartida = 1, IdUsuario = 10, IdPartida = 1, EnSala = true },
            new JugadorPartida { IdJugadorPartida = 2, IdUsuario = 11, IdPartida = 1, EnSala = true });
        db.SaveChanges();
    }

    // ---------- utilidades ----------

    private Task EnviarAsync(ClientConnection conexion, ClientMessage mensaje) =>
        (Task)_procesar.Invoke(_handler, new object[] { conexion, mensaje })!;

    private async Task<(ClientConnection Conexion, WebSocketFalso Socket)> ConectarAsync(
        string playerId, int idUsuario, string sala = Sala)
    {
        var socket = new WebSocketFalso();
        var conexion = new ClientConnection(socket, playerId);
        await EnviarAsync(conexion, new ClientMessage
        {
            Type = "subscribe_room", RoomCode = sala, IdUsuario = idUsuario, PlayerName = playerId
        });
        return (conexion, socket);
    }

    private Task MoverAsync(ClientConnection c, float x, float y) =>
        EnviarAsync(c, new ClientMessage { Type = "move", X = x, Y = y });

    private static ClientMessage Extraer(string? id = null) => new()
    {
        Type = "extract_start", ReactorId = "central", ExtractionId = id ?? Guid.NewGuid().ToString("N")
    };

    private void ModificarPartida(Action<Partida> cambio)
    {
        using var db = new ControlReactorDbContext(_opciones);
        cambio(db.Partidas.Single(p => p.IdPartida == 1));
        db.SaveChanges();
    }

    // ---------- Suscripción a la sala ----------

    // INT-31.1 / CA-31.2, CA-18.7: dos jugadores en la misma partida reciben el estado inicial.
    [Fact]
    public async Task INT31_Positiva_DosJugadores_SeSuscribenYRecibenEstado()
    {
        var (_, socketA) = await ConectarAsync("pA", 10);
        var (_, socketB) = await ConectarAsync("pB", 11);

        Assert.Contains("room_subscribed", socketA.Tipos);
        Assert.Contains("reactor_state", socketA.Tipos);
        Assert.Equal(2, socketA.DeTipo("score_updated").Count());
        Assert.Contains(socketA.DeTipo("player_joined"), m => m.GetProperty("playerId").GetString() == "pB");
        Assert.Contains(socketB.DeTipo("player_joined"), m => m.GetProperty("playerId").GetString() == "pA");
        Assert.Equal(2, _grupos.ObtenerConexionesSala(Sala).Count);
    }

    // INT-31.2 / CA-29.7, CA-31.1: sala inexistente.
    [Fact]
    public async Task INT31_Negativa_SalaInexistente_SeRechaza()
    {
        var (conexion, socket) = await ConectarAsync("pA", 10, "NOEXISTE");

        Assert.Equal("La sala no existe.", socket.UltimoError());
        Assert.Null(conexion.RoomCode);
    }

    // INT-31.3 / CA-29.7: un usuario que no participa no puede entrar a la partida.
    [Fact]
    public async Task INT31_Negativa_UsuarioQueNoParticipa_SeRechaza()
    {
        var (conexion, socket) = await ConectarAsync("pX", 99);

        Assert.Equal("El usuario no pertenece a esta sala.", socket.UltimoError());
        Assert.Empty(_grupos.ObtenerConexionesSala(Sala));
        Assert.Null(conexion.IdJugadorPartida);
    }

    // INT-31.4: mensajes desconocidos se responden con error comprensible (CA-24.6).
    [Fact]
    public async Task INT31_Negativa_TipoDeMensajeDesconocido_DevuelveError()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);

        await EnviarAsync(a, new ClientMessage { Type = "hackear_puntos" });

        Assert.Equal("Tipo de mensaje desconocido.", socketA.UltimoError());
    }

    // ---------- Movimiento ----------

    // INT-18.1 / CA-18.7, CA-19.4: el movimiento llega a los demás sin recargar
    // y NO genera puntos.
    [Fact]
    public async Task INT19_Positiva_Movimiento_SeRetransmiteYNoOtorgaPuntos()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        var (_, socketB) = await ConectarAsync("pB", 11);
        socketA.Limpiar();
        socketB.Limpiar();

        await MoverAsync(a, 1f, 2f);
        await MoverAsync(a, 1.5f, 2f);
        await MoverAsync(a, 2f, 2f);

        var movimientos = socketB.DeTipo("player_moved").ToList();
        Assert.Equal(3, movimientos.Count);
        Assert.Equal("pA", movimientos[^1].GetProperty("playerId").GetString());
        Assert.Equal(2f, movimientos[^1].GetProperty("x").GetSingle());
        Assert.Empty(socketA.DeTipo("player_moved")); // no se reenvía al emisor

        using var db = new ControlReactorDbContext(_opciones);
        Assert.Equal(0, db.JugadoresPartida.Single(j => j.IdJugadorPartida == 1).Puntaje);
    }

    // INT-21.1 / CA-21.5, CA-21.8: un jugador congelado no puede moverse aunque el cliente lo envíe.
    [Fact]
    public async Task INT21_Negativa_JugadorCongelado_ServidorIgnoraElMovimiento()
    {
        var (a, _) = await ConectarAsync("pA", 10);
        var (_, socketB) = await ConectarAsync("pB", 11);
        await MoverAsync(a, 1f, 1f);
        socketB.Limpiar();

        a.CongeladoHasta = DateTime.UtcNow.AddSeconds(2);
        await MoverAsync(a, 5f, 5f);

        Assert.Empty(socketB.DeTipo("player_moved"));
        Assert.Equal(1f, a.X);
        Assert.Equal(1f, a.Y);
    }

    // INT-21.2: coordenadas no numéricas se rechazan (CA-20.8: posición inválida).
    [Fact]
    public async Task INT20_Negativa_CoordenadasNoValidas_SeRechazan()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);

        await MoverAsync(a, float.NaN, 0f);

        Assert.Equal("Las coordenadas no son validas.", socketA.UltimoError());
        Assert.False(a.HasReceivedPosition);
    }

    // ---------- Extracción del reactor ----------

    // INT-18.2 / CA-18.2, CA-18.3, CA-18.9: dentro de la zona válida se registra la extracción
    // con la identidad del jugador.
    [Fact]
    public async Task INT18_Positiva_JugadorEnZonaDelReactor_IniciaExtraccion()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        await MoverAsync(a, _reactor.X, _reactor.Y);

        await EnviarAsync(a, Extraer());

        Assert.Contains("extract_started", socketA.Tipos);
        var sesion = Assert.Single(_sesiones.GetActiveSessions());
        Assert.Equal(1, sesion.PlayerMatchId);
        Assert.Equal(Sala, sesion.RoomCode);
    }

    // INT-18.3 / CA-18.5: fuera de la zona de interacción se rechaza e informa el motivo.
    [Fact]
    public async Task INT18_Negativa_FueraDeLaZona_SeRechazaConMotivo()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        await MoverAsync(a, _reactor.X + _reactor.Radius + 1f, _reactor.Y);

        await EnviarAsync(a, Extraer());

        Assert.Equal("Estás demasiado lejos del reactor.", socketA.UltimoError());
        Assert.Empty(_sesiones.GetActiveSessions());
    }

    // INT-18.4 / CA-18.3: sin posición conocida el servidor no confía en el cliente.
    [Fact]
    public async Task INT18_Negativa_SinPosicionConocida_SeRechaza()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);

        await EnviarAsync(a, Extraer());

        Assert.Equal("Todavía no se conoce tu posición.", socketA.UltimoError());
        Assert.Empty(_sesiones.GetActiveSessions());
    }

    // INT-18.5 / CA-18.5, CA-25.3: partida no activa -> no hay acciones competitivas.
    [Fact]
    public async Task INT25_Negativa_PartidaFinalizada_RechazaExtraccion()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        await MoverAsync(a, _reactor.X, _reactor.Y);
        ModificarPartida(p => { p.Estado = "FINALIZADA"; p.FechaFin = DateTime.UtcNow; });

        await EnviarAsync(a, Extraer());

        Assert.Equal("La partida no está en curso.", socketA.UltimoError());
        Assert.Empty(_sesiones.GetActiveSessions());
    }

    // INT-18.6 / CA-18.1, CA-18.5: reactor sin energía (recargando) no está disponible.
    [Fact]
    public async Task INT18_Negativa_ReactorRecargando_SeRechaza()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        await MoverAsync(a, _reactor.X, _reactor.Y);
        ModificarPartida(p =>
        {
            p.EnergiaRestante = 0;
            p.ReactorRechargeReadyAtUtc = DateTime.UtcNow.AddSeconds(15);
        });

        await EnviarAsync(a, Extraer());

        Assert.Contains("recargando", socketA.UltimoError());
        Assert.Empty(_sesiones.GetActiveSessions());
    }

    // INT-22.1 / CA-22.3: durante la sobrecarga el reactor no entrega puntos.
    [Fact]
    public async Task INT22_Negativa_SobrecargaActiva_RechazaExtraccion()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        await MoverAsync(a, _reactor.X, _reactor.Y);

        // Partida equivalente iniciada hace 100 s: ya pasó la primera ventana (10-25 %).
        var cambio = _sobrecargas.Advance(
            AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow.AddSeconds(-100), codigoSala: Sala),
            DateTime.UtcNow);
        Assert.True(cambio is { Started: true });

        await EnviarAsync(a, Extraer());

        Assert.Contains("sobrecarga", socketA.UltimoError());
        Assert.Empty(_sesiones.GetActiveSessions());
    }

    // INT-18.7 / CA-18.8: el mismo evento físico retransmitido no se contabiliza dos veces.
    [Fact]
    public async Task INT18_Negativa_EventoFisicoRetransmitido_SeRechazaComoDuplicado()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        await MoverAsync(a, _reactor.X, _reactor.Y);
        string id = Guid.NewGuid().ToString("N");

        await EnviarAsync(a, Extraer(id));
        _sesiones.TryStop(a, id);
        await EnviarAsync(a, Extraer(id));

        Assert.Equal("Solicitud de extracción duplicada.", socketA.UltimoError());
        Assert.Empty(_sesiones.GetActiveSessions());
    }

    // INT-18.8: identificador de reactor o de extracción malformado.
    [Fact]
    public async Task INT18_Negativa_SolicitudMalformada_SeRechaza()
    {
        var (a, socketA) = await ConectarAsync("pA", 10);
        await MoverAsync(a, _reactor.X, _reactor.Y);

        await EnviarAsync(a, new ClientMessage { Type = "extract_start", ReactorId = "otro", ExtractionId = "123" });

        Assert.Equal("Solicitud de extracción inválida.", socketA.UltimoError());
    }

    // INT-18.9 / CA-18.3: un jugador sin suscripción a la sala no puede extraer.
    [Fact]
    public async Task INT18_Negativa_JugadorNoIdentificado_SeRechaza()
    {
        var socket = new WebSocketFalso();
        var anonimo = new ClientConnection(socket, "anon");

        await EnviarAsync(anonimo, Extraer());

        Assert.Equal("El jugador no está identificado.", socket.UltimoError());
    }
}
