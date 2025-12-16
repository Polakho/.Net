using System.Net.Sockets;
using Gauniv.GameServer.Message;
using MessagePack;

namespace Gauniv.GameServer;

public class GameClient
{
    private String _serverIp = "localhost";
    private int _serverPort = 5000;
    private TcpClient _client;
    private NetworkStream _stream;
    
    public GameClient()
    {
        _client = new TcpClient();
    }
    
    public async Task ConnectToServer()
    {
        try
        {
            await _client.ConnectAsync(_serverIp, _serverPort);
            _stream = _client.GetStream();
            Console.WriteLine("Connected to server");
            
            // Send a CreateGame message asa test
            var createGameRequest = new CreateGameRequest { BoardSize = 19 };
            var data = MessagePackSerializer.Serialize(createGameRequest);
            var envelope = new MessageGeneric 
            { 
                Type = MessageType.CreateGame, 
                Data = data 
            };
            var bytes = MessagePackSerializer.Serialize(envelope);
            await _stream.WriteAsync(bytes);        
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
        }
    }
    
}