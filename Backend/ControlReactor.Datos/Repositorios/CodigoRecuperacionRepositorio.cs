using ControlReactor.Datos.Contexto;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ControlReactor.Datos.Repositorios
{
    public class CodigoRecuperacionRepositorio : ICodigoRecuperacionRepositorio
    {
        private readonly ControlReactorDbContext _contexto;

        public CodigoRecuperacionRepositorio(ControlReactorDbContext contexto)
        {
            _contexto = contexto;
        }

        // Guarda un nuevo código de recuperación.
        public async Task GuardarAsync(CodigoRecuperacion codigo)
        {
            _contexto.CodigosRecuperacion.Add(codigo);
            await _contexto.SaveChangesAsync();
        }

        // Obtiene el código más reciente del usuario.
        public async Task<CodigoRecuperacion?> ObtenerUltimoAsync(int idUsuario)
        {
            return await _contexto.CodigosRecuperacion
                .Where(c => c.IdUsuario == idUsuario)
                .OrderByDescending(c => c.IdCodigoRecuperacion)
                .FirstOrDefaultAsync();
        }

        // Actualiza un código existente.
        public async Task ActualizarAsync(CodigoRecuperacion codigo)
        {
            _contexto.CodigosRecuperacion.Update(codigo);
            await _contexto.SaveChangesAsync();
        }

        // Invalida los códigos anteriores del usuario.
        public async Task InvalidarCodigosAnterioresAsync(int idUsuario)
        {
            var codigos = await _contexto.CodigosRecuperacion.Where(c => c.IdUsuario == idUsuario && !c.Utilizado).ToListAsync();

            foreach (var codigo in codigos)
            {
                codigo.Utilizado = true;
            }

            await _contexto.SaveChangesAsync();
        }

        // Cambia la contraseña y consume el código en una transacción.
        public async Task<bool> RestablecerPasswordAsync(int idUsuario,int idCodigoRecuperacion,string codigo,string nuevaPassword)
        {
            await using var transaccion =  await _contexto.Database.BeginTransactionAsync();

            // Bloquear al usuario para evitar cambios simultáneos.
            var usuarios = await _contexto.Usuarios.FromSqlInterpolated($"SELECT * FROM \"Usuarios\" WHERE \"IdUsuario\" = {idUsuario} FOR UPDATE").ToListAsync();

            var usuario = usuarios.FirstOrDefault();

            if (usuario == null)
            {
                return false;
            }

            // Buscar el código más reciente del usuario.
            var registro = await _contexto.CodigosRecuperacion.Where(c => c.IdUsuario == idUsuario).OrderByDescending(c => c.IdCodigoRecuperacion).FirstOrDefaultAsync();

            if (registro == null ||
                registro.IdCodigoRecuperacion != idCodigoRecuperacion ||
                registro.Utilizado ||
                registro.FechaExpiracion <= DateTime.UtcNow ||
                registro.IntentosFallidos >= 5)
            {
                return false;
            }

            // Comprobar el código ingresado.
            var hasherCodigo = new PasswordHasher<CodigoRecuperacion>();

            var resultado = hasherCodigo.VerifyHashedPassword(
                registro,
                registro.CodigoHash,
                codigo
            );

            if (resultado == PasswordVerificationResult.Failed)
            {
                registro.IntentosFallidos++;

                await _contexto.SaveChangesAsync();
                await transaccion.CommitAsync();

                return false;
            }

            // Generar el hash de la nueva contraseña.
            var hasherPassword = new PasswordHasher<Usuario>();

            usuario.PasswordHash = hasherPassword.HashPassword(usuario,nuevaPassword);

            // Marcar el código como utilizado.
            registro.Utilizado = true;

            // Guardar ambos cambios juntos.
            await _contexto.SaveChangesAsync();

            await transaccion.CommitAsync();

            return true;
        }
        
    }
}