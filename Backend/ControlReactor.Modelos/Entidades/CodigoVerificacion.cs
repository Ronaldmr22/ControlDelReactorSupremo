namespace ControlReactor.Modelos.Entidades
{
    public class CodigoVerificacion
    {
        public int IdCodigoVerificacion { get; set; }

        public int IdUsuario { get; set; }

        public string CodigoHash { get; set; } = string.Empty;

        public DateTime FechaExpiracion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public bool Utilizado { get; set; } = false;

        public Usuario Usuario { get; set; } = null!;
        public int IntentosFallidos { get; set; } = 0;
    }
}