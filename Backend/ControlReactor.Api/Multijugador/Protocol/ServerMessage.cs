
using ControlReactor.Api.Multijugador.Players;

namespace ControlReactor.Api.Multijugador.Protocol;

public class ServerMessage
{
    public string Type { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string? RoomCode { get; set; }

    public string? PlayerId { get; set; }

    public string? PlayerName { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public int? IdJugadorPartida { get; set; }

    public int? IdUsuario { get; set; }

    public int? Puntaje { get; set; }

    // Clasificación final de todos los participantes.
    public List<ResultadoJugador>? Resultados { get; set; }
}

public class ResultadoJugador
{
    public int Posicion { get; set; }

    public int IdJugadorPartida { get; set; }

    public int IdUsuario { get; set; }

    public int Puntaje { get; set; }

    public bool EsGanador { get; set; }
}
