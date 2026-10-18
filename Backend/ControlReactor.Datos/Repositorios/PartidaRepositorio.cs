
using ControlReactor.Datos.Contexto;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Datos.Repositorios
{
    public class PartidaRepositorio : IPartidaRepositorio
    {
        private readonly ControlReactorDbContext _context;

        public PartidaRepositorio(ControlReactorDbContext context)
        {
            _context = context;
        }

        public async Task<Partida?> ObtenerPorIdAsync(int idPartida)
        {
            return await _context.Partidas
                .FirstOrDefaultAsync(p => p.IdPartida == idPartida);
        }

        public async Task ActualizarAsync(Partida partida)
        {
            _context.Partidas.Update(partida);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Partida>> ObtenerPartidasEnCursoAsync()
        {
            return await _context.Partidas
                .Where(p => p.Estado == "EN_CURSO")
                .ToListAsync();
        }
    }
}
