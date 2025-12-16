using System.Net;
using System.Net.Sockets;
using Gauniv.GameServer.Service;
using Gauniv.GameServer.Message;
using MessagePack;

namespace Gauniv.GameServer
{
    public class Server
    {
        private TcpListener _listener;
        private bool _isRunning;
        private List<TcpClient> _clients = new List<TcpClient>();
        private int port = 5000;
        private readonly GameService _gameService;
        
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
            
            Console.WriteLine($"Server started on port {port}");

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _clients.Add(client);
                Console.WriteLine("Client connected");
                
                _ = HandleClientAsync(client);
            }
        }
        
        private async Task HandleClientAsync(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];

            try
            {
                while (client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var message = MessagePackSerializer.Deserialize<MessageGeneric>(new ReadOnlyMemory<byte>(buffer, 0, bytesRead));
                    Console.WriteLine($"Received message type: {message.Type}");

                    var response = await ProcessMessageAsync(message);
                    
                    if (response != null)
                    {
                        var responseBytes = MessagePackSerializer.Serialize(response);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                _clients.Remove(client);
                client.Close();
                Console.WriteLine("Client disconnected");
            }
        }

        private async Task<MessageGeneric?> ProcessMessageAsync(MessageGeneric message)
        {
            switch (message.Type)
            {
                case "CreateGame":
                    var request = MessagePackSerializer.Deserialize<CreateGameRequest>(message.Data);
                    Console.WriteLine($"Received data: {request.BoardSize}");
                    var gameId = await _gameService.CreateGameAsync(request.BoardSize);
                    var responseData = MessagePackSerializer.Serialize(new { GameId = gameId });
                    return new MessageGeneric { Type = "GameCreated", Data = responseData };
                
                case "JoinGame":
                    // À implémenter
                    break;
                    
                case "PlayMove":
                    // À implémenter
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
