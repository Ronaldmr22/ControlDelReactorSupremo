public static class ContextoPartida
{
    public static string CodigoSala { get; private set; }
    public static int IdPartida { get; private set; }

    public static int IdJugador1 { get; private set; }
    public static int IdJugador2 { get; private set; }
    public static int IdJugador3 { get; private set; }
    public static ParticipanteSala[] Participantes { get; private set; }
    public static string TipoFinalizacion { get; private set; }
    public static int LimiteTiempo { get; private set; }
    public static int PuntajeObjetivo { get; private set; }
    public static string FechaInicio { get; private set; }

    public static void Guardar(DatosSala sala)
    {
        CodigoSala = sala.codigoSala;
        IdPartida = sala.idPartida;
        Participantes = sala.participantes;
        TipoFinalizacion = sala.tipoFinalizacion;
        LimiteTiempo = sala.limiteTiempo;
        PuntajeObjetivo = sala.puntajeObjetivo;
        FechaInicio = sala.fechaInicio;

        // El creador siempre es el jugador 1.
        IdJugador1 = sala.idUsuarioCreador;
        IdJugador2 = 0;
        IdJugador3 = 0;

        if (sala.participantes == null)
            return;

        foreach (var participante in sala.participantes)
        {
            int id = participante.idUsuario;

            if (id == IdJugador1)
                continue;

            if (IdJugador2 == 0)
            {
                IdJugador2 = id;
            }
            else if (IdJugador3 == 0 && id != IdJugador2)
            {
                IdJugador3 = id;
            }
        }
    }

    public static void Limpiar()
    {
        CodigoSala = null;
        IdPartida = 0;

        IdJugador1 = 0;
        IdJugador2 = 0;
        IdJugador3 = 0;
        Participantes = null;
        TipoFinalizacion = null;
        LimiteTiempo = 0;
        PuntajeObjetivo = 0;
        FechaInicio = null;
    }
}
