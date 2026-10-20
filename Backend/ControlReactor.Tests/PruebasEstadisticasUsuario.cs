using ControlReactor.Datos.Contexto;
using ControlReactor.Datos.Repositorios;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Servicios;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Tests;

public class PruebasEstadisticasUsuario
{
    [Fact]
    public async Task Estadisticas_ContabilizanSoloPartidasFinalizadas()
    {
        await using var db = CrearContext();
        var usuario = new Usuario { IdUsuario = 1, NombreUsuario = "Ana", Correo = "ana@test.com" };
        db.Usuarios.Add(usuario);

        var partidaActiva = new Partida
        {
            IdPartida = 1,
            Estado = "EN_CURSO",
            FechaInicio = DateTime.UtcNow.AddHours(-2),
            FechaFin = null
        };

        var partidaFinalizada1 = new Partida
        {
            IdPartida = 2,
            Estado = "FINALIZADA",
            FechaInicio = DateTime.UtcNow.AddHours(-6),
            FechaFin = DateTime.UtcNow.AddHours(-4)
        };

        var partidaFinalizada2 = new Partida
        {
            IdPartida = 3,
            Estado = "FINALIZADA",
            FechaInicio = DateTime.UtcNow.AddHours(-10),
            FechaFin = DateTime.UtcNow.AddHours(-8)
        };

        db.Partidas.AddRange(partidaActiva, partidaFinalizada1, partidaFinalizada2);

        db.JugadoresPartida.AddRange(
            new JugadorPartida { IdJugadorPartida = 1, IdUsuario = 1, IdPartida = 1, Puntaje = 99, EsGanador = false, Partida = partidaActiva, Usuario = usuario },
            new JugadorPartida { IdJugadorPartida = 2, IdUsuario = 1, IdPartida = 2, Puntaje = 150, EsGanador = true, Partida = partidaFinalizada1, Usuario = usuario },
            new JugadorPartida { IdJugadorPartida = 3, IdUsuario = 1, IdPartida = 3, Puntaje = 40, EsGanador = false, Partida = partidaFinalizada2, Usuario = usuario }
        );

        await db.SaveChangesAsync();

        var repo = new JugadorPartidaRepositorio(db);
        var estadisticas = await ObtenerEstadisticasAsync(1, db, repo, new ServicioLogros());

        Assert.Equal(2, estadisticas.PartidasJugadas);
        Assert.Equal(1, estadisticas.PartidasGanadas);
        Assert.Equal(1, estadisticas.PartidasPerdidas);
        Assert.Equal(190, estadisticas.PuntosTotales);
        Assert.NotNull(estadisticas.HistorialPartidas);
        Assert.DoesNotContain(estadisticas.HistorialPartidas, h => h.IdPartida == 1);
    }

    [Fact]
    public async Task Estadisticas_DebenCalcularVictoriasDerrotasYPuntosTotalesCorrectamente()
    {
        await using var db = CrearContext();
        var usuario = new Usuario { IdUsuario = 2, NombreUsuario = "Beto", Correo = "beto@test.com" };
        db.Usuarios.Add(usuario);

        var p1 = new Partida { IdPartida = 10, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-20), FechaFin = DateTime.UtcNow.AddHours(-18) };
        var p2 = new Partida { IdPartida = 11, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-15), FechaFin = DateTime.UtcNow.AddHours(-13) };
        var p3 = new Partida { IdPartida = 12, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-10), FechaFin = DateTime.UtcNow.AddHours(-8) };
        db.Partidas.AddRange(p1, p2, p3);

        db.JugadoresPartida.AddRange(
            new JugadorPartida { IdJugadorPartida = 10, IdUsuario = 2, IdPartida = 10, Puntaje = 90, EsGanador = true, Partida = p1, Usuario = usuario },
            new JugadorPartida { IdJugadorPartida = 11, IdUsuario = 2, IdPartida = 11, Puntaje = 30, EsGanador = false, Partida = p2, Usuario = usuario },
            new JugadorPartida { IdJugadorPartida = 12, IdUsuario = 2, IdPartida = 12, Puntaje = 60, EsGanador = false, Partida = p3, Usuario = usuario }
        );

        await db.SaveChangesAsync();

        var repo = new JugadorPartidaRepositorio(db);
        var estadisticas = await ObtenerEstadisticasAsync(2, db, repo, new ServicioLogros());

        Assert.Equal(3, estadisticas.PartidasJugadas);
        Assert.Equal(1, estadisticas.PartidasGanadas);
        Assert.Equal(2, estadisticas.PartidasPerdidas);
        Assert.Equal(180, estadisticas.PuntosTotales);
    }

    [Fact]
    public async Task Estadisticas_ParaUsuarioSinPartidas_DebenSerCero()
    {
        await using var db = CrearContext();
        var usuario = new Usuario { IdUsuario = 3, NombreUsuario = "Caro", Correo = "caro@test.com" };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var repo = new JugadorPartidaRepositorio(db);
        var estadisticas = await ObtenerEstadisticasAsync(3, db, repo, new ServicioLogros());

        Assert.Equal(0, estadisticas.PartidasJugadas);
        Assert.Equal(0, estadisticas.PartidasGanadas);
        Assert.Equal(0, estadisticas.PartidasPerdidas);
        Assert.Equal(0, estadisticas.PuntosTotales);
        Assert.Empty(estadisticas.Logros);
        Assert.Empty(estadisticas.HistorialPartidas);
        Assert.Null(estadisticas.PosicionRanking);
    }

    [Fact]
    public async Task Ranking_DebeOrdenarPorPuntosDescendentes()
    {
        await using var db = CrearContext();

        var u1 = new Usuario { IdUsuario = 10, NombreUsuario = "J1", Correo = "j1@test.com" };
        var u2 = new Usuario { IdUsuario = 11, NombreUsuario = "J2", Correo = "j2@test.com" };
        var u3 = new Usuario { IdUsuario = 12, NombreUsuario = "J3", Correo = "j3@test.com" };
        db.Usuarios.AddRange(u1, u2, u3);

        var p1 = new Partida { IdPartida = 20, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-10), FechaFin = DateTime.UtcNow.AddHours(-8) };
        var p2 = new Partida { IdPartida = 21, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-9), FechaFin = DateTime.UtcNow.AddHours(-7) };
        var p3 = new Partida { IdPartida = 22, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-8), FechaFin = DateTime.UtcNow.AddHours(-6) };
        db.Partidas.AddRange(p1, p2, p3);

        db.JugadoresPartida.AddRange(
            new JugadorPartida { IdJugadorPartida = 20, IdUsuario = 10, IdPartida = 20, Puntaje = 60, EsGanador = false, Usuario = u1, Partida = p1 },
            new JugadorPartida { IdJugadorPartida = 21, IdUsuario = 11, IdPartida = 21, Puntaje = 90, EsGanador = true, Usuario = u2, Partida = p2 },
            new JugadorPartida { IdJugadorPartida = 22, IdUsuario = 12, IdPartida = 22, Puntaje = 70, EsGanador = true, Usuario = u3, Partida = p3 }
        );

        await db.SaveChangesAsync();

        var ranking = await ObtenerRankingAsync(db);

        Assert.Equal(3, ranking.Count);
        Assert.Equal(11, ranking[0].IdUsuario);
        Assert.Equal(12, ranking[1].IdUsuario);
        Assert.Equal(10, ranking[2].IdUsuario);
    }

    [Fact]
    public async Task Ranking_ConEmpatesEnPuntaje_DebeResolverPorVictoriasYLuegoPorIdUsuario()
    {
        await using var db = CrearContext();

        var u1 = new Usuario { IdUsuario = 30, NombreUsuario = "U30", Correo = "u30@test.com" };
        var u2 = new Usuario { IdUsuario = 31, NombreUsuario = "U31", Correo = "u31@test.com" };
        var u3 = new Usuario { IdUsuario = 32, NombreUsuario = "U32", Correo = "u32@test.com" };
        db.Usuarios.AddRange(u1, u2, u3);

        var p1 = new Partida { IdPartida = 30, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-6), FechaFin = DateTime.UtcNow.AddHours(-5) };
        var p2 = new Partida { IdPartida = 31, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-5), FechaFin = DateTime.UtcNow.AddHours(-4) };
        var p3 = new Partida { IdPartida = 32, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-4), FechaFin = DateTime.UtcNow.AddHours(-3) };
        db.Partidas.AddRange(p1, p2, p3);

        db.JugadoresPartida.AddRange(
            new JugadorPartida { IdJugadorPartida = 30, IdUsuario = 30, IdPartida = 30, Puntaje = 100, EsGanador = true, Usuario = u1, Partida = p1 },
            new JugadorPartida { IdJugadorPartida = 31, IdUsuario = 31, IdPartida = 31, Puntaje = 100, EsGanador = false, Usuario = u2, Partida = p2 },
            new JugadorPartida { IdJugadorPartida = 32, IdUsuario = 32, IdPartida = 32, Puntaje = 100, EsGanador = true, Usuario = u3, Partida = p3 }
        );

        await db.SaveChangesAsync();

        var ranking = await ObtenerRankingAsync(db);

        Assert.Equal(30, ranking[0].IdUsuario);
        Assert.Equal(32, ranking[1].IdUsuario);
        Assert.Equal(31, ranking[2].IdUsuario);
    }

    [Fact]
    public async Task Historial_DebeExcluirPartidasEnCurso()
    {
        await using var db = CrearContext();
        var usuario = new Usuario { IdUsuario = 4, NombreUsuario = "Dani", Correo = "dani@test.com" };
        db.Usuarios.Add(usuario);

        var partidaEnCurso = new Partida { IdPartida = 40, Estado = "EN_CURSO", FechaInicio = DateTime.UtcNow.AddHours(-2), FechaFin = null };
        var partidaFinalizada = new Partida { IdPartida = 41, Estado = "FINALIZADA", FechaInicio = DateTime.UtcNow.AddHours(-6), FechaFin = DateTime.UtcNow.AddHours(-5) };
        db.Partidas.AddRange(partidaEnCurso, partidaFinalizada);

        db.JugadoresPartida.AddRange(
            new JugadorPartida { IdJugadorPartida = 40, IdUsuario = 4, IdPartida = 40, Puntaje = 33, EsGanador = false, Usuario = usuario, Partida = partidaEnCurso },
            new JugadorPartida { IdJugadorPartida = 41, IdUsuario = 4, IdPartida = 41, Puntaje = 80, EsGanador = true, Usuario = usuario, Partida = partidaFinalizada }
        );

        await db.SaveChangesAsync();

        var repo = new JugadorPartidaRepositorio(db);
        var estadisticas = await ObtenerEstadisticasAsync(4, db, repo, new ServicioLogros());

        Assert.Equal(1, estadisticas.PartidasJugadas);
        Assert.Equal(1, estadisticas.PartidasGanadas);
        Assert.Single(estadisticas.HistorialPartidas);
        Assert.Equal(41, estadisticas.HistorialPartidas[0].IdPartida);
    }

    private static ControlReactorDbContext CrearContext()
    {
        var options = new DbContextOptionsBuilder<ControlReactorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ControlReactorDbContext(options);
    }

    private static async Task<EstadisticasUsuarioResultado> ObtenerEstadisticasAsync(
        int idUsuario,
        ControlReactorDbContext db,
        JugadorPartidaRepositorio jugadorRepositorio,
        ServicioLogros servicioLogros)
    {
        if (idUsuario <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(idUsuario));
        }

        var existeUsuario = await db.Usuarios.AnyAsync(u => u.IdUsuario == idUsuario);
        if (!existeUsuario)
        {
            return new EstadisticasUsuarioResultado
            {
                IdUsuario = idUsuario,
                PartidasJugadas = 0,
                PartidasGanadas = 0,
                PartidasPerdidas = 0,
                PuntosTotales = 0,
                PosicionRanking = null,
                Logros = new List<Logro>(),
                HistorialPartidas = new List<HistorialPartidaResultado>()
            };
        }

        var participaciones = await jugadorRepositorio.ObtenerPorUsuarioAsync(idUsuario);
        var finalizadas = participaciones.Where(j => j.Partida.FechaFin != null).ToList();

        int partidasJugadas = finalizadas.Count;
        int partidasGanadas = finalizadas.Count(j => j.EsGanador);
        int partidasPerdidas = partidasJugadas - partidasGanadas;
        int puntosTotales = finalizadas.Sum(j => j.Puntaje);

        var historial = finalizadas.Select(j => new HistorialPartidaResultado
        {
            IdPartida = j.IdPartida,
            FechaInicio = j.Partida.FechaInicio,
            FechaFin = j.Partida.FechaFin,
            Puntaje = j.Puntaje,
            EsGanador = j.EsGanador
        }).ToList();

        var rankingGlobal = await db.JugadoresPartida
            .AsNoTracking()
            .Where(j => j.Partida.FechaFin != null)
            .GroupBy(j => j.IdUsuario)
            .Select(g => new
            {
                IdUsuario = g.Key,
                PuntosTotales = g.Sum(j => j.Puntaje),
                PartidasGanadas = g.Count(j => j.EsGanador)
            })
            .OrderByDescending(j => j.PuntosTotales)
            .ThenByDescending(j => j.PartidasGanadas)
            .ThenBy(j => j.IdUsuario)
            .ToListAsync();

        int? posicionRanking = null;
        for (int i = 0; i < rankingGlobal.Count; i++)
        {
            if (rankingGlobal[i].IdUsuario == idUsuario)
            {
                posicionRanking = i + 1;
                break;
            }
        }

        var logros = servicioLogros.ObtenerLogros(partidasJugadas, partidasGanadas);

        return new EstadisticasUsuarioResultado
        {
            IdUsuario = idUsuario,
            PartidasJugadas = partidasJugadas,
            PartidasGanadas = partidasGanadas,
            PartidasPerdidas = partidasPerdidas,
            PuntosTotales = puntosTotales,
            PosicionRanking = posicionRanking,
            Logros = logros,
            HistorialPartidas = historial
        };
    }

    private static async Task<List<RankingResultado>> ObtenerRankingAsync(ControlReactorDbContext db)
    {
        var ranking = await db.JugadoresPartida
            .AsNoTracking()
            .Where(j => j.Partida.FechaFin != null)
            .GroupBy(j => j.IdUsuario)
            .Select(g => new
            {
                IdUsuario = g.Key,
                PuntosTotales = g.Sum(j => j.Puntaje),
                PartidasJugadas = g.Count(),
                PartidasGanadas = g.Count(j => j.EsGanador)
            })
            .OrderByDescending(j => j.PuntosTotales)
            .ThenByDescending(j => j.PartidasGanadas)
            .ThenBy(j => j.IdUsuario)
            .ToListAsync();

        return ranking.Select((j, indice) => new RankingResultado
        {
            Posicion = indice + 1,
            IdUsuario = j.IdUsuario,
            PuntosTotales = j.PuntosTotales,
            PartidasJugadas = j.PartidasJugadas,
            PartidasGanadas = j.PartidasGanadas
        }).ToList();
    }

    private sealed class EstadisticasUsuarioResultado
    {
        public int IdUsuario { get; set; }
        public int PartidasJugadas { get; set; }
        public int PartidasGanadas { get; set; }
        public int PartidasPerdidas { get; set; }
        public int PuntosTotales { get; set; }
        public int? PosicionRanking { get; set; }
        public List<Logro> Logros { get; set; } = new();
        public List<HistorialPartidaResultado> HistorialPartidas { get; set; } = new();
    }

    private sealed class HistorialPartidaResultado
    {
        public int IdPartida { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int Puntaje { get; set; }
        public bool EsGanador { get; set; }
    }

    private sealed class RankingResultado
    {
        public int Posicion { get; set; }
        public int IdUsuario { get; set; }
        public int PuntosTotales { get; set; }
        public int PartidasJugadas { get; set; }
        public int PartidasGanadas { get; set; }
    }
}
