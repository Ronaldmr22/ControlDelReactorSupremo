
public static class ContextoPartida
{
    public static string CodigoSala { get; private set; }
    public static int IdPartida { get; private set; }

    public static int IdJugador1 { get; private set; }
    public static int IdJugador2 { get; private set; }

    public static void Guardar(DatosSala sala)
    {
        CodigoSala = sala.codigoSala;
        IdPartida = sala.idPartida;

        // El jugador 1 es quien creó la sala.
        IdJugador1 = sala.idUsuarioCreador;
        IdJugador2 = 0;

        // El jugador 2 es el otro participante.
        if (sala.participantes != null)
        {
            foreach (var participante in sala.participantes)
            {
                if (participante.idUsuario != IdJugador1)
                {
                    IdJugador2 = participante.idUsuario;
                    break;
                }
            }
        }
    }

    public static void Limpiar()
    {
        CodigoSala = null;
        IdPartida = 0;
        IdJugador1 = 0;
        IdJugador2 = 0;
    }
}

