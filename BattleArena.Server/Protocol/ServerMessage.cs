using BattleArena.Server.Players;

namespace BattleArena.Server.Protocol;

public class ServerMessage
{
    public string Type { get; set; } = string.Empty;

    public string? Message { get; set; }

    public string? RoomCode { get; set; }

    public string? PlayerId { get; set; }

    public string? PlayerName { get; set; }

    public float X { get; set; }

    public float Y { get; set; }
}