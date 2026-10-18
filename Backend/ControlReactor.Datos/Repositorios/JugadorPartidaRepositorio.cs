using ControlReactor.Datos.Contexto;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Datos.Repositorios
{
    public class JugadorPartidaRepositorio : IJugadorPartidaRepositorio
    {
        private readonly ControlReactorDbContext _context;

        public JugadorPartidaRepositorio(ControlReactorDbContext context)
        {
            _context = context;
        }

        public async Task<JugadorPartida?> ObtenerPorIdAsync(int idJugadorPartida)
        {
            return await _context.JugadoresPartida
                .Include(j => j.Partida)
                .FirstOrDefaultAsync(j => j.IdJugadorPartida == idJugadorPartida);
        }

        public async Task ActualizarAsync(JugadorPartida jugador)
        {
            _context.JugadoresPartida.Update(jugador);
            await _context.SaveChangesAsync();
        }
        public async Task<List<JugadorPartida>> ObtenerPorPartidaAsync(int idPartida)
        {
            return await _context.JugadoresPartida
                .Where(j => j.IdPartida == idPartida)
                .OrderByDescending(j => j.Puntaje)
                .ToListAsync();
        }
        
        public async Task RegistrarGanadorAsync(int idPartida, int idJugadorPartidaGanador)
        {
            var jugadores = await _context.JugadoresPartida
                .Where(j => j.IdPartida == idPartida)
                .ToListAsync();

            // Solo el participante ganador queda marcado.
            foreach (var jugador in jugadores)
            {
                jugador.EsGanador =
                    jugador.IdJugadorPartida == idJugadorPartidaGanador;
            }

            await _context.SaveChangesAsync();
        }


    }
}