namespace ControlReactor.Modelos.DTOs
{
    public class GuardarConfiguracionPartidaDto
    {
        public int IdUsuario { get; set; }

        public string TipoFinalizacion { get; set; } = string.Empty;

        public int? LimiteTiempo { get; set; }

        public int? PuntajeObjetivo { get; set; }

        public int SegundosCongelamiento { get; set; }
    }
}