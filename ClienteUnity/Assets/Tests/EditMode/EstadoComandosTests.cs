using NUnit.Framework;
using BattleArena.Core;

public class EstadoComandosTests
{
    [Test]
    public void PresionarDerecha_EjeHorizontalEsUno()
    {
        var estado = new EstadoComandos("jugador-1");

        estado.Press(ComandoJuego.Right);

        Assert.AreEqual(1, estado.EjeHorizontal);
    }

    [Test]
    public void SoltarDerecha_EjeHorizontalVuelveACero()
    {
        var estado = new EstadoComandos("jugador-1");
        estado.Press(ComandoJuego.Right);

        estado.Release(ComandoJuego.Right);

        Assert.AreEqual(0, estado.EjeHorizontal);
    }

    [Test]
    public void ConsumeAction_SinPulsar_DevuelveFalse()
    {
        var estado = new EstadoComandos("jugador-1");

        Assert.IsFalse(estado.ConsumeAction());
    }

    [Test]
    public void MantenerAccion_SoloDisparaUnaVez()
    {
        var estado = new EstadoComandos("jugador-1");

        estado.Press(ComandoJuego.Action);
        estado.Press(ComandoJuego.Action); // sigue mantenida

        Assert.IsTrue(estado.ConsumeAction());
        Assert.IsFalse(estado.ConsumeAction());
    }

    [Test]
    public void DosJugadores_NoMezclanControles()
    {
        var jugador1 = new EstadoComandos("jugador-1");
        var jugador2 = new EstadoComandos("jugador-2");

        jugador1.Press(ComandoJuego.Up);

        Assert.AreEqual(1, jugador1.EjeVertical);
        Assert.AreEqual(0, jugador2.EjeVertical);
        Assert.AreNotEqual(jugador1.PlayerId, jugador2.PlayerId);
    }
}