﻿using System.Collections.Concurrent;
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
    public void listGames()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === LISTE DES PARTIES ACTIVES ({_games.Count}) ===");
        foreach (var game in _games)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]   - ID: {game.Key}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]     Nom: {game.Value.Name}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]     État: {game.Value.State}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]     Joueurs: {game.Value.Players.Count}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]     Spectateurs: {game.Value.Spectators.Count}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]     Créée: {game.Value.Created:yyyy-MM-dd HH:mm:ss}");
        }
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] =====================================");
    }

    public Task<string> CreateGameAsync(string gameName, int boardSize)
    {
        var game = new Game(gameName, boardSize);
        _games.TryAdd(game.Id, game);
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✓ Partie créée - ID: {game.Id}, Nom: {gameName}, Taille: {boardSize}x{boardSize}");
        listGames();
        return Task.FromResult(game.Id);
    }

    public Task<String> JoinGameAsync(string gameId, Player player, bool asSpectator)
    {
        if (_games.TryGetValue(gameId, out var game))
        {
            if (asSpectator)
            {
                game.Spectators.Add(player);
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✓ {player.Name} ({player.Id}) a rejoint comme SPECTATEUR la partie {gameId}");
            }
            else
            {
                if (game.State == Game.GameState.WaitingForPlayers)
                {
                    game.Players.Add(player);
                    game.UpdateGameState();
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✓ {player.Name} ({player.Id}) a rejoint comme JOUEUR la partie {gameId}");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ {player.Name} ({player.Id}) ne peut pas rejoindre la partie {gameId} - État: {game.State}");
                }
                // TODO: could add logic to handle joining full/in-progress games
            }

            listGames();
            return Task.FromResult("Joined game successfully");
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
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ✓ {player.Name} ({player.Id}) a quitté la partie {gameId}");
                // If nobody remains, remove the game
                if (game.Players.Count == 0 && game.Spectators.Count == 0)
                {
                    _games.TryRemove(gameId, out _);
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 🗑️ Partie {gameId} supprimée (plus de joueurs ni de spectateurs)");
                }
                listGames();
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
            var response = new GetGameStateResponse
            {
                GameId = gameId,
                BoardSize = game.Board.Size,
                // currentPlayer can be null while waiting for players; avoid NRE
                currentPlayer = game.currentPlayer != null ? game.currentPlayer.Id.ToString() : string.Empty,
                Board = game.Board.Grid,
                GameState = game.State.ToString(),
                PlayerCount = game.Players.Count
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
            game.UpdateGameState();

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

    public async Task<List<Game>> ListGamesAsync()
    {
        return  await Task.FromResult(_games.Values.ToList());
    }
}


