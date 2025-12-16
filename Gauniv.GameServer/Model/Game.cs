using System;

namespace Gauniv.GameServer.Model;

public class Game
{
    public string Id { get; set; }
    public DateTime Created { get; set; }
    public Board Board { get; set; }
    public List<Move> MoveHistory { get; set; }
    public List<Player> Players { get; set; }
    public List<Player> Spectators { get; set; }
    public Player Winner { get; set; }
    public GameState State { get; set; }
    
    public Game(int boardSize)
    {
        Id = Guid.NewGuid().ToString();
        Created = DateTime.UtcNow;
        Board = new Board(boardSize);
        MoveHistory = new List<Move>();
        Players = new List<Player>();
        Spectators = new List<Player>();
        State = GameState.WaitingForPlayers;
    }
}

public enum GameState
{
    WaitingForPlayers,
    InProgress,
    Finished
}