namespace ControlReactor.Modelos.Entidades
{
    public class JugadorPartida
    {
        public int IdJugadorPartida { get; set; }

        public int IdUsuario { get; set; }

        public int IdPartida { get; set; }

        public int Puntaje { get; set; } = 0;

        public bool EsGanador { get; set; } = false;
        
        // Permite liberar el espacio sin borrar el registro.
        public bool EnSala { get; set; } = false;
        public DateTime? UltimaActividadUtc { get; set; }

        public Usuario Usuario { get; set; } = null!;

        public Partida Partida { get; set; } = null!;
    }
}