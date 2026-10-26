using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioVerificacionCorreo
    {
        private readonly ICodigoVerificacionRepositorio _repositorio;
        private readonly ServicioCodigoVerificacion _generador;
        private readonly ServicioEnvioCorreo _envioCorreo;
        private readonly IUsuarioRepositorio _usuarios;

        public ServicioVerificacionCorreo(ICodigoVerificacionRepositorio repositorio,ServicioCodigoVerificacion generador,ServicioEnvioCorreo envioCorreo, IUsuarioRepositorio usuarios)
        {
            _repositorio = repositorio;
            _generador = generador;
            _envioCorreo = envioCorreo;
            _usuarios = usuarios;
        }

        // Genera, guarda y envía el código de verificación.
        public async Task GenerarYEnviarAsync(Usuario usuario)
        {
            string codigo = _generador.GenerarCodigo();

            var registro = new CodigoVerificacion
            {
                IdUsuario = usuario.IdUsuario,
                FechaExpiracion = _generador.ObtenerExpiracion(),
                Utilizado = false
            };

            var hasher = new PasswordHasher<CodigoVerificacion>();

            registro.CodigoHash = hasher.HashPassword(registro, codigo);

            await _repositorio.GuardarAsync(registro);

            await _envioCorreo.EnviarCodigoAsync(usuario.Correo, codigo);
        }


        // Comprueba el código recibido y verifica la cuenta.
        public async Task<(bool Exito, string Mensaje)> VerificarAsync(string correo, string codigo)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(codigo))
            {
                return (false, "El correo y el código son obligatorios.");
            }

            correo = correo.Trim().ToLowerInvariant();

            var usuario = await _usuarios.ObtenerPorCorreoAsync(correo);

            if (usuario == null) return (false, "No se pudo verificar el correo.");

            if (usuario.CorreoVerificado) return (false, "Este correo ya está verificado.");

            var registro = await _repositorio.ObtenerUltimoAsync(usuario.IdUsuario);

            if (registro == null || registro.Utilizado || registro.FechaExpiracion <= DateTime.UtcNow)
            {
                return (false, "El código no es válido o ha expirado.");
            }

            if (registro.IntentosFallidos >= 5) return (false, "Se alcanzó el límite de intentos.");

            var hasher = new PasswordHasher<CodigoVerificacion>();

            var resultado = hasher.VerifyHashedPassword(registro, registro.CodigoHash, codigo);

            if (resultado == PasswordVerificationResult.Failed)
            {
                // Registra el intento incorrecto.
                registro.IntentosFallidos++;
                await _repositorio.ActualizarAsync(registro);
                return (false, "El código es incorrecto.");
            }

            // Comprueba el código y confirma la verificación.
            bool confirmado = await _repositorio.ConfirmarVerificacionAsync(
                usuario.IdUsuario,
                registro.IdCodigoVerificacion,
                codigo
            );

            if (!confirmado)
                return (false, "Código inválido o expirado.");

            return (true, "Correo verificado correctamente.");

        }

        // Permite solicitar un nuevo código de verificación
        public async Task<(bool Exito, string Mensaje)> ReenviarCodigoAsync(string correo)
        {
            // Validar que se haya ingresado un correo
            if (string.IsNullOrWhiteSpace(correo))return (false, "Debes ingresar un correo.");
            correo = correo.Trim().ToLowerInvariant();

            // Buscar al usuario en la base de datos
            var usuario = await _usuarios.ObtenerPorCorreoAsync(correo);

            // No revelar si la cuenta existe o ya está verificada
            if (usuario == null || usuario.CorreoVerificado) return (true, "Si la cuenta existe y está pendiente de verificación, recibirás un código.");

            // Obtener el último código generado
            var ultimoCodigo = await _repositorio.ObtenerUltimoAsync(usuario.IdUsuario);

            // Comprobar que hayan pasado al menos 60 segundos
            if (ultimoCodigo != null && DateTime.UtcNow < ultimoCodigo.FechaCreacion.AddSeconds(60)) 
                return (false, "Debes esperar 60 segundos antes de solicitar otro código.");

            // Invalidar los códigos anteriores del usuario
            await _repositorio.InvalidarCodigosAnterioresAsync(usuario.IdUsuario);
            
            // Generar y enviar un nuevo código mediante Brevo
            await GenerarYEnviarAsync(usuario);

            return (true, "Si la cuenta existe y está pendiente de verificación, recibirás un código.");
        }
    }
}