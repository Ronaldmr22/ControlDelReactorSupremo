namespace ControlReactor.Modelos.Entidades
{
    public class Partida
    {
        public int IdPartida { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        public string TipoFinalizacion { get; set; } = string.Empty;

        public int? LimiteTiempo { get; set; }

        public int? PuntajeObjetivo { get; set; }

        public string Estado { get; set; } = "ESPERANDO";

        public int SegundosCongelamiento { get; set; }
    }
}