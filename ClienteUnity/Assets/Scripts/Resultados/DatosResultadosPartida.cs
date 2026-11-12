using System;

[Serializable]
public class DatosResultadoPartida
{
    public int idPartida;
    public string estado;
    public GanadorPartida ganador;

    // Clasificación completa de la partida.
    public JugadorClasificacion[] clasificacion;
}

[Serializable]
public class GanadorPartida
{
    public int idUsuario;
    public string nombreUsuario;
    public int avatarSeleccionado;
    public int puntaje;
}

[Serializable]
public class JugadorClasificacion
{
    public int posicion;
    public int idUsuario;
    public string nombreUsuario;
    public int avatarSeleccionado;
    public int puntaje;
    public bool esGanador;
}
