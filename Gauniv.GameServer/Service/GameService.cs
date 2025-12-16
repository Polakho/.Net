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
    
    

}