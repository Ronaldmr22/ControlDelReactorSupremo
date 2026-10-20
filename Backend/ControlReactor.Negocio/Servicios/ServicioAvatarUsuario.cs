
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioAvatarUsuario
    {
        private readonly IUsuarioRepositorio _repositorio;

        public ServicioAvatarUsuario(IUsuarioRepositorio repositorio)
        {
            _repositorio = repositorio;
        }
        public async Task<(bool Exito, string Mensaje)> ActualizarAvatarAsync(
            int idUsuario, int avatarSeleccionado)
        {
            // Comprobar que el avatar sea válido.
            if (avatarSeleccionado < 1 || avatarSeleccionado > 5)
            {
                return (false, "El avatar seleccionado no es válido.");
            }

            // Buscar al usuario en la base de datos.
            var usuario = await _repositorio.ObtenerPorIdAsync(idUsuario);

            if (usuario == null)
            {
                return (false, "El usuario no existe.");
            }

            // Actualizar el avatar seleccionado.
            usuario.AvatarSeleccionado = avatarSeleccionado;

            // Guardar los cambios en PostgreSQL.
            await _repositorio.ActualizarAsync(usuario);

            return (true, "Avatar actualizado correctamente.");
        }
    }
}
