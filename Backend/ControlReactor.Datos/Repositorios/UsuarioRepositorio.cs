using ControlReactor.Datos.Contexto;
using ControlReactor.Modelos.Entidades;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Negocio.Interfaces;

namespace ControlReactor.Datos.Repositorios
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly ControlReactorDbContext _context;

        public UsuarioRepositorio(ControlReactorDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
        {
            return await _context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
        }

        public async Task<bool> ExisteCorreoAsync(string correo)
        {
            return await _context.Usuarios.AnyAsync(u => u.Correo == correo);
        }

        public async Task<Usuario> CrearAsync(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return usuario;
        }

        // Busca un usuario por su correo.
        public async Task<Usuario?> ObtenerPorCorreoAsync(string correo)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
        }

        // Guarda los cambios de un usuario.
        public async Task ActualizarAsync(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }
        public async Task<Usuario?> ObtenerPorIdAsync(int idUsuario)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        }

        // Busca un usuario vinculado a una cuenta de Google
        public async Task<Usuario?> ObtenerPorGoogleAsync(string idGoogle)
        {
            return await _context.UsuariosLoginGoogle
                .Where(g => g.IdGoogle == idGoogle && g.Proveedor == "Google")
                .Select(g => g.Usuario)
                .FirstOrDefaultAsync();
        }

        // Vincula una cuenta de Google con un usuario del juego
        public async Task VincularGoogleAsync(int idUsuario, string idGoogle)
        {
            var vinculacion = new UsuarioLoginGoogle
            {
                IdUsuario = idUsuario,
                Proveedor = "Google",
                IdGoogle = idGoogle
            };

            _context.UsuariosLoginGoogle.Add(vinculacion);

            await _context.SaveChangesAsync();
        }

        // Crea un usuario y vincula su cuenta de Google
        public async Task<Usuario> CrearUsuarioGoogleAsync(
            Usuario usuario,
            string idGoogle)
        {
            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            // Crear usuario
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Crear vinculación con Google
            var vinculacion = new UsuarioLoginGoogle
            {
                IdUsuario = usuario.IdUsuario,
                Proveedor = "Google",
                IdGoogle = idGoogle
            };

            _context.UsuariosLoginGoogle.Add(vinculacion);
            await _context.SaveChangesAsync();

            // Confirmar ambas operaciones
            await transaccion.CommitAsync();

            return usuario;
        }

        // Comprueba si el usuario ya tiene una cuenta de Google vinculada
        public async Task<bool> TieneGoogleVinculadoAsync(int idUsuario)
        {
            return await _context.UsuariosLoginGoogle
                .AnyAsync(g => g.IdUsuario == idUsuario
                            && g.Proveedor == "Google");
        }

    }
}