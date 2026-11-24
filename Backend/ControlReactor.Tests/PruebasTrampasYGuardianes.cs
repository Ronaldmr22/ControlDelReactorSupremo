using ControlReactor.Api.Multijugador.Events;
using ControlReactor.Negocio.Guardianes;

namespace ControlReactor.Tests;

// US-20 Obstáculos y trampas, US-21 Guardianes y estados temporales.
public class PruebasTrampasYGuardianes
{
    // ---------- US-20 Trampas ----------

    [Fact]
    public void US20_Positiva_PartidaEnCurso_GeneraTrampasDeTiposDistintos()
    {
        var gestor = new TrapManager(AyudasPruebas.RutaMapas());
        var partida = AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow);

        var estado = gestor.GetOrCreate(partida, AyudasPruebas.SinConexiones);

        Assert.NotNull(estado);
        Assert.Contains(estado!.Traps, t => t.Kind == TrapKind.InvertedControls);
        Assert.Contains(estado.Traps, t => t.Kind == TrapKind.ElectricCage);
    }

    // CA-20.9 pide al menos 3 trampas distintas. Esta prueba documenta el requisito:
    // si falla, registrar el defecto con la cantidad obtenida.
    [Fact]
    public void US20_Requisito_CA20_9_GeneraAlMenosTresTrampas()
    {
        var gestor = new TrapManager(AyudasPruebas.RutaMapas());
        var partida = AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow);

        var estado = gestor.GetOrCreate(partida, AyudasPruebas.SinConexiones);

        Assert.NotNull(estado);
        Assert.True(estado!.Traps.Count >= 3,
            $"CA-20.9 requiere al menos 3 trampas; se generaron {estado.Traps.Count}.");
    }

    [Fact]
    public void US20_Positiva_ConsultasRepetidas_DevuelvenLasMismasTrampas()
    {
        var gestor = new TrapManager(AyudasPruebas.RutaMapas());
        var partida = AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow);

        var primera = gestor.GetOrCreate(partida, AyudasPruebas.SinConexiones);
        var segunda = gestor.GetOrCreate(partida, AyudasPruebas.SinConexiones);

        Assert.Same(primera, segunda);
    }

    [Fact]
    public void US20_Positiva_TrampasSeparadasAlMenosTresUnidades()
    {
        var gestor = new TrapManager(AyudasPruebas.RutaMapas());
        var estado = gestor.GetOrCreate(AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow), AyudasPruebas.SinConexiones)!;

        var a = estado.Traps[0];
        var b = estado.Traps[1];
        float distancia = MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        Assert.True(distancia >= 3f);
    }

    [Fact]
    public void US20_Negativa_PartidaEnEspera_NoGeneraTrampas()
    {
        var gestor = new TrapManager(AyudasPruebas.RutaMapas());
        var partida = AyudasPruebas.PartidaEnCurso(1, DateTime.UtcNow);
        partida.Estado = "ESPERANDO";

        Assert.Null(gestor.GetOrCreate(partida, AyudasPruebas.SinConexiones));
    }

    // ---------- US-21 Guardianes ----------

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void US21_Positiva_CadaEscenario_TieneCuatroGuardianesIniciales(int escenario)
    {
        var administrador = new AdministradorGuardianesPorSala(AyudasPruebas.RutaMapas())
            .ObtenerOCrear("SALA" + escenario, escenario);

        var guardianes = administrador.ObtenerGuardianes();

        Assert.Equal(4, guardianes.Count);
        Assert.All(guardianes, g => Assert.IsType<EstadoPatrulla>(g.EstadoActual));
    }

    [Fact]
    public void US21_Negativa_CrearGuardianesSinCuatroPosiciones_SeRechaza()
    {
        var administrador = new AdministradorGuardianes();

        Assert.Throws<ArgumentException>(() =>
            administrador.CrearGuardianes(new (float X, float Y)[] { (0, 0), (1, 1), (2, 2) }));
    }

    [Fact]
    public void US21_Positiva_MismaSala_ReutilizaLosMismosGuardianes()
    {
        var porSala = new AdministradorGuardianesPorSala(AyudasPruebas.RutaMapas());

        var primero = porSala.ObtenerOCrear("abc123", 1);
        var segundo = porSala.ObtenerOCrear("ABC123", 1);

        Assert.Same(primero, segundo);
    }

    [Fact]
    public void US21_Positiva_Persecucion_AcercaAlGuardianAlObjetivo()
    {
        var guardian = new Guardian(1, 0f, 0f);
        guardian.CambiarEstado(new EstadoPersecucion { ObjetivoX = 3f, ObjetivoY = 0f });

        guardian.Actualizar(0.5f); // 1,5 unidades/s durante 0,5 s

        Assert.Equal(0.75f, guardian.X, 3);
        Assert.Equal(2.25f, guardian.CalcularDistancia(3f, 0f), 3);
    }

    [Fact]
    public void US21_Negativa_Ataque_DetieneAlGuardian()
    {
        var guardian = new Guardian(1, 2f, 2f);
        guardian.CambiarEstado(new EstadoAtaque());

        guardian.Actualizar(1f);

        Assert.Equal(2f, guardian.X);
        Assert.Equal(2f, guardian.Y);
    }

    // CA-21.5 y CA-21.6: estado congelado temporal en el servidor.
    [Fact]
    public void US21_Positiva_JugadorCongelado_RecuperaMovimientoAlVencer()
    {
        var conexion = AyudasPruebas.Conexion("p1");

        conexion.CongeladoHasta = DateTime.UtcNow.AddSeconds(2);
        bool congelado = conexion.EstaCongelado();

        conexion.CongeladoHasta = DateTime.UtcNow.AddSeconds(-1);
        bool trasVencer = conexion.EstaCongelado();

        Assert.True(congelado);
        Assert.False(trasVencer);
    }

    [Fact]
    public void US21_Negativa_JugadorNuncaAtacado_NoEstaCongelado()
    {
        Assert.False(AyudasPruebas.Conexion("p1").EstaCongelado());
    }
}
