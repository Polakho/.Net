using System.Buffers;
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
                case MessageType.CreateGame:
                    var request = MessagePackSerializer.Deserialize<CreateGameRequest>(message.Data);
                    Console.WriteLine($"{server}Received data: {request.BoardSize}");
                    var gameId = await _gameService.CreateGameAsync(request.BoardSize);
                    var responseData = MessagePackSerializer.Serialize(new { GameId = gameId });
                    return new MessageGeneric { Type = "GameCreated", Data = responseData };
                
                case MessageType.JoinGame:
                    var joinRequest = MessagePackSerializer.Deserialize<JoinGameRequest>(message.Data);
                    Console.WriteLine($"{server}Received data: {joinRequest.GameId}, AsSpectator: {joinRequest.AsSpectator}");
                    var joinResult = await _gameService.JoinGameAsync(joinRequest.GameId, player, joinRequest.AsSpectator);
                    var joinResponseData = MessagePackSerializer.Serialize(new { Result = joinResult });
                    return new MessageGeneric { Type = "GameJoined", Data = joinResponseData };
                    
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
                        return new MessageGeneric { Type = MessageType.GameState, Data = gameStateData };
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
    }
}
