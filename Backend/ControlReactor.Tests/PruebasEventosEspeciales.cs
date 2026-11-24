using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Negocio.Guardianes;

namespace ControlReactor.Tests;

// US-22 Eventos especiales. Las partidas usan 300 s de duración y un reloj controlado.
public class PruebasEventosEspeciales
{
    // ---------- CA-22.1 Energía adicional ----------

    [Fact]
    public void US22_Positiva_EnergiaAdicional_AparecePrimeraVezAl15PorCiento()
    {
        var gestor = new EnergyBonusManager(AyudasPruebas.RutaMapas());
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        var partida = AyudasPruebas.PartidaEnCurso(1, t0);

        var antes = gestor.Advance(partida, AyudasPruebas.SinConexiones, t0.AddSeconds(40));
        var despues = gestor.Advance(partida, AyudasPruebas.SinConexiones, t0.AddSeconds(46));

        Assert.Null(antes);
        Assert.NotNull(despues);
        Assert.True(despues!.Spawned);
        Assert.Equal(t0.AddSeconds(46 + EnergyBonusManager.DurationSeconds), despues.Bonus.ExpiresAtUtc);
    }

    [Fact]
    public void US22_Positiva_EnergiaAdicional_ApareceExactamenteTresVeces()
    {
        var gestor = new EnergyBonusManager(AyudasPruebas.RutaMapas());
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        var partida = AyudasPruebas.PartidaEnCurso(1, t0);
        int apariciones = 0;

        // Recorre la partida segundo a segundo.
        for (int s = 0; s <= 300; s++)
        {
            var cambio = gestor.Advance(partida, AyudasPruebas.SinConexiones, t0.AddSeconds(s));
            if (cambio is { Spawned: true })
                apariciones++;
        }

        Assert.Equal(EnergyBonusManager.AppearancesPerMatch, apariciones);
    }

    [Fact]
    public void US22_Positiva_EnergiaAdicional_PrimerJugadorCercanoLaReserva()
    {
        var gestor = new EnergyBonusManager(AyudasPruebas.RutaMapas());
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-46));
        var bonus = gestor.Advance(partida, AyudasPruebas.SinConexiones, ahora)!.Bonus;

        bool primero = gestor.TryReserve("123456", bonus.BonusId, bonus.X, bonus.Y, out var reservado);
        bool segundo = gestor.TryReserve("123456", bonus.BonusId, bonus.X, bonus.Y, out _);

        Assert.True(primero);
        Assert.Equal(bonus.BonusId, reservado!.BonusId);
        Assert.False(segundo); // CA-22.9: una sola activación
    }

    [Fact]
    public void US22_Negativa_EnergiaAdicional_JugadorLejano_NoLaRecoge()
    {
        var gestor = new EnergyBonusManager(AyudasPruebas.RutaMapas());
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-46));
        var bonus = gestor.Advance(partida, AyudasPruebas.SinConexiones, ahora)!.Bonus;

        bool reservado = gestor.TryReserve("123456", bonus.BonusId, bonus.X + 5f, bonus.Y, out _);

        Assert.False(reservado);
    }

    // CA-22.8: un evento no afecta una partida finalizada.
    [Fact]
    public void US22_Negativa_PartidaFinalizada_NoGeneraEventos()
    {
        string mapas = AyudasPruebas.RutaMapas();
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        var partida = AyudasPruebas.PartidaEnCurso(1, t0);
        partida.Estado = "FINALIZADA";

        Assert.Null(new EnergyBonusManager(mapas).Advance(partida, AyudasPruebas.SinConexiones, t0.AddSeconds(46)));
        Assert.Null(new ReactorOverloadManager().Advance(partida, t0.AddSeconds(100)));
        Assert.Null(new CreativeEventsManager(mapas).Advance(partida, AyudasPruebas.SinConexiones, t0.AddSeconds(200)));
    }

    [Fact]
    public void US22_Positiva_AlFinalizarPartida_SeRetiraLaEnergiaActiva()
    {
        var gestor = new EnergyBonusManager(AyudasPruebas.RutaMapas());
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-46));
        gestor.Advance(partida, AyudasPruebas.SinConexiones, ahora);

        var retiradas = gestor.RemoveFinishedMatches(new HashSet<int>());

        Assert.Single(retiradas);
        Assert.Null(gestor.GetActiveForRoom("123456"));
    }

    // ---------- CA-22.3 Sobrecarga del reactor ----------

    [Fact]
    public void US22_Positiva_Sobrecarga_DesactivaElReactorDuranteDiezSegundos()
    {
        var gestor = new ReactorOverloadManager();
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-100));

        var inicio = gestor.Advance(partida, ahora);

        Assert.NotNull(inicio);
        Assert.True(inicio!.Started);
        Assert.True(gestor.IsOverloaded("123456"));

        var fin = gestor.Advance(partida, ahora.AddSeconds(ReactorOverloadManager.DurationSeconds + 1));
        Assert.NotNull(fin);
        Assert.False(fin!.Started);
    }

    [Fact]
    public void US22_Positiva_Sobrecarga_OcurreDosVecesPorPartida()
    {
        var gestor = new ReactorOverloadManager();
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        var partida = AyudasPruebas.PartidaEnCurso(1, t0);
        int activaciones = 0;

        for (int s = 0; s <= 300; s++)
        {
            var cambio = gestor.Advance(partida, t0.AddSeconds(s));
            if (cambio is { Started: true })
                activaciones++;
        }

        Assert.Equal(ReactorOverloadManager.ActivationsPerMatch, activaciones);
    }

    [Fact]
    public void US22_Negativa_SinSobrecarga_ReactorDisponible()
    {
        var gestor = new ReactorOverloadManager();
        var ahora = DateTime.UtcNow;
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-5));

        var cambio = gestor.Advance(partida, ahora);

        Assert.Null(cambio);
        Assert.False(gestor.IsOverloaded("123456"));
    }

    // ---------- CA-22.4 Eventos creativos ----------

    [Fact]
    public void US22_Positiva_ArenaMovediza_CreaCuatroZonasYLuegoDoblePuntaje()
    {
        var gestor = new CreativeEventsManager(AyudasPruebas.RutaMapas());
        var ahora = DateTime.UtcNow;
        // A los 200 s ya pasaron las ventanas de arena (15-25 %) y doble puntaje (48-58 %).
        var partida = AyudasPruebas.PartidaEnCurso(1, ahora.AddSeconds(-200));

        var arena = gestor.Advance(partida, AyudasPruebas.SinConexiones, ahora);
        Assert.NotNull(arena);
        Assert.Equal(CreativeEventKind.Sand, arena!.State.Kind);
        Assert.Equal(CreativeEventsManager.SandCount, arena.State.SandZones.Count);

        var finArena = gestor.Advance(partida, AyudasPruebas.SinConexiones,
            ahora.AddSeconds(CreativeEventsManager.SandDurationSeconds + 1));
        Assert.False(finArena!.Started);

        var doble = gestor.Advance(partida, AyudasPruebas.SinConexiones,
            ahora.AddSeconds(CreativeEventsManager.SandDurationSeconds + 1));
        Assert.Equal(CreativeEventKind.DoublePoints, doble!.State.Kind);
        Assert.True(gestor.IsDoublePoints("123456"));
    }

    // ---------- CA-22.2 Aparición de 5 guardianes ----------

    [Fact]
    public void US22_Positiva_OleadaDeGuardianes_AgregaCincoYLosRetiraAlTerminar()
    {
        string mapas = AyudasPruebas.RutaMapas();
        var gestor = new GuardianSurgeManager(mapas);
        var administrador = new AdministradorGuardianesPorSala(mapas).ObtenerOCrear("123456", 1);
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        var partida = AyudasPruebas.PartidaEnCurso(1, t0);

        var inicio = gestor.Advance(partida, administrador, AyudasPruebas.SinConexiones, t0.AddSeconds(21));
        Assert.True(inicio!.Started);
        Assert.Equal(GuardianSurgeManager.ExtraGuardians, inicio.State.GuardianIds.Count);
        Assert.Equal(9, administrador.ObtenerGuardianes().Count);

        var fin = gestor.Advance(partida, administrador, AyudasPruebas.SinConexiones,
            t0.AddSeconds(21 + GuardianSurgeManager.DurationSeconds + 1));
        Assert.False(fin!.Started);
        Assert.Equal(4, administrador.ObtenerGuardianes().Count);

        // Solo una oleada por partida.
        Assert.Null(gestor.Advance(partida, administrador, AyudasPruebas.SinConexiones, t0.AddSeconds(120)));
    }
}
