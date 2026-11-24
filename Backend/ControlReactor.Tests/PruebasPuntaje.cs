
using Xunit;
using NSubstitute;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Tests;

public class PruebasPuntuacion
{
    [Fact]
    public async Task ModificarPuntaje_DebeSumarUnPunto()
    {
        var repositorio =
            Substitute.For<IJugadorPartidaRepositorio>();

        var jugador = new JugadorPartida
        {
            IdJugadorPartida = 1,
            Puntaje = 10
        };

        repositorio.ObtenerPorIdAsync(1)
            .Returns(Task.FromResult<JugadorPartida?>(jugador));

        var servicio = new ServicioPuntuacion(repositorio);

        var resultado = await servicio.ModificarPuntaje(1, 1);

        Assert.Equal(11, resultado);
        Assert.Equal(11, jugador.Puntaje);

        await repositorio.Received(1)
            .ActualizarAsync(jugador);
    }

    [Fact]
    public async Task ModificarPuntaje_JugadorInexistente()
    {
        var repositorio =
            Substitute.For<IJugadorPartidaRepositorio>();

        repositorio.ObtenerPorIdAsync(1)
            .Returns(Task.FromResult<JugadorPartida?>(null));

        var servicio = new ServicioPuntuacion(repositorio);

        var resultado = await servicio.ModificarPuntaje(1, 1);

        Assert.Null(resultado);

        await repositorio.DidNotReceive()
            .ActualizarAsync(Arg.Any<JugadorPartida>());
    }
}
