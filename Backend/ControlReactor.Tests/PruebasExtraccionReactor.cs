using ControlReactor.Api.Multijugador.Configuration;
using ControlReactor.Api.Multijugador.Networking;

namespace ControlReactor.Tests;

// US-18 Extracción de energía del reactor y US-23 escenarios.
public class PruebasExtraccionReactor
{
    [Fact]
    public void US18_Positiva_PrimeraSolicitud_IniciaExtraccion()
    {
        var gestor = new ExtractionSessionManager();
        var conexion = AyudasPruebas.Conexion("p1");

        var resultado = gestor.TryStart(conexion, "ext-1");

        Assert.Equal(ExtractionStartResult.Started, resultado);
        var sesion = Assert.Single(gestor.GetActiveSessions());
        Assert.Equal("p1", sesion.PlayerId);
        Assert.Equal(1, sesion.PlayerMatchId);
        Assert.Equal("123456", sesion.RoomCode);
    }

    // CA-18.8: un mismo evento físico no se contabiliza dos veces.
    [Fact]
    public void US18_Negativa_IdentificadorRepetido_SeRechazaComoDuplicado()
    {
        var gestor = new ExtractionSessionManager();
        var conexion = AyudasPruebas.Conexion("p1");

        gestor.TryStart(conexion, "ext-1");
        gestor.TryStop(conexion, "ext-1");
        var repetido = gestor.TryStart(conexion, "ext-1");

        Assert.Equal(ExtractionStartResult.Duplicate, repetido);
        Assert.Empty(gestor.GetActiveSessions());
    }

    [Fact]
    public void US18_Negativa_SegundaExtraccionSimultanea_SeRechaza()
    {
        var gestor = new ExtractionSessionManager();
        var conexion = AyudasPruebas.Conexion("p1");

        gestor.TryStart(conexion, "ext-1");
        var segunda = gestor.TryStart(conexion, "ext-2");

        Assert.Equal(ExtractionStartResult.AlreadyActive, segunda);
        Assert.Single(gestor.GetActiveSessions());
    }

    [Fact]
    public void US18_Negativa_ConexionSinSala_NoIniciaExtraccion()
    {
        var gestor = new ExtractionSessionManager();
        var conexion = AyudasPruebas.Conexion("p1", codigoSala: null, idJugadorPartida: null);

        var resultado = gestor.TryStart(conexion, "ext-1");

        Assert.NotEqual(ExtractionStartResult.Started, resultado);
        Assert.Empty(gestor.GetActiveSessions());
    }

    [Fact]
    public void US18_Positiva_SoltarBoton_DetieneSoloLaExtraccionCorrecta()
    {
        var gestor = new ExtractionSessionManager();
        var conexion = AyudasPruebas.Conexion("p1");
        gestor.TryStart(conexion, "ext-1");

        bool otroId = gestor.TryStop(conexion, "ext-otra");
        bool mismoId = gestor.TryStop(conexion, "ext-1");

        Assert.False(otroId);
        Assert.True(mismoId);
        Assert.Empty(gestor.GetActiveSessions());
    }

    // Recarga o sobrecarga: se detienen todas las extracciones de una sala.
    [Fact]
    public void US18_Positiva_DetenerSala_NoAfectaOtrasSalas()
    {
        var gestor = new ExtractionSessionManager();
        gestor.TryStart(AyudasPruebas.Conexion("p1", "111111", 1), "a");
        gestor.TryStart(AyudasPruebas.Conexion("p2", "111111", 2), "b");
        gestor.TryStart(AyudasPruebas.Conexion("p3", "222222", 3), "c");

        var detenidas = gestor.StopRoom("111111");

        Assert.Equal(2, detenidas.Count);
        var restante = Assert.Single(gestor.GetActiveSessions());
        Assert.Equal("222222", restante.RoomCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void US23_Positiva_CadaEscenario_TieneReactorConRadioDeInteraccion(int escenario)
    {
        bool existe = ReactorConfiguration.TryGet(escenario, out var reactor);

        Assert.True(existe);
        Assert.Equal(1.5f, reactor.Radius);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void US23_Negativa_EscenarioInexistente_NoTieneReactor(int escenario)
    {
        Assert.False(ReactorConfiguration.TryGet(escenario, out _));
    }
}
