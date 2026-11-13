using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Negocio.Interfaces
{
    public interface IUsuarioRepositorio
    {
        Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);
        Task<bool> ExisteCorreoAsync(string correo);
        Task<Usuario> CrearAsync(Usuario usuario);
        Task<Usuario?> ObtenerPorCorreoAsync(string correo);
        Task ActualizarAsync(Usuario usuario);
        Task<Usuario?> ObtenerPorIdAsync(int idUsuario);

        Task<Usuario?> ObtenerPorGoogleAsync(string idGoogle);
        Task VincularGoogleAsync(int idUsuario, string idGoogle);
        Task<Usuario> CrearUsuarioGoogleAsync(Usuario usuario, string idGoogle);

    }
}