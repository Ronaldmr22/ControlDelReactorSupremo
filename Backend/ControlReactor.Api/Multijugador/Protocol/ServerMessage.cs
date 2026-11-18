
using ControlReactor.Api.Multijugador.Players;

namespace ControlReactor.Api.Multijugador.Protocol;

public class ServerMessage
{
    public string Type { get; set; } = string.Empty;
    public bool? SolicitarCierrePuertas { get; set; }
    public int[]? PuertasCerradas { get; set; }

    public string? Message { get; set; }

    public string? RoomCode { get; set; }

    public string? PlayerId { get; set; }

    public string? PlayerName { get; set; }

    public float X { get; set; }

    public float Y { get; set; }

    public int? IdJugadorPartida { get; set; }

    public int? IdUsuario { get; set; }

    public int? Puntaje { get; set; }
    public int? GuardianId { get; set; }

    public int? EnergiaRestante { get; set; }

    // Identificador compartido para la pastilla de energía adicional.
    public string? BonusId { get; set; }
    public int? BonusDuration { get; set; }

    // Segundos restantes de los cinco guardianes temporales (CA-22.2).
    public int? GuardianSurgeDuration { get; set; }

    // Tiempo restante de sobrecarga del reactor (CA-22.3).
    public int? OverloadDuration { get; set; }

    // Clasificación final de todos los participantes.
    public List<ResultadoJugador>? Resultados { get; set; }
    // Posiciones actuales de los guardianes.
    public List<PosicionGuardianServidor>? Guardians { get; set; }
}

public class ResultadoJugador
{
    public int Posicion { get; set; }

    public int IdJugadorPartida { get; set; }

    public int IdUsuario { get; set; }

    public int Puntaje { get; set; }

    public bool EsGanador { get; set; }
}

public class PosicionGuardianServidor
{
    public int Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

