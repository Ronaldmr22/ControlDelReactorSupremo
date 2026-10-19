
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioFotoPerfil
    {
        private readonly IUsuarioRepositorio _repositorio;

        public ServicioFotoPerfil(IUsuarioRepositorio repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<(bool Exito, string Mensaje)> ActualizarFotoAsync(
            int idUsuario, string urlFoto)
        {
            if (string.IsNullOrWhiteSpace(urlFoto))
            {
                return (false, "La foto de perfil es obligatoria.");
            }

            var usuario = await _repositorio.ObtenerPorIdAsync(idUsuario);

            if (usuario == null)
            {
                return (false, "El usuario no existe.");
            }

            usuario.UrlFotoPerfil = urlFoto;

            await _repositorio.ActualizarAsync(usuario);

            return (true, "Foto de perfil actualizada correctamente.");
        }
    }
}
