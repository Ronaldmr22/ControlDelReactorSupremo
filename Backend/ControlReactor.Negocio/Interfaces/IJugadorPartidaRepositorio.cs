using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Negocio.Interfaces
{
    public interface IJugadorPartidaRepositorio
    {
        Task<JugadorPartida?> ObtenerPorIdAsync(int idJugadorPartida);
        Task ActualizarAsync(JugadorPartida jugador);
        Task<List<JugadorPartida>> ObtenerPorPartidaAsync(int idPartida);
        Task<List<JugadorPartida>> ObtenerPorUsuarioAsync(int idUsuario);
        Task RegistrarGanadorAsync(int idPartida, int idJugadorPartidaGanador);
    }
}