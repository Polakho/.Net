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
    public Player currentPlayer { get; set; }
    public Player Winner { get; set; }
    public GameState State { get; set; }
    
    

    public Game(int boardSize)
    {
        Id = Guid.NewGuid().ToString();
        Created = DateTime.UtcNow;
        Board = new Board(boardSize);
        MoveHistory = new List<Move>();
        Players = new List<Player>(2);
        Spectators = new List<Player>();
        State = GameState.WaitingForPlayers;
    }

    public void UpdateGameState()
    {
        if (Players.Count == 2 && State == GameState.WaitingForPlayers)
        {
            State = GameState.InProgress;
            // get random player to start
            var rand = new Random();
            Player black = Players[rand.Next(0, 2)];
            black.Color = StoneColor.Black;
            Player white = Players.Find(p => p != black);
            white.Color = StoneColor.White;
            currentPlayer = black;
            Console.WriteLine($"Game {Id} started with players {Players[0].Id} and {Players[1].Id}");
        }
    }

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        Finished
    }
}