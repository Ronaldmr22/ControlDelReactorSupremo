
using ControlReactor.Modelos.DTOs;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioLoginUsuario
    {
        private readonly IUsuarioRepositorio _repositorio;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public ServicioLoginUsuario(IUsuarioRepositorio repositorio)
        {
            _repositorio = repositorio;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        public async Task<(bool Exito, string Mensaje, Usuario? Usuario)> LoginAsync(LoginUsuarioDto dto)
        {
            // 1. Validar que se ingresaron los datos.
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Correo) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return (false, "El correo y la contraseña son obligatorios.", null);
            }

            // 2. Normalizar el correo.
            string correo = dto.Correo.Trim().ToLowerInvariant();

            // 3. Buscar al usuario en PostgreSQL.
            Usuario? usuario = await _repositorio.ObtenerPorCorreoAsync(correo);

            // 4. Comprobar si existe.
            if (usuario == null)
            {
                return (false, "Correo o contraseña incorrectos.", null);
            }

            // 5. Verificar la contraseña contra el hash.
            var resultado = _passwordHasher.VerifyHashedPassword( usuario, usuario.PasswordHash, dto.Password);

            if (resultado == PasswordVerificationResult.Failed)
            {
                return (false, "Correo o contraseña incorrectos.", null);
            }

            // 6. Comprobar la verificación del correo.
            if (!usuario.CorreoVerificado)
            {
                return (false, "Debes verificar tu correo electrónico.", null);
            }

            // 7. Credenciales válidas.
            return (true, "Credenciales verificadas correctamente.", usuario);
        }
    }
}
