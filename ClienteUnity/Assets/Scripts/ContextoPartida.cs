public static class ContextoPartida
{
    public static string CodigoSala { get; private set; }
    public static int IdPartida { get; private set; }

    public static void Guardar(DatosSala sala)
    {
        CodigoSala = sala.codigoSala;
        IdPartida = sala.idPartida;
    }

    public static void Limpiar()
    {
        CodigoSala = null;
        IdPartida = 0;
    }
}
