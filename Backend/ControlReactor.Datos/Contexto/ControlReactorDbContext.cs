using Microsoft.EntityFrameworkCore;
using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Datos.Contexto
{
    public class ControlReactorDbContext : DbContext
    {
        public ControlReactorDbContext(DbContextOptions<ControlReactorDbContext> options): base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }

        public DbSet<Partida> Partidas { get; set; }

        public DbSet<JugadorPartida> JugadoresPartida { get; set; }

        public DbSet<Logro> Logros { get; set; }

        public DbSet<UsuarioLogro> UsuariosLogros { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // USUARIO
            modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);

            modelBuilder.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();

            modelBuilder.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();

            // PARTIDA
            modelBuilder.Entity<Partida>().HasKey(p => p.IdPartida);

            // JUGADOR - PARTIDA
            modelBuilder.Entity<JugadorPartida>().HasKey(jp => jp.IdJugadorPartida);

            modelBuilder.Entity<JugadorPartida>().HasOne(jp => jp.Usuario).WithMany().HasForeignKey(jp => jp.IdUsuario);

            modelBuilder.Entity<JugadorPartida>().HasOne(jp => jp.Partida).WithMany().HasForeignKey(jp => jp.IdPartida);

            // LOGRO
            modelBuilder.Entity<Logro>().HasKey(l => l.IdLogro);

            // USUARIO - LOGRO
            modelBuilder.Entity<UsuarioLogro>().HasKey(ul => ul.IdUsuarioLogro);

            modelBuilder.Entity<UsuarioLogro>().HasOne(ul => ul.Usuario).WithMany().HasForeignKey(ul => ul.IdUsuario);

            modelBuilder.Entity<UsuarioLogro>().HasOne(ul => ul.Logro).WithMany().HasForeignKey(ul => ul.IdLogro);
        }
    }
}