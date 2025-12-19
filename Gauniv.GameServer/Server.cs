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
            
            Console.WriteLine($"{server}Server started on port {port}");

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
            Console.WriteLine($"{server}Client connected with ID: {player.Id}");
            try
            {
                while (client.Connected)
                {
                    var cancelToken = new CancellationTokenSource(TimeSpan.FromMinutes(30)).Token;
                    var binaryData = await reader.ReadAsync(cancelToken);
                    var message = MessagePackSerializer.Deserialize<MessageGeneric>(binaryData.Value, null, cancelToken);
                    
                    Console.WriteLine($"{server}Received message type: {message.Type}");

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
                Console.WriteLine($"{server}Error: {ex.Message}");
            }
            finally
            {
                _players.Remove(player.Id.ToString());
                client.Close();
                Console.WriteLine($"{server}Client disconnected");
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
                    Console.WriteLine($"{server}Received data: {request.BoardSize}");
                    var gameId = await _gameService.CreateGameAsync(request.GameName, request.BoardSize);
                    
                    // Automatically join the game as the creator
                    await _gameService.JoinGameAsync(gameId, player, false);
                    
                    // Broadcast updated list to everyone
                    await BroadcastGameListAsync();
                    
                    var responseData = MessagePackSerializer.Serialize(new JoinGameResponse { GameId = gameId, Result = "Joined game successfully" });
                    return new MessageGeneric { Type = MessageType.CreateGame, Data = responseData };
                
                case MessageType.JoinGame:
                    var joinRequest = MessagePackSerializer.Deserialize<JoinGameRequest>(message.Data);
                    Console.WriteLine($"{server}Received data: {joinRequest.GameId}, AsSpectator: {joinRequest.AsSpectator}");
                    var joinResult = await _gameService.JoinGameAsync(joinRequest.GameId, player, joinRequest.AsSpectator);
                    
                    // Broadcast updated list to everyone
                    await BroadcastGameListAsync();
                    
                    var joinResponseData = MessagePackSerializer.Serialize(new JoinGameResponse { Result = joinResult, GameId = joinRequest.GameId});
                    return new MessageGeneric { Type = MessageType.JoinGame, Data = joinResponseData };

                case MessageType.LeaveGame:
                    var leaveRequest = MessagePackSerializer.Deserialize<LeaveGameRequest>(message.Data);
                    Console.WriteLine($"{server}Received data: {leaveRequest.GameId}");
                    var leaveResult = await _gameService.LeaveGameAsync(leaveRequest.GameId, player);

                    // Broadcast updated list to everyone
                    await BroadcastGameListAsync();

                    var leaveResponseData = MessagePackSerializer.Serialize(new LeaveGameResponse { Result = leaveResult, GameId = leaveRequest.GameId});
                    return new MessageGeneric { Type = MessageType.LeaveGame, Data = leaveResponseData };
                
                case MessageType.GetGameState:
                    var stateRequest = MessagePackSerializer.Deserialize<GetGameStateRequest>(message.Data);
                    Console.WriteLine($"{server}Received data: {stateRequest.GameId}");
                    var gameStateResponse = await _gameService.GetGameAsync(stateRequest.GameId);
                    var stateResponseData = MessagePackSerializer.Serialize(gameStateResponse);
                    return new MessageGeneric { Type = MessageType.GameState, Data = stateResponseData };
                    
                case MessageType.MakeMove:
                    Console.Write($"{server}Processing MakeMove request from player {player.Id}...");
                    var moveRequest = MessagePackSerializer.Deserialize<MakeMoveRequest>(message.Data);
                    var moveResult = await _gameService.MakeMoveAsync(moveRequest.GameId, player, moveRequest.X, moveRequest.Y, moveRequest.IsPass);
                    if (moveResult is WrongMoveResponse wrongMove)
                    {
                        var wrongMoveData = MessagePackSerializer.Serialize(wrongMove);
                        return new MessageGeneric { Type = MessageType.WrongMove, Data = wrongMoveData };
                    }
                    else if (moveResult is GetGameStateResponse gameState)
                    {
                        var gameStateData = MessagePackSerializer.Serialize(gameState);
                        var broadcastMessage = new MessageGeneric { Type = MessageType.GameState, Data = gameStateData };
                        byte[] data = MessagePackSerializer.Serialize(broadcastMessage);
                        // Récupérer la partie
                        Game game =  _gameService.GetGameById(moveRequest.GameId);
        
                        // Envoyer à tous les joueurs et spectateurs
                        var recipients = game.Players.Concat(game.Spectators);
                        
                        foreach (var recipient in recipients)
                        {
                            if (_players.TryGetValue(recipient.Id.ToString(), out var recipientPlayer))
                            {
                                try
                                {
                                    Console.WriteLine($"{server}Sending to player {recipient.Id}: {recipientPlayer.Name}");
                                    var recipientStream = recipientPlayer.Stream;
                                    await recipientStream.WriteAsync(data, 0, data.Length);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"{server}Failed to send update to player {recipient.Id}: {ex.Message}");
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

            Console.WriteLine($"{server}Broadcasting updated game list to {_players.Count} players");

            foreach (var p in _players.Values)
            {
                try
                {
                    await p.Stream.WriteAsync(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{server}Failed to send game list update to player {p.Id}: {ex.Message}");
                }
            }
        }
    }
}
