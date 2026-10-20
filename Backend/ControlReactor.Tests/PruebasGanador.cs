
using Xunit;
using NSubstitute;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Modelos.Entidades;
using System.Collections.Generic;

namespace ControlReactor.Tests;

public class PruebasGanador
{
    [Fact]
    public async Task DeterminarGanador_DebeElegirMayorPuntaje()
    {
        // Crear los repositorios simulados.
        var repositorioJugadores = Substitute.For<IJugadorPartidaRepositorio>();
        var repositorioPartidas = Substitute.For<IPartidaRepositorio>();

        // Jugadores ordenados de mayor a menor puntaje.
        var jugadores = new List<JugadorPartida>
        {
            new JugadorPartida { IdJugadorPartida = 1, Puntaje = 50 },
            new JugadorPartida { IdJugadorPartida = 2, Puntaje = 30 },
            new JugadorPartida { IdJugadorPartida = 3, Puntaje = 20 }
        };

        // Simular la consulta de participantes de la partida 1.
        repositorioJugadores.ObtenerPorPartidaAsync(1).Returns(Task.FromResult(jugadores));

        // Crear el servicio real.
        var servicio = new ServicioGanador(
            repositorioJugadores,
            repositorioPartidas
        );

        // Determinar quién ganó.
        var resultado = await servicio.DeterminarGanadorAsync(1);

        // Comprobar que ganó el participante 1.
        Assert.Equal(1, resultado.IdGanador);
        Assert.False(resultado.HayEmpate);
    }

    
    [Fact]
    public async Task DeterminarGanador_DebeDetectarEmpate()
    {
        // Crear repositorios simulados.
        var repositorioJugadores = Substitute.For<IJugadorPartidaRepositorio>();
        var repositorioPartidas = Substitute.For<IPartidaRepositorio>();

        // Dos jugadores comparten el puntaje más alto.
        var jugadores = new List<JugadorPartida>
        {
            new JugadorPartida { IdJugadorPartida = 1, Puntaje = 50 },
            new JugadorPartida { IdJugadorPartida = 2, Puntaje = 50 },
            new JugadorPartida { IdJugadorPartida = 3, Puntaje = 20 }
        };

        repositorioJugadores.ObtenerPorPartidaAsync(1).Returns(Task.FromResult(jugadores));

        var servicio = new ServicioGanador(
            repositorioJugadores,
            repositorioPartidas
        );

        var resultado = await servicio.DeterminarGanadorAsync(1);

        // No debe existir un ganador único.
        Assert.Null(resultado.IdGanador);
        Assert.True(resultado.HayEmpate);
    }

    [Fact]
    public async Task DebeFinalizar_DebeSerVerdaderoAlAlcanzarPuntajeObjetivo()
    {
        // Crear repositorios simulados.
        var repositorioJugadores = Substitute.For<IJugadorPartidaRepositorio>();
        var repositorioPartidas = Substitute.For<IPartidaRepositorio>();

        // Partida activa con objetivo de 100 puntos.
        var partida = new Partida
        {
            IdPartida = 1,
            Estado = "EN_CURSO",
            PuntajeObjetivo = 100
        };

        var jugadores = new List<JugadorPartida>
        {
            new JugadorPartida { IdJugadorPartida = 1, Puntaje = 100 },
            new JugadorPartida { IdJugadorPartida = 2, Puntaje = 75 }
        };

        // Simular las consultas.
        repositorioPartidas.ObtenerPorIdAsync(1)
            .Returns(Task.FromResult<Partida?>(partida));

        repositorioJugadores.ObtenerPorPartidaAsync(1)
            .Returns(Task.FromResult(jugadores));

        // Ejecutar el servicio real.
        var servicio = new ServicioGanador(
            repositorioJugadores,
            repositorioPartidas
        );

        var resultado = await servicio.DebeFinalizarAsync(1);

        // La partida debe cumplir la condición de finalización.
        Assert.True(resultado);
    }

    [Fact]
    public async Task DebeFinalizar_DebeSerVerdaderoCuandoSeAgotaElTiempo()
    {
        // Crear repositorios simulados.
        var repositorioJugadores = Substitute.For<IJugadorPartidaRepositorio>();
        var repositorioPartidas = Substitute.For<IPartidaRepositorio>();

        // Partida de 60 segundos que comenzó hace 90.
        var partida = new Partida
        {
            IdPartida = 1,
            Estado = "EN_CURSO",
            FechaInicio = DateTime.UtcNow.AddSeconds(-90),
            LimiteTiempo = 60
        };

        repositorioPartidas.ObtenerPorIdAsync(1)
            .Returns(Task.FromResult<Partida?>(partida));

        repositorioJugadores.ObtenerPorPartidaAsync(1)
            .Returns(Task.FromResult(new List<JugadorPartida>()));

        // Ejecutar el servicio real.
        var servicio = new ServicioGanador(
            repositorioJugadores,
            repositorioPartidas
        );

        var resultado = await servicio.DebeFinalizarAsync(1);

        // El tiempo agotado debe activar la finalización.
        Assert.True(resultado);
    }

    [Fact]
    public async Task FinalizarPartida_DebeExtenderTiempoSiHayEmpate()
    {
        // Crear repositorios simulados.
        var repositorioJugadores = Substitute.For<IJugadorPartidaRepositorio>();
        var repositorioPartidas = Substitute.For<IPartidaRepositorio>();

        // Partida cuyo tiempo ya terminó.
        var partida = new Partida
        {
            IdPartida = 1,
            Estado = "EN_CURSO",
            FechaInicio = DateTime.UtcNow.AddSeconds(-90),
            LimiteTiempo = 60,
            PuntajeObjetivo = 100
        };

        // Dos jugadores empatados en primer lugar.
        var jugadores = new List<JugadorPartida>
        {
            new JugadorPartida { IdJugadorPartida = 1, Puntaje = 50 },
            new JugadorPartida { IdJugadorPartida = 2, Puntaje = 50 },
            new JugadorPartida { IdJugadorPartida = 3, Puntaje = 30 }
        };

        repositorioPartidas.ObtenerPorIdAsync(1)
            .Returns(Task.FromResult<Partida?>(partida));

        repositorioJugadores.ObtenerPorPartidaAsync(1)
            .Returns(Task.FromResult(jugadores));

        // Ejecutar la finalización.
        var servicio = new ServicioGanador(
            repositorioJugadores,
            repositorioPartidas
        );

        var resultado = await servicio.FinalizarPartidaAsync(1);

        // Comprobar que se activó la prórroga.
        Assert.Contains("empate", resultado, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(120, partida.LimiteTiempo);
        Assert.Null(partida.PuntajeObjetivo);
        Assert.Equal("EN_CURSO", partida.Estado);

        // Comprobar que se solicitó guardar la partida.
        await repositorioPartidas.Received(1).ActualizarAsync(partida);
    }
    
    [Fact]
    public async Task FinalizarPartida_DebeRegistrarGanadorYFinalizar()
    {
        // Crear repositorios simulados.
        var repositorioJugadores = Substitute.For<IJugadorPartidaRepositorio>();
        var repositorioPartidas = Substitute.For<IPartidaRepositorio>();

        // Partida activa con objetivo de 100 puntos.
        var partida = new Partida
        {
            IdPartida = 1,
            Estado = "EN_CURSO",
            FechaInicio = DateTime.UtcNow,
            PuntajeObjetivo = 100
        };

        // Jugadores ordenados por puntaje.
        var jugadores = new List<JugadorPartida>
        {
            new JugadorPartida { IdJugadorPartida = 1, Puntaje = 100 },
            new JugadorPartida { IdJugadorPartida = 2, Puntaje = 75 },
            new JugadorPartida { IdJugadorPartida = 3, Puntaje = 40 }
        };

        repositorioPartidas.ObtenerPorIdAsync(1).Returns(Task.FromResult<Partida?>(partida));

        repositorioJugadores.ObtenerPorPartidaAsync(1).Returns(Task.FromResult(jugadores));

        // Ejecutar el servicio real.
        var servicio = new ServicioGanador(
            repositorioJugadores,
            repositorioPartidas
        );

        var resultado = await servicio.FinalizarPartidaAsync(1);

        // Comprobar el ganador y la finalización.
        Assert.Contains("Ganador: participante 1", resultado);
        Assert.Equal("FINALIZADA", partida.Estado);
        Assert.NotNull(partida.FechaFin);

        // Verificar que se solicitaron los cambios.
        await repositorioJugadores.Received(1).RegistrarGanadorAsync(1, 1);

        await repositorioPartidas.Received(1).ActualizarAsync(partida);
    }
}