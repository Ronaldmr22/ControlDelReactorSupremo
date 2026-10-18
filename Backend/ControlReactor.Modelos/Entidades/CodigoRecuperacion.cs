namespace ControlReactor.Modelos.Entidades
{
    public class CodigoRecuperacion
    {
        public int IdCodigoRecuperacion { get; set; }

        public int IdUsuario { get; set; }

        public string CodigoHash { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime FechaExpiracion { get; set; }

        public bool Utilizado { get; set; } = false;

        public int IntentosFallidos { get; set; } = 0;

        public Usuario Usuario { get; set; } = null!;
    }
}