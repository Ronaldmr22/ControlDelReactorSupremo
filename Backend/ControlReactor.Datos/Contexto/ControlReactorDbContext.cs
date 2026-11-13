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
        public DbSet<CodigoVerificacion> CodigosVerificacion { get; set; }
        public DbSet<CodigoRecuperacion> CodigosRecuperacion { get; set; }
        public DbSet<UsuarioLoginGoogle> UsuariosLoginGoogle { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // USUARIO
            modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);

            modelBuilder.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();

            modelBuilder.Entity<Usuario>().HasIndex(u => u.Correo).IsUnique();

            // PARTIDA
            modelBuilder.Entity<Partida>().HasKey(p => p.IdPartida);
            modelBuilder.Entity<Partida>()
            .HasIndex(p => p.CodigoSala)
            .IsUnique();

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
        
            // CÓDIGO DE VERIFICACIÓN
            modelBuilder.Entity<CodigoVerificacion>().HasKey(c => c.IdCodigoVerificacion);

            modelBuilder.Entity<CodigoVerificacion>().HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.IdUsuario);
        
            // CÓDIGO DE RECUPERACIÓN DE CONTRASEÑA
            modelBuilder.Entity<CodigoRecuperacion>().HasKey(c => c.IdCodigoRecuperacion);
            modelBuilder.Entity<CodigoRecuperacion>().HasOne(c => c.Usuario).WithMany().HasForeignKey(c => c.IdUsuario);

            // INICIO DE SESIÓN CON GOOGLE
            modelBuilder.Entity<UsuarioLoginGoogle>().HasKey(g => g.IdUsuarioLoginGoogle);
            modelBuilder.Entity<UsuarioLoginGoogle>().HasOne(g => g.Usuario).WithMany().HasForeignKey(g => g.IdUsuario).OnDelete(DeleteBehavior.Cascade);

            // Una cuenta de Google solo puede estar vinculada a un usuario.
            modelBuilder.Entity<UsuarioLoginGoogle>().HasIndex(g => g.IdGoogle).IsUnique();

            // Un usuario solo puede vincular una cuenta de Google.
            modelBuilder.Entity<UsuarioLoginGoogle>().HasIndex(g => g.IdUsuario).IsUnique();
            modelBuilder.Entity<UsuarioLoginGoogle>().Property(g => g.IdGoogle).IsRequired();
        }
    }
}