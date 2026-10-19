
using Xunit;
using NSubstitute;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Modelos.Entidades;
using System.Collections.Generic;

namespace ControlReactor.Tests;

public class PruebasPuntuacion
{
    [Fact]
    public async Task RegistrarMovimiento_DebeSumarUnPunto()
    {
        // 1. Crear un repositorio simulado
        var repositorio = Substitute.For<IJugadorPartidaRepositorio>();

        // 2. Crear un jugador ficticio con 10 puntos
        var jugador = new JugadorPartida
        {
            IdJugadorPartida = 1,
            Puntaje = 10,
            Partida = new Partida
            {
                Estado = "EN_CURSO"
            }
        };

        // 3. Configurar el repositorio simulado
        repositorio.ObtenerPorIdAsync(1).Returns(Task.FromResult<JugadorPartida?>(jugador));

        // 4. Crear el servicio real
        var servicio = new ServicioPuntuacion(repositorio);

        // 5. Registrar un movimiento
        var resultado = await servicio.RegistrarMovimiento(1);

        // 6. Verificar que sumó exactamente un punto
        Assert.Equal(11, resultado);
        Assert.Equal(11, jugador.Puntaje);

        // 7. Verificar que se intentó guardar
        await repositorio.Received(1).ActualizarAsync(jugador);
    }
    
    [Fact]
    public async Task NoDebeSumarSiPartidaFinalizada()
    {
        // 1. Crear repositorio simulado.
        var repositorio = Substitute.For<IJugadorPartidaRepositorio>();

        // 2. Crear jugador con 10 puntos en partida finalizada.
        var jugador = new JugadorPartida
        {
            IdJugadorPartida = 1,
            Puntaje = 10,
            Partida = new Partida
            {
                Estado = "FINALIZADA"
            }
        };

        // 3. Configurar el repositorio.
        repositorio.ObtenerPorIdAsync(1)
            .Returns(Task.FromResult<JugadorPartida?>(jugador));

        // 4. Ejecutar el servicio real.
        var servicio = new ServicioPuntuacion(repositorio);
        var resultado = await servicio.RegistrarMovimiento(1);

        // 5. Comprobar que NO se sumaron puntos.
        Assert.Null(resultado);
        Assert.Equal(10, jugador.Puntaje);

        // 6. Comprobar que NO se intentó guardar.
        await repositorio.DidNotReceive().ActualizarAsync(
            Arg.Any<JugadorPartida>());
    }
}
