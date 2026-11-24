using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using NSubstitute;

namespace ControlReactor.Tests;

// US-19 Sistema competitivo de puntuación (reglas de ServicioPuntuacion).
public class PruebasPuntuacionCompetitiva
{
    private static (ServicioPuntuacion Servicio, IJugadorPartidaRepositorio Repo) Crear(JugadorPartida jugador)
    {
        var repositorio = Substitute.For<IJugadorPartidaRepositorio>();
        repositorio.ObtenerPorIdAsync(jugador.IdJugadorPartida)
            .Returns(Task.FromResult<JugadorPartida?>(jugador));
        return (new ServicioPuntuacion(repositorio), repositorio);
    }

    [Fact]
    public async Task US19_Positiva_CapsulaDeEnergia_SumaDiezPuntos()
    {
        var jugador = new JugadorPartida { IdJugadorPartida = 1, Puntaje = 5 };
        var (servicio, repositorio) = Crear(jugador);

        var resultado = await servicio.ModificarPuntaje(1, 10);

        Assert.Equal(15, resultado);
        await repositorio.Received(1).ActualizarAsync(jugador);
    }

    // Ataque de guardián: resta 10, pero el puntaje nunca es negativo.
    [Fact]
    public async Task US19_Negativa_PenalizacionMayorAlPuntaje_NoDejaPuntajeNegativo()
    {
        var jugador = new JugadorPartida { IdJugadorPartida = 1, Puntaje = 4 };
        var (servicio, _) = Crear(jugador);

        var resultado = await servicio.ModificarPuntaje(1, -10);

        Assert.Equal(0, resultado);
    }

    [Fact]
    public async Task US19_Negativa_PartidaPorPuntos_NoSuperaElPuntajeObjetivo()
    {
        var partida = new Partida { IdPartida = 1, TipoFinalizacion = "PUNTOS", PuntajeObjetivo = 50 };
        var jugador = new JugadorPartida { IdJugadorPartida = 1, Puntaje = 45, Partida = partida };
        var (servicio, _) = Crear(jugador);

        var resultado = await servicio.ModificarPuntaje(1, 10);

        Assert.Equal(50, resultado);
    }

    [Fact]
    public async Task US19_Positiva_PartidaPorTiempo_NoAplicaLimiteDePuntos()
    {
        var partida = new Partida { IdPartida = 1, TipoFinalizacion = "TIEMPO", LimiteTiempo = 300 };
        var jugador = new JugadorPartida { IdJugadorPartida = 1, Puntaje = 95, Partida = partida };
        var (servicio, _) = Crear(jugador);

        var resultado = await servicio.ModificarPuntaje(1, 10);

        Assert.Equal(105, resultado);
    }
}
