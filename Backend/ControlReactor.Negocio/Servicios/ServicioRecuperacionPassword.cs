using Microsoft.AspNetCore.Identity;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioRecuperacionPassword
    {
        private readonly ICodigoRecuperacionRepositorio _repositorio;
        private readonly ServicioCodigoVerificacion _generador;
        private readonly ServicioEnvioCorreo _envioCorreo;
        private readonly IUsuarioRepositorio _usuarios;

        public ServicioRecuperacionPassword(
            ICodigoRecuperacionRepositorio repositorio,
            ServicioCodigoVerificacion generador,
            ServicioEnvioCorreo envioCorreo,
            IUsuarioRepositorio usuarios)
        {
            _repositorio = repositorio;
            _generador = generador;
            _envioCorreo = envioCorreo;
            _usuarios = usuarios;
        }

        // Genera, guarda y envía un código de recuperación.
        public async Task GenerarYEnviarAsync(Usuario usuario)
        {
            // Generar un código seguro de seis dígitos.
            string codigo = _generador.GenerarCodigo();

            // Invalidar los códigos anteriores del usuario.
            await _repositorio.InvalidarCodigosAnterioresAsync(usuario.IdUsuario);

            // Crear el registro que guardaremos en PostgreSQL.
            var registro = new CodigoRecuperacion
            {
                IdUsuario = usuario.IdUsuario,
                FechaExpiracion = _generador.ObtenerExpiracion(),
                Utilizado = false
            };

            // Guardar el código como hash, no como texto.
            var hasher = new PasswordHasher<CodigoRecuperacion>();

            registro.CodigoHash = hasher.HashPassword(registro, codigo);

            // Guardar el registro en PostgreSQL.
            await _repositorio.GuardarAsync(registro);

            // Enviar el código real al correo del usuario.
            await _envioCorreo.EnviarCodigoRecuperacionAsync(
                usuario.Correo,
                codigo
            );
        }

        // Solicita un código para recuperar la contraseña.
        public async Task<(bool Exito, string Mensaje)> SolicitarRecuperacionAsync(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                return (false, "Debes ingresar un correo electrónico.");
            }

            // Normalizar el correo.
            correo = correo.Trim().ToLowerInvariant();

            // Buscar al usuario.
            var usuario = await _usuarios.ObtenerPorCorreoAsync(correo);

            // No revelar si el correo está registrado.
            string mensaje = "Si el correo está registrado, recibirás un código de recuperación.";

            if (usuario == null)
            {
                return (true, mensaje);
            }

            // Obtener el último código generado.
            var ultimoCodigo = await _repositorio.ObtenerUltimoAsync(usuario.IdUsuario);

            // Evitar solicitudes repetidas durante 60 segundos.
            if (ultimoCodigo != null &&
                DateTime.UtcNow < ultimoCodigo.FechaCreacion.AddSeconds(60))
            {
                return (true, mensaje);
            }

            // Generar, guardar y enviar el nuevo código.
            await GenerarYEnviarAsync(usuario);

            return (true, mensaje);
        }
        // Comprueba si el código de recuperación es válido.
        public async Task<(bool Exito, string Mensaje)> VerificarCodigoAsync(string correo, string codigo)
        {
            // Comprobar que los campos no estén vacíos.
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(codigo))
            {
                return (false, "El correo y el código son obligatorios.");
            }

            correo = correo.Trim().ToLowerInvariant();

            // Buscar al usuario.
            var usuario = await _usuarios.ObtenerPorCorreoAsync(correo);

            if (usuario == null)
            {
                return (false, "El código no es válido o ha expirado.");
            }

            // Obtener el código más reciente.
            var registro = await _repositorio.ObtenerUltimoAsync(usuario.IdUsuario);

            // Comprobar que exista, no haya sido utilizado y no esté vencido.
            if (registro == null ||
                registro.Utilizado ||
                registro.FechaExpiracion <= DateTime.UtcNow ||
                registro.IntentosFallidos >= 5)
            {
                return (false, "El código no es válido o ha expirado.");
            }

            // Comprobar el código ingresado contra el hash almacenado.
            var hasher = new PasswordHasher<CodigoRecuperacion>();

            var resultado = hasher.VerifyHashedPassword(
                registro,
                registro.CodigoHash,
                codigo
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                // Registrar el intento incorrecto.
                registro.IntentosFallidos++;

                await _repositorio.ActualizarAsync(registro);

                return (false, "El código no es válido o ha expirado.");
            }

            return (true, "Código de recuperación válido.");
        }

        // Cambia la contraseña utilizando un código de recuperación.
        public async Task<(bool Exito, string Mensaje)> CambiarPasswordAsync(string correo,string codigo,string nuevaPassword)
        {
            // Comprobar que los campos no estén vacíos.
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nuevaPassword))
            {
                return (false, "Todos los campos son obligatorios.");
            }

            // Validar la longitud de la contraseña.
            if (nuevaPassword.Length < 8)
            {
                return (false, "La contraseña debe tener al menos 8 caracteres.");
            }

            correo = correo.Trim().ToLowerInvariant();

            // Buscar al usuario por su correo.
            var usuario = await _usuarios.ObtenerPorCorreoAsync(correo);

            if (usuario == null)
            {
                return (false, "No se pudo restablecer la contraseña.");
            }

            // Obtener el código de recuperación más reciente.
            var registro = await _repositorio.ObtenerUltimoAsync(usuario.IdUsuario);

            if (registro == null)
            {
                return (false, "No se pudo restablecer la contraseña.");
            }

            // Verificar el código y cambiar la contraseña en una transacción.
            bool actualizado = await _repositorio.RestablecerPasswordAsync(
                usuario.IdUsuario,
                registro.IdCodigoRecuperacion,
                codigo,
                nuevaPassword
            );

            if (!actualizado)
            {
                return (false, "El código no es válido o ha expirado.");
            }

            return (true, "Contraseña actualizada correctamente.");
        }
    }
}