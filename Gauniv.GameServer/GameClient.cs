using System.Net.Sockets;
using Gauniv.GameServer.Message;
using Gauniv.GameServer.Model;
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
            _ = HandleCLientAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{clientTag}Error connecting to server: {ex.Message}");
        }
    }

    private async Task HandleCLientAsync()
    {
        while (_client.Connected)
        {
            MessagePackStreamReader reader = new MessagePackStreamReader(_stream);
            var cancelToken = new CancellationTokenSource(TimeSpan.FromMinutes(30)).Token;
            var binaryData = await reader.ReadAsync(cancelToken);
            var message = MessagePackSerializer.Deserialize<MessageGeneric>(binaryData.Value, null, cancelToken);
                    
            Console.WriteLine($"{clientTag}Received message type: {message.Type}");
            Console.WriteLine($"{clientTag}Received message Data: {message.Data}");
            if (MessageType.GameState == message.Type)
            {
                var gameState = MessagePackSerializer.Deserialize<GetGameStateResponse>(message.Data);
                Console.WriteLine($"{clientTag}Game ID: {gameState.GameId}, Board Size: {gameState.BoardSize}, Current Player: {gameState.currentPlayer}");
                // Print the board
                for (int i = 0; i < gameState.BoardSize; i++)
                {
                    for (int j = 0; j < gameState.BoardSize; j++)
                    {
                        var stone = gameState.Board[i, j];
                        char symbol = stone switch
                        {
                            StoneColor.Black => 'B',
                            StoneColor.White => 'W',
                            _ => '.'
                        };
                        Console.Write(symbol + " ");
                    }
                    Console.WriteLine();
                }
            }

            if (MessageType.WrongMove == message.Type)
            {
                var moveResult = MessagePackSerializer.Deserialize<WrongMoveResponse>(message.Data);
                Console.WriteLine($"{clientTag}Move Result: {moveResult}");
            }
        }
    }
    

    public async Task SendMessageAsync(MessageGeneric message)
    {
        try
        {
            byte[] data = MessagePackSerializer.Serialize(message);
            await _stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"{clientTag}Message sent to server");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{clientTag}Error sending message: {ex.Message}");
        }
    }
    
}