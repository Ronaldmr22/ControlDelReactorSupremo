namespace ControlReactor.Modelos.Entidades
{
    public class JugadorPartida
    {
        public int IdJugadorPartida { get; set; }

        public int IdUsuario { get; set; }

        public int IdPartida { get; set; }

        public int Puntaje { get; set; } = 0;

        public bool EsGanador { get; set; } = false;

        public Usuario Usuario { get; set; } = null!;

        public Partida Partida { get; set; } = null!;
    }
}