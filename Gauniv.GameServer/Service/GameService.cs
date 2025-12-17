using System.Collections.Concurrent;
using Gauniv.GameServer.Model;

namespace Gauniv.GameServer.Service;

public class GameService
{
    private readonly ConcurrentDictionary<string, Game> _games = new();

    public void listGames()
    {
        foreach (var game in _games)
        {
            Console.WriteLine($"Game ID: {game.Key}, Created: {game.Value.Created}, State: {game.Value.State}");
        }
    }
    
    public Task<string> CreateGameAsync(int boardSize)
    {
        var gameId = Guid.NewGuid().ToString();
        var game = new Game(boardSize);
        _games.TryAdd(gameId, game);
        Console.WriteLine($"Created game with id {gameId}");
        listGames();
        return Task.FromResult(gameId);
    }
    
    public Task<String> JoinGameAsync(string gameId, Player player, bool asSpectator)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            if (asSpectator)
            {
                game.Spectators.Add(player);
            }
            else
            {
                if (game.State == Game.GameState.WaitingForPlayers)
                {
                    game.Players.Add(player);
                    game.UpdateGameState(); 
                }
                // TODO: could add logic to handle joining full/in-progress games
            }
            
            Console.WriteLine($"Player {player.Id} joined game {gameId} as {(asSpectator ? "spectator" : "player")}");
            listGames();
            return Task.FromResult("Joined successfully");
        }
        else
        {
            Console.WriteLine($"Game with id {gameId} not found");
            return Task.FromResult("Game not found");
        }
    }
    
    

}