using Microsoft.AspNetCore.Identity;
using ControlReactor.Datos.Contexto;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ControlReactor.Datos.Repositorios
{
    public class CodigoVerificacionRepositorio : ICodigoVerificacionRepositorio
    {
        private readonly ControlReactorDbContext _contexto;

        public CodigoVerificacionRepositorio(ControlReactorDbContext contexto)
        {
            _contexto = contexto;
        }

        // Guarda un nuevo código de verificación.
        public async Task GuardarAsync(CodigoVerificacion codigo)
        {
            _contexto.CodigosVerificacion.Add(codigo);
            await _contexto.SaveChangesAsync();
        }

        // Obtiene el código más reciente del usuario.
        public async Task<CodigoVerificacion?> ObtenerUltimoAsync(int idUsuario)
        {
            return await _contexto.CodigosVerificacion.Where(c => c.IdUsuario == idUsuario).OrderByDescending(c => c.IdCodigoVerificacion).FirstOrDefaultAsync();
        }

        // Guarda los cambios realizados al código.
        public async Task ActualizarAsync(CodigoVerificacion codigo)
        {
            _contexto.CodigosVerificacion.Update(codigo);
            await _contexto.SaveChangesAsync();
        }

        // Valida y consume el código dentro de una transacción.
        public async Task<bool> ConfirmarVerificacionAsync(int idUsuario, int idCodigoVerificacion, string codigo)
        {
            await using var transaccion = await _contexto.Database.BeginTransactionAsync();

            // Bloquea la fila del usuario durante la verificación.
            var usuarios = await _contexto.Usuarios.FromSqlInterpolated($"SELECT * FROM \"Usuarios\" WHERE \"IdUsuario\" = {idUsuario} FOR UPDATE").ToListAsync();

            var usuario = usuarios.FirstOrDefault();

            if (usuario == null || usuario.CorreoVerificado)return false;

            var registro = await _contexto.CodigosVerificacion.FirstOrDefaultAsync(c => c.IdCodigoVerificacion == idCodigoVerificacion && c.IdUsuario == idUsuario);

            if (registro == null ||
                registro.Utilizado ||
                registro.FechaExpiracion <= DateTime.UtcNow ||
                registro.IntentosFallidos >= 5)
            {
                return false;
            }

            var hasher = new PasswordHasher<CodigoVerificacion>();

            var resultado = hasher.VerifyHashedPassword(registro, registro.CodigoHash, codigo);

            if (resultado == PasswordVerificationResult.Failed)
            {
                // Registra el intento fallido antes de liberar el bloqueo.
                registro.IntentosFallidos++;
                await _contexto.SaveChangesAsync();
                await transaccion.CommitAsync();

                return false;
            }

            registro.Utilizado = true;
            usuario.CorreoVerificado = true;

            await _contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return true;
        }

        // Marca como utilizados los códigos anteriores del usuario
        public async Task InvalidarCodigosAnterioresAsync(int idUsuario)
        {
            // Buscar todos los códigos que todavía no han sido utilizados
            var codigos = await _contexto.CodigosVerificacion.Where(c => c.IdUsuario == idUsuario && !c.Utilizado).ToListAsync();

            // Invalidar los códigos encontrados
            foreach (var codigo in codigos)
            {
                codigo.Utilizado = true;
            }

            // Guardar los cambios en PostgreSQL
            await _contexto.SaveChangesAsync();
        }
    }
}