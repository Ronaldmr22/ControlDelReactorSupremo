using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioLoginGoogle
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;

        public ServicioLoginGoogle(IUsuarioRepositorio usuarioRepositorio)
        {
            _usuarioRepositorio = usuarioRepositorio;
        }

        // Busca un usuario que ya tenga vinculada su cuenta de Google
        public async Task<Usuario?> BuscarUsuarioGoogleAsync(string idGoogle)
        {
            if (string.IsNullOrWhiteSpace(idGoogle))
            {
                return null;
            }

            return await _usuarioRepositorio.ObtenerPorGoogleAsync(idGoogle);
        }

        public async Task<(bool Exito, string Mensaje, Usuario? Usuario)> LoginAsync(
            string idGoogle,
            string correo,
            string nombre,
            bool correoVerificado)
        {
            // Validar los datos recibidos de Google
            if (string.IsNullOrWhiteSpace(idGoogle) ||
                string.IsNullOrWhiteSpace(correo) ||
                !correoVerificado)
            {
                return (false, "No se pudo verificar la cuenta de Google.", null);
            }

            correo = correo.Trim().ToLowerInvariant();

            // 1. Buscar si esta cuenta de Google ya está vinculada
            var usuarioExistente =
                await _usuarioRepositorio.ObtenerPorGoogleAsync(idGoogle);

            if (usuarioExistente != null)
            {
                return (true, "Inicio de sesión exitoso.", usuarioExistente);
            }

            // 2. Comprobar si el correo pertenece a una cuenta tradicional
            var usuarioPorCorreo =
                await _usuarioRepositorio.ObtenerPorCorreoAsync(correo);

            if (usuarioPorCorreo != null)
            {
                return (false,
                    "Este correo ya está registrado. Inicia sesión con tu cuenta existente.",
                    null);
            }

            // 3. Generar un nombre de usuario único
            var nombreBase = correo.Split('@')[0];

            if (nombreBase.Length > 20)
            {
                nombreBase = nombreBase[..20];
            }

            var nombreUsuario = nombreBase;
            var contador = 1;

            while (await _usuarioRepositorio.ExisteNombreUsuarioAsync(nombreUsuario))
            {
                nombreUsuario = $"{nombreBase}{contador}";
                contador++;
            }

            // 4. Crear el usuario
            var nuevoUsuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Correo = correo,
                CorreoVerificado = true,
                PasswordHash = string.Empty
            };

            // 5. Guardar el usuario y su vinculación con Google
            var usuarioCreado =
                await _usuarioRepositorio.CrearUsuarioGoogleAsync(
                    nuevoUsuario,
                    idGoogle
                );

            return (true, "Cuenta de Google registrada correctamente.", usuarioCreado);
        }
    }
}