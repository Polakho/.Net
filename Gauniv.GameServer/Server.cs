using System.Net;
using System.Net.Sockets;
using Gauniv.GameServer.Service;
using Gauniv.GameServer.Message;
using Gauniv.GameServer.Model;
using MessagePack;

namespace Gauniv.GameServer
{
    public class Server
    {
        private TcpListener _listener;
        private bool _isRunning;
        private Dictionary<string, Player> _players = new Dictionary<string, Player>();
        private int port = 5000;
        private readonly GameService _gameService;
        private String server = "SERVER-";
        private String name = "SERVER MASTER OF GAMES";
        
        public Server(int port)
        {
            this.port = port;
            _gameService = new GameService();
        }
        
        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            
            Console.WriteLine($"========================================");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Serveur démarré sur le port {port}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}En attente de connexions clients...");
            Console.WriteLine($"========================================");

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }
        
        private async Task HandleClientAsync(TcpClient client)
        {
            var player = new Player(client);
            _players[player.Id.ToString()] = player;
            var stream = client.GetStream();
            MessagePackStreamReader reader = new MessagePackStreamReader(stream);
            Console.WriteLine($"========================================");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}NOUVEAU CLIENT CONNECTÉ");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}  - ID: {player.Id}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}  - Total clients connectés: {_players.Count}");
            Console.WriteLine($"========================================");
            try
            {
                while (client.Connected)
                {
                    var cancelToken = new CancellationTokenSource(TimeSpan.FromMinutes(30)).Token;
                    var binaryData = await reader.ReadAsync(cancelToken);
                    var message = MessagePackSerializer.Deserialize<MessageGeneric>(binaryData.Value, null, cancelToken);
                    
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Message reçu - Type: {message.Type} de {player.Id}");

                    var response = await ProcessMessageAsync(message, player);
                    
                    if (response != null)
                    {
                        var responseBytes = MessagePackSerializer.Serialize(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                } 
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}ERREUR: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}StackTrace: {ex.StackTrace}");
            }
            finally
            {
                _players.Remove(player.Id.ToString());
                client.Close();
                Console.WriteLine($"========================================");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}CLIENT DÉCONNECTÉ");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}  - ID: {player.Id}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}  - Clients restants: {_players.Count}");
                Console.WriteLine($"========================================");
            }
        }

        private async Task<MessageGeneric?> ProcessMessageAsync(MessageGeneric message, Player player)
        {
            switch (message.Type)
            {
                case MessageType.SetPlayerName:
                    var nameRequest = MessagePackSerializer.Deserialize<SetPlayerNameRequest>(message.Data);
                    Console.WriteLine($"{server}Received data: {nameRequest.Name} for player {player.Id}");
                    player.Name = nameRequest.Name;
                    // Send name of server back to client
                    var nameResponseData = MessagePack.MessagePackSerializer.Serialize(new Message.SetPlayerNameRequest { Name = name });
                    return new MessageGeneric { Type = MessageType.SetPlayerName, Data = nameResponseData };
                
                case MessageType.GetGameList:
                    var games = await _gameService.ListGamesAsync();
                    var gameInfos = games.Select(g => new GameInfo
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Players = g.Players.Select(p => new PlayerInfo { Id = p.Id.ToString(), Name = p.Name }).ToList(),
                        Spectators = g.Spectators.Select(s => new PlayerInfo { Id = s.Id.ToString(), Name = s.Name }).ToList(),
                        State = g.State,
                        BoardSize = g.Board.Size
                    }).ToList();
                    
                    // Log détaillé des games
                    Console.WriteLine($"{server}[GetGameList] Nombre total de games: {gameInfos.Count}");
                    foreach (var gameInfo in gameInfos)
                    {
                        Console.WriteLine($"{server}[GetGameList] Game ID: {gameInfo.Id}, Name: {gameInfo.Name}");
                        Console.WriteLine($"{server}[GetGameList]   - Nombre de players: {gameInfo.Players.Count}");
                        foreach (var plr in gameInfo.Players)
                        {
                            Console.WriteLine($"{server}[GetGameList]     - Player: {plr.Name} ({plr.Id})");
                        }
                        Console.WriteLine($"{server}[GetGameList]   - Nombre de spectators: {gameInfo.Spectators.Count}");
                        foreach (var spectator in gameInfo.Spectators)
                        {
                            Console.WriteLine($"{server}[GetGameList]     - Spectator: {spectator.Name} ({spectator.Id})");
                        }
                    }
                    
                    var listResponse = new GetListGamesResponse { Games = gameInfos };
                    var listResponseData = MessagePackSerializer.Serialize(listResponse);
                    return new MessageGeneric { Type = MessageType.GetGameList, Data = listResponseData };
                
                case MessageType.CreateGame:
                    var request = MessagePackSerializer.Deserialize<CreateGameRequest>(message.Data);
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Création d'une partie - Taille: {request.BoardSize}, Nom: {request.GameName}");
                    var gameId = await _gameService.CreateGameAsync(request.GameName, request.BoardSize);
                    
                    // Automatically join the game as the creator
                    await _gameService.JoinGameAsync(gameId, player, false);
                    
                    // Broadcast updated list to everyone
                    await BroadcastGameListAsync();
                    
                    var responseData = MessagePackSerializer.Serialize(new JoinGameResponse { GameId = gameId, Result = "Joined game successfully", YourPlayerId = player.Id.ToString() });
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Partie créée avec succès - ID: {gameId}");
                    return new MessageGeneric { Type = MessageType.CreateGame, Data = responseData };
                
                case MessageType.JoinGame:
                    var joinRequest = MessagePackSerializer.Deserialize<JoinGameRequest>(message.Data);
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Joueur {player.Name} ({player.Id}) rejoint la partie {joinRequest.GameId} - Spectateur: {joinRequest.AsSpectator}");
                    var joinResult = await _gameService.JoinGameAsync(joinRequest.GameId, player, joinRequest.AsSpectator);
                    
                    // Broadcast updated list to everyone
                    await BroadcastGameListAsync();
                    
                    var joinResponseData = MessagePackSerializer.Serialize(new JoinGameResponse { Result = joinResult, GameId = joinRequest.GameId, YourPlayerId = player.Id.ToString() });
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Résultat: {joinResult}");
                    return new MessageGeneric { Type = MessageType.JoinGame, Data = joinResponseData };

                case MessageType.LeaveGame:
                    var leaveRequest = MessagePackSerializer.Deserialize<LeaveGameRequest>(message.Data);
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Joueur {player.Name} ({player.Id}) quitte la partie {leaveRequest.GameId}");
                    var leaveResult = await _gameService.LeaveGameAsync(leaveRequest.GameId, player);

                    // Broadcast updated list to everyone
                    await BroadcastGameListAsync();

                    var leaveResponseData = MessagePackSerializer.Serialize(new LeaveGameResponse { Result = leaveResult, GameId = leaveRequest.GameId});
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Résultat: {leaveResult}");
                    return new MessageGeneric { Type = MessageType.LeaveGame, Data = leaveResponseData };
                
                case MessageType.GetGameState:
                    var stateRequest = MessagePackSerializer.Deserialize<GetGameStateRequest>(message.Data);
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Demande d'état de la partie {stateRequest.GameId}");
                    var gameStateResponse = await _gameService.GetGameAsync(stateRequest.GameId);
                    var stateResponseData = MessagePackSerializer.Serialize(gameStateResponse);
                    return new MessageGeneric { Type = MessageType.GameState, Data = stateResponseData };
                    
                case MessageType.MakeMove:
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Traitement d'un coup du joueur {player.Id} ({player.Name})...");
                    var moveRequest = MessagePackSerializer.Deserialize<MakeMoveRequest>(message.Data);
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Détails - GameId: {moveRequest.GameId}, X: {moveRequest.X}, Y: {moveRequest.Y}, Pass: {moveRequest.IsPass}");
                    var moveResult = await _gameService.MakeMoveAsync(moveRequest.GameId, player, moveRequest.X, moveRequest.Y, moveRequest.IsPass);
                    if (moveResult is WrongMoveResponse wrongMove)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}❌ Coup refusé: {wrongMove.Reason}");
                        var wrongMoveData = MessagePackSerializer.Serialize(wrongMove);
                        return new MessageGeneric { Type = MessageType.WrongMove, Data = wrongMoveData };
                    }
                    else if (moveResult is GetGameStateResponse gameState)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}✓ Coup accepté! Prochain joueur: {gameState.currentPlayer}");
                        var gameStateData = MessagePackSerializer.Serialize(gameState);
                        var broadcastMessage = new MessageGeneric { Type = MessageType.GameState, Data = gameStateData };
                        byte[] data = MessagePackSerializer.Serialize(broadcastMessage);
                        // Récupérer la partie
                        Game game =  _gameService.GetGameById(moveRequest.GameId);
        
                        // Envoyer à tous les joueurs et spectateurs
                        var recipients = game.Players.Concat(game.Spectators);
                        
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Diffusion du nouvel état à {recipients.Count()} destinataires");
                        foreach (var recipient in recipients)
                        {
                            if (_players.TryGetValue(recipient.Id.ToString(), out var recipientPlayer))
                            {
                                try
                                {
                                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}  → Envoi à {recipientPlayer.Name} ({recipient.Id})");
                                    var recipientStream = recipientPlayer.Stream;
                                    await recipientStream.WriteAsync(data, 0, data.Length);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}❌ Échec d'envoi à {recipient.Id}: {ex.Message}");
                                }
                            }
                        }
        
                        return null;
                    }
                    break;
                
            }
            return null;
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }

        private async Task BroadcastGameListAsync()
        {
            var games = await _gameService.ListGamesAsync();
            var gameInfos = games.Select(g => new GameInfo
            {
                Id = g.Id,
                Name = g.Name,
                Players = g.Players.Select(p => new PlayerInfo { Id = p.Id.ToString(), Name = p.Name }).ToList(),
                Spectators = g.Spectators.Select(s => new PlayerInfo { Id = s.Id.ToString(), Name = s.Name }).ToList(),
                State = g.State,
                BoardSize = g.Board.Size
            }).ToList();

            var listResponse = new GetListGamesResponse { Games = gameInfos };
            var listResponseData = MessagePackSerializer.Serialize(listResponse);
            var broadcastMessage = new MessageGeneric { Type = MessageType.GetGameList, Data = listResponseData };
            byte[] data = MessagePackSerializer.Serialize(broadcastMessage);

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}Diffusion de la liste des parties à {_players.Count} joueurs - {gameInfos.Count} parties actives");

            foreach (var p in _players.Values)
            {
                try
                {
                    await p.Stream.WriteAsync(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {server}❌ Échec d'envoi de la liste à {p.Id}: {ex.Message}");
                }
            }
        }
    }
}
