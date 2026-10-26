using System;

[Serializable]
public class DatosResultadoPartida
{
    public int idPartida;
    public string estado;
    public GanadorPartida ganador;
}

[Serializable]
public class GanadorPartida
{
    public int idUsuario;
    public string nombreUsuario;
    public int avatarSeleccionado;
    public int puntaje;
}


