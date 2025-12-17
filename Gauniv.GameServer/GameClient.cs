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
    private string clientTag = "CLIENT-";
    
    public GameClient(string name)
    {
        _client = new TcpClient();
        clientTag = "CLIENT-"+name+"-";
    }
    
    public async Task ConnectToServer()
    {
        try
        {
            await _client.ConnectAsync(_serverIp, _serverPort);
            _stream = _client.GetStream();
            Console.WriteLine($"{clientTag}Connected to server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{clientTag}Error connecting to server: {ex.Message}");
        }
    }
    
    public async Task SendMessageAsync(MessageGeneric message)
    {
        try
        {
            byte[] data = MessagePackSerializer.Serialize(message);
            await _stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"{clientTag}Message sent to server");
            // Send a CreateGame message asa test
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{clientTag}Error sending message: {ex.Message}");
        }
    }
    
}