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
    }
}