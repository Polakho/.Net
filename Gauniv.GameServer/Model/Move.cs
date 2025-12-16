namespace Gauniv.GameServer.Model;

public class Move
{
    public string PlayerId { get; set; }
    public StoneColor Color { get; set; }
    public Point? Position { get; set; } // null for pass
    public bool IsPass => Position == null;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<Point> Captured { get; set; } = new();

    public Move() { }

    public Move(string playerId, StoneColor color, Point? position)
    {
        PlayerId = playerId;
        Color = color;
        Position = position;
    }
}