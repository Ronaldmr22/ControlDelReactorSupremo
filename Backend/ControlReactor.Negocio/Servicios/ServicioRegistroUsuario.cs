using ControlReactor.Modelos.DTOs;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Net.Mail;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioRegistroUsuario
    {
        private readonly IUsuarioRepositorio _repositorio;
        private readonly ServicioValidacionCorreo _validadorCorreo;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public ServicioRegistroUsuario(IUsuarioRepositorio repositorio, ServicioValidacionCorreo validadorCorreo)
        {
            _repositorio = repositorio;
            _validadorCorreo = validadorCorreo;
            _passwordHasher = new PasswordHasher<Usuario>();
        }

        public async Task<(bool Exito, string Mensaje)> RegistrarAsync(RegistroUsuarioDto dto)
        {
            // 1. Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(dto.NombreUsuario) ||
                string.IsNullOrWhiteSpace(dto.Correo) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.ConfirmarPassword))
            {
                return (false, "Todos los campos son obligatorios.");
            }

            // 2. Validar contraseña
            if (dto.Password.Length < 8)
            {
                return (false, "La contraseña debe tener al menos 8 caracteres.");
            }

            if (dto.Password != dto.ConfirmarPassword)
            {
                return (false, "Las contraseñas no coinciden.");
            }

            // 3. Normalizar y validar correo
            string correo = dto.Correo.Trim().ToLowerInvariant();
            string nombreUsuario = dto.NombreUsuario.Trim();

            try
            {
                var direccion = new MailAddress(correo);

                if (direccion.Address != correo)
                {
                    return (false, "El formato del correo no es válido.");
                }
            }
            catch (FormatException)
            {
                return (false, "El formato del correo no es válido.");
            }

            // 4. Consultar duplicados
            if (await _repositorio.ExisteNombreUsuarioAsync(nombreUsuario))
            {
                return (false, "El nombre de usuario ya está registrado.");
            }

            if (await _repositorio.ExisteCorreoAsync(correo))
            {
                return (false, "El correo ya está registrado.");
            }

            // 5. Comprobar entregabilidad con Abstract API
            bool correoEntregable = await _validadorCorreo.CorreoExisteAsync(correo);

            if (!correoEntregable)
            {
                return (false,"No se pudo confirmar que el correo pueda recibir mensajes.");
            }

            // 6. Crear la entidad
            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Correo = correo,
                CorreoVerificado = false
            };

            // 7. Generar hash de contraseña
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario,dto.Password);

            // 8. Guardar en PostgreSQL
            await _repositorio.CrearAsync(usuario);

            return (true,"Usuario registrado. Debe verificar su correo para jugar.");
        }
    }
}