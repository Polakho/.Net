using System;

namespace Gauniv.GameServer.Model;

public class Game
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Created { get; set; }
    public Board Board { get; set; }
    public List<Move> MoveHistory { get; set; }
    public List<Player> Players { get; set; }
    public List<Player> Spectators { get; set; }
    public Player currentPlayer { get; set; }
    public GameState State { get; set; }
    public Player Winner { get; set; }
    
    public Game(string name, int boardSize)
    {
        Name = name;
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

        if (State == GameState.InProgress && MoveHistory.Count >= 2)
        {
            // check for two consecutive passes
            var lastMove = MoveHistory[MoveHistory.Count - 1];
            var secondLastMove = MoveHistory[MoveHistory.Count - 2];
            if (lastMove.IsPass && secondLastMove.IsPass)
            {
                State = GameState.Finished;
                Console.WriteLine($"Game {Id} finished");
                //Check for winner (simplified, real scoring is more complex)
                int blackStones = 0;
                int whiteStones = 0;
                for (int x = 0; x < Board.Size; x++)
                {
                    for (int y = 0; y < Board.Size; y++)
                    {
                        var stone = Board.Grid[x, y];
                        if (stone == StoneColor.Black) blackStones++;
                        else if (stone == StoneColor.White) whiteStones++;
                    }
                }
                blackStones += Board.blackScore;
                whiteStones += Board.whiteScore;
                if (blackStones > whiteStones)
                {
                    Winner = Players.Find(p => p.Color == StoneColor.Black);
                }
                else if (whiteStones > blackStones)
                {
                    Winner = Players.Find(p => p.Color == StoneColor.White);
                }
                else
                {
                    Winner = null; // Tie
                }
                
                Console.WriteLine(Winner != null
                    ? $"Winner is Player {Winner.Id} with color {Winner.Color}"
                    : "The game ended in a tie");
            }
        }
    }

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        Finished
    }
}