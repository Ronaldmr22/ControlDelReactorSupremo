using System;
using NUnit.Framework;
using BattleArena.Core;

public class ConfiguracionPartidaTests
{
    [Test]
    public void NuevaConfiguracion_TieneValoresPorDefecto()
    {
        var config = new ConfiguracionPartida();

        Assert.AreEqual(ModoFinalizacion.PorTiempo, config.Modo);
        Assert.AreEqual(5, config.Minutos);
        Assert.AreEqual(100, config.PuntosObjetivo);
        Assert.AreEqual(5, config.SegundosCongelamiento);
    }

    [Test]
    public void Configurar_ConDatosValidos_GuardaLosValores()
    {
        var config = new ConfiguracionPartida();

        config.Configurar(ModoFinalizacion.PorPuntos, 10, 50, 3);

        Assert.AreEqual(ModoFinalizacion.PorPuntos, config.Modo);
        Assert.AreEqual(10, config.Minutos);
        Assert.AreEqual(50, config.PuntosObjetivo);
        Assert.AreEqual(3, config.SegundosCongelamiento);
    }

    [Test]
    public void Configurar_ConMinutosCero_LanzaError()
    {
        var config = new ConfiguracionPartida();

        // () => empaqueta la llamada para que Assert.Throws la ejecute.
        // La prueba pasa solo si Configurar lanza ArgumentException.
        Assert.Throws<ArgumentException>(() => config.Configurar(ModoFinalizacion.PorTiempo, 0, 100, 5));
    }

    [Test]
    public void Configurar_ConPuntosNegativos_LanzaError()
    {
        var config = new ConfiguracionPartida();

        Assert.Throws<ArgumentException>(() => config.Configurar(ModoFinalizacion.PorPuntos, 5, -1, 5));
    }

    [Test]
    public void Configurar_ConCongelamientoCero_LanzaError()
    {
        var config = new ConfiguracionPartida();

        Assert.Throws<ArgumentException>(() => config.Configurar(ModoFinalizacion.PorTiempo, 5, 100, 0));
    }

    [Test]
    public void Configurar_ConDatosInvalidos_ConservaLaConfiguracionAnterior()
    {
        var config = new ConfiguracionPartida();
        config.Configurar(ModoFinalizacion.PorPuntos, 10, 50, 3);

        Assert.Throws<ArgumentException>(() => config.Configurar(ModoFinalizacion.PorTiempo, 0, 20, 8));

        Assert.AreEqual(ModoFinalizacion.PorPuntos, config.Modo);
        Assert.AreEqual(10, config.Minutos);
    }
}
