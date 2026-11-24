using ControlReactor.Datos.Contexto;
using ControlReactor.Datos.Repositorios;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ControlReactor.Tests;

// Pruebas de INTEGRACIÓN Negocio + Datos (US-25, US-27, US-28).
// Usan el ServicioGanador real con los repositorios reales sobre EF Core InMemory,
// en lugar de simulaciones, para comprobar que el resultado queda persistido.
public class PruebasIntegracionFinalizacion
{
    private static DbContextOptions<ControlReactorDbContext> Opciones(string nombreBd) =>
        new DbContextOptionsBuilder<ControlReactorDbContext>()
            .UseInMemoryDatabase(nombreBd)
            .Options;

    private static ServicioGanador Servicio(ControlReactorDbContext db) =>
        new(new JugadorPartidaRepositorio(db), new PartidaRepositorio(db));

    private static async Task SembrarAsync(
        string nombreBd, Partida partida, params (int Id, int IdUsuario, int Puntaje)[] jugadores)
    {
        await using var db = new ControlReactorDbContext(Opciones(nombreBd));
        db.Partidas.Add(partida);
        foreach (var j in jugadores)
        {
            db.JugadoresPartida.Add(new JugadorPartida
            {
                IdJugadorPartida = j.Id,
                IdUsuario = j.IdUsuario,
                IdPartida = partida.IdPartida,
                Puntaje = j.Puntaje,
                EnSala = true
            });
        }
        await db.SaveChangesAsync();
    }

    private static Partida PartidaPorTiempo(int id, int segundosTranscurridos, int limite = 300) => new()
    {
        IdPartida = id,
        CodigoSala = "SALA" + id,
        Estado = "EN_CURSO",
        TipoFinalizacion = "TIEMPO",
        LimiteTiempo = limite,
        FechaInicio = DateTime.UtcNow.AddSeconds(-segundosTranscurridos)
    };

    // INT-25.1 / CA-25.1, CA-25.2, CA-25.5, CA-25.9
    [Fact]
    public async Task INT25_Positiva_TiempoAgotado_FinalizaYPersisteGanador()
    {
        string bd = Guid.NewGuid().ToString();
        await SembrarAsync(bd, PartidaPorTiempo(1, 400), (1, 10, 30), (2, 11, 80), (3, 12, 50));

        await using (var db = new ControlReactorDbContext(Opciones(bd)))
        {
            string mensaje = await Servicio(db).FinalizarPartidaAsync(1);
            Assert.Contains("Ganador: participante 2", mensaje);
        }

        // Contexto nuevo: simula reiniciar el servidor o que el cliente abandone (CA-25.9).
        await using var lectura = new ControlReactorDbContext(Opciones(bd));
        var partida = await lectura.Partidas.SingleAsync(p => p.IdPartida == 1);
        var jugadores = await lectura.JugadoresPartida.Where(j => j.IdPartida == 1).ToListAsync();

        Assert.Equal("FINALIZADA", partida.Estado);
        Assert.NotNull(partida.FechaFin);
        var ganador = Assert.Single(jugadores, j => j.EsGanador);
        Assert.Equal(2, ganador.IdJugadorPartida);
    }

    // INT-25.2 / CA-25.1 (puntuación objetivo)
    [Fact]
    public async Task INT25_Positiva_PuntajeObjetivoAlcanzado_Finaliza()
    {
        string bd = Guid.NewGuid().ToString();
        var partida = new Partida
        {
            IdPartida = 2, CodigoSala = "SALA2", Estado = "EN_CURSO",
            TipoFinalizacion = "PUNTOS", PuntajeObjetivo = 50,
            FechaInicio = DateTime.UtcNow.AddSeconds(-30)
        };
        await SembrarAsync(bd, partida, (4, 10, 50), (5, 11, 20));

        await using var db = new ControlReactorDbContext(Opciones(bd));
        await Servicio(db).FinalizarPartidaAsync(2);

        var guardada = await db.Partidas.SingleAsync(p => p.IdPartida == 2);
        Assert.Equal("FINALIZADA", guardada.Estado);
    }

    // INT-25.3 / CA-25.1 negativa: sin condición cumplida no se finaliza.
    [Fact]
    public async Task INT25_Negativa_TiempoNoAgotado_NoFinaliza()
    {
        string bd = Guid.NewGuid().ToString();
        await SembrarAsync(bd, PartidaPorTiempo(3, 60), (6, 10, 30), (7, 11, 10));

        await using var db = new ControlReactorDbContext(Opciones(bd));
        string mensaje = await Servicio(db).FinalizarPartidaAsync(3);

        var partida = await db.Partidas.SingleAsync(p => p.IdPartida == 3);
        Assert.Equal("EN_CURSO", partida.Estado);
        Assert.Null(partida.FechaFin);
        Assert.Contains("todavía no cumple", mensaje);
        Assert.DoesNotContain(await db.JugadoresPartida.ToListAsync(), j => j.EsGanador);
    }

    // INT-25.4 / CA-25.6: empate -> se amplía 2 minutos y se mantienen los puntajes.
    [Fact]
    public async Task INT25_Positiva_Empate_ExtiendeDosMinutosYConservaPuntajes()
    {
        string bd = Guid.NewGuid().ToString();
        await SembrarAsync(bd, PartidaPorTiempo(4, 400), (8, 10, 60), (9, 11, 60));

        await using (var db = new ControlReactorDbContext(Opciones(bd)))
        {
            string mensaje = await Servicio(db).FinalizarPartidaAsync(4);
            Assert.Contains("empate", mensaje, StringComparison.OrdinalIgnoreCase);
        }

        await using var lectura = new ControlReactorDbContext(Opciones(bd));
        var partida = await lectura.Partidas.SingleAsync(p => p.IdPartida == 4);
        var puntajes = await lectura.JugadoresPartida.Select(j => j.Puntaje).ToListAsync();

        Assert.Equal("EN_CURSO", partida.Estado);
        Assert.Equal(120, partida.LimiteTiempo);
        Assert.All(puntajes, p => Assert.Equal(60, p));
    }

    // INT-25.5 / CA-25.7, CA-27.8, CA-28.8: finalizar dos veces no duplica el resultado.
    [Fact]
    public async Task INT25_Negativa_FinalizarDosVeces_NoCambiaNiDuplicaElResultado()
    {
        string bd = Guid.NewGuid().ToString();
        await SembrarAsync(bd, PartidaPorTiempo(5, 400), (10, 10, 90), (11, 11, 40));

        await using var db = new ControlReactorDbContext(Opciones(bd));
        var servicio = Servicio(db);
        await servicio.FinalizarPartidaAsync(5);
        var fechaFin = (await db.Partidas.SingleAsync(p => p.IdPartida == 5)).FechaFin;

        string segunda = await servicio.FinalizarPartidaAsync(5);

        Assert.Equal("La partida no está en curso.", segunda);
        Assert.Equal(fechaFin, (await db.Partidas.SingleAsync(p => p.IdPartida == 5)).FechaFin);
        Assert.Single(await db.JugadoresPartida.Where(j => j.EsGanador).ToListAsync());
    }

    // INT-25.6 / CA-25.3: la partida finalizada ya no se considera "debe finalizar".
    [Fact]
    public async Task INT25_Negativa_PartidaFinalizada_NoVuelveAProcesarse()
    {
        string bd = Guid.NewGuid().ToString();
        var partida = PartidaPorTiempo(6, 400);
        partida.Estado = "FINALIZADA";
        partida.FechaFin = DateTime.UtcNow;
        await SembrarAsync(bd, partida, (12, 10, 10));

        await using var db = new ControlReactorDbContext(Opciones(bd));
        Assert.False(await Servicio(db).DebeFinalizarAsync(6));
    }

    // INT-28.1 / CA-28.1, CA-28.3, CA-28.6: historial por usuario desde la persistencia,
    // del más reciente al más antiguo.
    [Fact]
    public async Task INT28_Positiva_HistorialDelUsuario_SeRecuperaOrdenadoDesdeLaBase()
    {
        string bd = Guid.NewGuid().ToString();
        await using (var db = new ControlReactorDbContext(Opciones(bd)))
        {
            db.Partidas.AddRange(
                new Partida { IdPartida = 20, Estado = "FINALIZADA", TipoFinalizacion = "TIEMPO", FechaInicio = DateTime.UtcNow.AddDays(-2), FechaFin = DateTime.UtcNow.AddDays(-2).AddMinutes(5) },
                new Partida { IdPartida = 21, Estado = "FINALIZADA", TipoFinalizacion = "TIEMPO", FechaInicio = DateTime.UtcNow.AddDays(-1), FechaFin = DateTime.UtcNow.AddDays(-1).AddMinutes(5) });
            db.JugadoresPartida.AddRange(
                new JugadorPartida { IdJugadorPartida = 30, IdUsuario = 99, IdPartida = 20, Puntaje = 10 },
                new JugadorPartida { IdJugadorPartida = 31, IdUsuario = 99, IdPartida = 21, Puntaje = 20, EsGanador = true },
                new JugadorPartida { IdJugadorPartida = 32, IdUsuario = 77, IdPartida = 21, Puntaje = 5 });
            await db.SaveChangesAsync();
        }

        await using var lectura = new ControlReactorDbContext(Opciones(bd));
        var historial = await new JugadorPartidaRepositorio(lectura).ObtenerPorUsuarioAsync(99);

        Assert.Equal(new[] { 21, 20 }, historial.Select(h => h.IdPartida).ToArray());
        Assert.All(historial, h => Assert.Equal(99, h.IdUsuario));
        Assert.NotNull(historial[0].Partida);
    }

    // DEF-01 (abierto): tras un empate, FinalizarPartidaAsync sobrescribe FechaInicio,
    // por lo que la duración guardada (FechaFin - FechaInicio) solo cuenta la prórroga
    // (afecta CA-26.5 y CA-28.2). Quitar Skip cuando se corrija.
    [Fact(Skip = "DEF-01 abierto: la prórroga sobrescribe FechaInicio y se pierde la duración real.")]
    public async Task DEF02_Empate_LaDuracionFinalIncluyeElTiempoOriginal()
    {
        string bd = Guid.NewGuid().ToString();
        await SembrarAsync(bd, PartidaPorTiempo(7, 400), (40, 10, 60), (41, 11, 60));

        await using var db = new ControlReactorDbContext(Opciones(bd));
        await Servicio(db).FinalizarPartidaAsync(7);

        var partida = await db.Partidas.SingleAsync(p => p.IdPartida == 7);
        Assert.True((DateTime.UtcNow - partida.FechaInicio).TotalSeconds >= 400);
    }
}
