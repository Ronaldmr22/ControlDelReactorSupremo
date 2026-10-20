using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Negocio.Interfaces
{
    public interface IPartidaRepositorio
    {
        Task<Partida?> ObtenerPorIdAsync(int idPartida);
        Task ActualizarAsync(Partida partida);
        Task<List<Partida>> ObtenerPartidasEnCursoAsync();
    }
}
