namespace Gauniv.GameServer.Model;

public class Board
{
    public int Size { get; set; }
    public StoneColor?[,] Grid { get; set; }
    
    public Board(int size)
    {
        Size = size;
        Grid = new StoneColor?[size, size];
    }
    
    public bool InBounds(Point p) => p.X >= 0 && p.X < Size && p.Y >= 0 && p.Y < Size;

    public StoneColor? Get(Point p) => InBounds(p) ? Grid[p.X, p.Y] : null;

    public void Set(Point p, StoneColor color)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        Grid[p.X, p.Y] = color;
    }

    public void Clear(Point p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        Grid[p.X, p.Y] = null;
    }
    
}