using System;

[Serializable]
public class DatosSala
{
    public int idPartida;
    public string codigoSala;
    public string estado;
    public int idUsuarioCreador;
    public ParticipanteSala[] participantes;

    // Configuración de la partida
    public string tipoFinalizacion;
    public int limiteTiempo;
    public int puntajeObjetivo;
    public string fechaInicio;
}

[Serializable]
public class ParticipanteSala
{
    public int idUsuario;
    public string nombreUsuario;
    public int avatarSeleccionado;
    public string fotoPerfil;
}
