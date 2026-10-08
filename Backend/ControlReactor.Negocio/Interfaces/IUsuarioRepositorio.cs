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
    }
}