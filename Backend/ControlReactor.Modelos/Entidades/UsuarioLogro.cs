namespace ControlReactor.Modelos.Entidades
{
    public class UsuarioLogro
    {
        public int IdUsuarioLogro { get; set; }

        public int IdUsuario { get; set; }

        public int IdLogro { get; set; }

        public DateTime FechaObtencion { get; set; }

        public Usuario Usuario { get; set; } = null!;

        public Logro Logro { get; set; } = null!;
    }
}