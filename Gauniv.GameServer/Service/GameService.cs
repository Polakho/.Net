using System.Collections.Concurrent;
using Gauniv.GameServer.Message;
using Gauniv.GameServer.Model;

namespace Gauniv.GameServer.Service;

public class GameService
{
    private readonly ConcurrentDictionary<string, Game> _games = new();

    public Game GetGameById(string gameId)
    {
        _games.TryGetValue(gameId, out var game);
        return game;
    }

    public Task<string> CreateGameAsync(string gameName, int boardSize)
    {
        var game = new Game(gameName, boardSize);
        _games.TryAdd(game.Id, game);
        return Task.FromResult(game.Id);
    }

    public Task<String> JoinGameAsync(string gameId, Player player, bool asSpectator)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            if (asSpectator)
            {
                game.Spectators.Add(player);
                return Task.FromResult("Joined game successfully");
            }
            else
            {
                if (game.Players.Count >= 2)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ {player.Name} ne peut pas rejoindre la partie {gameId} - Partie complète");
                    return Task.FromResult("Game is full");
                }
                
                if (game.State == Game.GameState.WaitingForPlayers)
                {
                    game.Players.Add(player);
                    game.UpdateGameState();
                    return Task.FromResult("Joined game successfully");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ {player.Name} ne peut pas rejoindre la partie {gameId} - État: {game.State}");
                    return Task.FromResult("Game already started");
                }
            }
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ Partie {gameId} introuvable");
            return Task.FromResult("Game not found");
        }
    }
    
    public Task<string> LeaveGameAsync(string gameId, Player player)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            if (game.Players.RemoveAll(p => p.Id == player.Id) > 0 || 
                game.Spectators.RemoveAll(s => s.Id == player.Id) > 0)
            {
                game.UpdateGameState();
                
                if (game.Players.Count == 0 && game.Spectators.Count == 0)
                {
                    _games.TryRemove(gameId, out _);
                }
                return Task.FromResult("Left game successfully");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ Joueur {player.Id} non trouvé dans la partie {gameId}");
                return Task.FromResult("Player not found in game");
            }
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ Partie {gameId} introuvable");
            return Task.FromResult("Game not found");
        }
    }

    public Task<GetGameStateResponse?> GetGameAsync(string gameId)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            // Calculer les scores
            int blackStones = 0;
            int whiteStones = 0;
            for (int x = 0; x < game.Board.Size; x++)
            {
                for (int y = 0; y < game.Board.Size; y++)
                {
                    var stone = game.Board.Grid[x, y];
                    if (stone == StoneColor.Black) blackStones++;
                    else if (stone == StoneColor.White) whiteStones++;
                }
            }
            int blackScore = blackStones + game.Board.blackScore;
            int whiteScore = whiteStones + game.Board.whiteScore;
            
            var response = new GetGameStateResponse
            {
                GameId = gameId,
                BoardSize = game.Board.Size,
                currentPlayer = game.currentPlayer != null ? game.currentPlayer.Id.ToString() : string.Empty,
                Board = game.Board.Grid,
                GameState = game.State.ToString(),
                PlayerCount = game.Players.Count,
                SpectatorCount = game.Spectators.Count,
                WinnerId = game.Winner?.Id.ToString() ?? string.Empty,
                BlackScore = blackScore,
                WhiteScore = whiteScore
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
                
                game.Board.Set(point.Value, player.Color);
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
            game.UpdateGameState();

            // Switch current player
            var nextPlayer = game.Players.Find(p => p.Id != player.Id);
            game.currentPlayer = nextPlayer;
            
            // Calculer les scores
            int blackStones = 0;
            int whiteStones = 0;
            for (int i = 0; i < game.Board.Size; i++)
            {
                for (int j = 0; j < game.Board.Size; j++)
                {
                    var stone = game.Board.Grid[i, j];
                    if (stone == StoneColor.Black) blackStones++;
                    else if (stone == StoneColor.White) whiteStones++;
                }
            }
            int blackScore = blackStones + game.Board.blackScore;
            int whiteScore = whiteStones + game.Board.whiteScore;
            
            return Task.FromResult<object>(new GetGameStateResponse
            {
                GameId = gameId,
                BoardSize = game.Board.Size,
                currentPlayer = game.currentPlayer.Id.ToString(),
                Board = game.Board.Grid,
                GameState = game.State.ToString(),
                PlayerCount = game.Players.Count,
                SpectatorCount = game.Spectators.Count,
                WinnerId = game.Winner?.Id.ToString() ?? string.Empty,
                BlackScore = blackScore,
                WhiteScore = whiteScore
            });
            
        }
        
        return Task.FromResult<object>(new WrongMoveResponse { Reason = "Game not found" });
    }

    public async Task<List<Game>> ListGamesAsync()
    {
        return  await Task.FromResult(_games.Values.ToList());
    }
}
