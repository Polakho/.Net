namespace Gauniv.GameServer.Model;

public class Board
{
    public int Size { get; set; }
    public StoneColor?[,] Grid { get; set; }
    public int blackScore { get; set; }
    public int whiteScore { get; set; }
    
    public Point? KoPoint { get; set; } = null;
    
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
        List<Point> captures = CheckForCaptures(p, color);
        if (color == StoneColor.Black)
            blackScore += captures.Count;
        else
            whiteScore += captures.Count;
        
        if (captures.Count == 1 && CountLiberties(p) == 1)
        {
            KoPoint = captures[0];
        }
    }

    public void Clear(Point p)
    {
        if (!InBounds(p)) throw new ArgumentOutOfRangeException(nameof(p));
        Grid[p.X, p.Y] = null;
    }

    public List<Point> CheckForCaptures(Point placedPoint, StoneColor placedColor)
    {
        var capturedStones = new List<Point>();
        var opponentColor = placedColor == StoneColor.Black ? StoneColor.White : StoneColor.Black;
            
        foreach (var neighbor in GetNeighbors(placedPoint))
        {
            if (Get(neighbor) == opponentColor && !HasLiberties(neighbor))
            {
                capturedStones.AddRange(CaptureGroup(neighbor));
            }
        }
    
        return capturedStones;
    }
    
    private List<Point> GetNeighbors(Point p)
    {
        var neighbors = new List<Point>
        {
            new (p.X - 1, p.Y),
            new (p.X + 1, p.Y),
            new (p.X, p.Y - 1),
            new (p.X, p.Y + 1)
        };
        return neighbors.Where(InBounds).ToList();
    }
    
    private bool HasLiberties(Point p)
    {
        var color = Get(p);
        if (!color.HasValue) return false;
    
        var visited = new HashSet<Point>();
        var toVisit = new Queue<Point>();
        toVisit.Enqueue(p);
    
        while (toVisit.Count > 0)
        {
            var current = toVisit.Dequeue();
            if (!visited.Add(current)) continue;
    
            foreach (var neighbor in GetNeighbors(current))
            {
                var neighborColor = Get(neighbor);
                if (!neighborColor.HasValue) return true; // Liberté trouvée
                if (neighborColor == color) toVisit.Enqueue(neighbor);
            }
        }
    
        return false;
    }

    private int CountLiberties(Point p)
    {
        var color = Get(p);
        if (!color.HasValue) return 0;

        var visited = new HashSet<Point>();
        var toVisit = new Queue<Point>();
        toVisit.Enqueue(p);
        var liberties = 0;

        while (toVisit.Count > 0)
        {
            var current = toVisit.Dequeue();
            if (!visited.Add(current)) continue;

            foreach (var neighbor in GetNeighbors(current))
            {
                var neighborColor = Get(neighbor);
                if (!neighborColor.HasValue && visited.Add(neighbor))
                    liberties++;
                else if (neighborColor == color)
                    toVisit.Enqueue(neighbor);
            }
        }

        return liberties;
    }

    private List<Point> CaptureGroup(Point p)
    {
        var color = Get(p);
        var captured = new List<Point>();
        var visited = new HashSet<Point>();
        var toVisit = new Queue<Point>();
        toVisit.Enqueue(p);
    
        while (toVisit.Count > 0)
        {
            var current = toVisit.Dequeue();
            if (!visited.Add(current)) continue;
    
            captured.Add(current);
            Clear(current);
    
            foreach (var neighbor in GetNeighbors(current))
            {
                if (Get(neighbor) == color) toVisit.Enqueue(neighbor);
            }
        }
    
        return captured;
    }
    
    
}