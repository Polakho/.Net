using System.Collections.Concurrent;
using Gauniv.GameServer.Message;
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

    public Task<string> CreateGameAsync(string gameName, int boardSize)
    {
        var gameId = Guid.NewGuid().ToString();
        var game = new Game(gameName, boardSize);
        _games.TryAdd(gameId, game);
        Console.WriteLine($"Created game with id {gameId} and name {gameName}");
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

            Console.WriteLine($"Player {player.Id}, name {player.Name} joined game {gameId} as {(asSpectator ? "spectator" : "player")}");
            listGames();
            return Task.FromResult("Joined successfully");
        }
        else
        {
            Console.WriteLine($"Game with id {gameId} not found");
            return Task.FromResult("Game not found");
        }
    }

    public Task<GetGameStateResponse?> GetGameAsync(string gameId)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            var response = new GetGameStateResponse
            {
                GameId = gameId,
                BoardSize = game.Board.Size,
                currentPlayer = game.currentPlayer.Id.ToString(),
                Board = game.Board.Grid
            };

            return Task.FromResult<GetGameStateResponse?>(response);
        }

        return Task.FromResult<GetGameStateResponse?>(null);
    }

public Task<object> MakeMoveAsync(string gameId, Player player, int x, int y, bool isPass)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            if (game.State != Game.GameState.InProgress)
            {
                return Task.FromResult<object>(new WrongMoveResponse { Reason = "Game is not in progress" });
            }
            
            if (game.currentPlayer.Id != player.Id)
            {
                return Task.FromResult<object>(new WrongMoveResponse { Reason = "Not your turn" });
            }
    
            Point? point;
            
            if (!isPass)
            {
                point = new Point(x, y);
                if (!game.Board.InBounds(point.Value))
                {
                    return Task.FromResult<object>(new WrongMoveResponse { Reason = "Move out of bounds" });
                }
    
                if (game.Board.Get(point.Value) != null)
                {
                    return Task.FromResult<object>(new WrongMoveResponse { Reason = "Position already occupied" });
                }

                if (game.Board.KoPoint != null && game.Board.KoPoint.Value.Equals(point.Value))
                {
                    return Task.FromResult<object>(new WrongMoveResponse { Reason = "Ko rule violation" });
                }
                
                // Place the stone
                game.Board.Set(point.Value, player.Color);
                game.UpdateGameState();
                // Check score 
                Console.WriteLine($"Scores - Black: {game.Board.blackScore}, White: {game.Board.whiteScore}");
            }
            else
            {
                point = null;
            }
            
            // Record the move
            var move = new Move
            {
                PlayerId = player.Id.ToString(),
                Position = point,
                Color = player.Color,
                Timestamp = DateTime.UtcNow
            };
            game.MoveHistory.Add(move);
            
            // Switch current player
            var nextPlayer = game.Players.Find(p => p.Id != player.Id);
            game.currentPlayer = nextPlayer;
            
            Console.WriteLine($"Player {player.Id} made a move in game {gameId} at ({x}, {y}), Pass: {isPass}");
            return Task.FromResult<object>(new GetGameStateResponse
            {
                GameId = gameId,
                BoardSize = game.Board.Size,
                currentPlayer = game.currentPlayer.Id.ToString(),
                Board = game.Board.Grid
            });
            
        }
        
        return Task.FromResult<object>(new WrongMoveResponse { Reason = "Game not found" });
    }
    
}


