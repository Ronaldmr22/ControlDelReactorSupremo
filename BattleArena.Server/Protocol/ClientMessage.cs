namespace BattleArena.Server.Protocol;

public class ClientMessage
{
    public string Type { get; set; } = string.Empty;
    public string? RoomCode { get; set; }
    public string? PlayerName { get; set; }
    public float? X { get; set; }
    public float? Y { get; set; }
}