namespace ControlReactor.Api.Multijugador.Players;

public class PlayerState
{
    public string PlayerId { get; set; } = string.Empty;

    public string PlayerName { get; set; } = string.Empty;

    public float X { get; set; }

    public float Y { get; set; }
}