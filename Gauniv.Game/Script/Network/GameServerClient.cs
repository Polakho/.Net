using Godot;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessagePack;

public partial class GameServerClient : Node
{
	[Export] public string ServerIp = "127.0.0.1";
	[Export] public int ServerPort = 5000;

	private TcpClient _client;
	private NetworkStream _stream;

	public override async void _Ready()
	{
		await ConnectAsync();
	}

	public async Task ConnectAsync()
	{
		_client = new TcpClient();
		await _client.ConnectAsync(ServerIp, ServerPort);
		_stream = _client.GetStream();

		GD.Print("Connected to GameServer");

		_ = Task.Run(ReadLoopAsync);
		
		await CreateGame(9);
	}

	private async Task ReadLoopAsync()
	{
		var buffer = new byte[4096];

		while (_client.Connected)
		{
			int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
			if (bytesRead <= 0) break;

			var message = MessagePackSerializer.Deserialize<MessageGeneric>(
				new ReadOnlyMemory<byte>(buffer, 0, bytesRead)
			);

			HandleMessage(message);
		}
	}

	private void HandleMessage(MessageGeneric message)
	{
		GD.Print($"Received message type: {message.Type}");

		if (message.Type == "GameCreated")
		{
			var data = MessagePackSerializer.Deserialize<dynamic>(message.Data);
			GD.Print($"Game created: {data.GameId}");
		}

		if (message.Type == "GameJoined")
		{
			var data = MessagePackSerializer.Deserialize<dynamic>(message.Data);
			GD.Print($"Join result: {data.Result}");
		}
	}

	// --------------------
	// Envois
	// --------------------

	public async Task CreateGame(int boardSize)
	{
		var request = new CreateGameRequest { BoardSize = boardSize };
		await Send("CreateGame", request);
	}

	public async Task JoinGame(string gameId, bool asSpectator)
	{
		var request = new JoinGameRequest
		{
			GameId = gameId,
			AsSpectator = asSpectator
		};
		await Send("JoinGame", request);
	}

	private async Task Send<T>(string type, T payload)
	{
		var message = new MessageGeneric
		{
			Type = type,
			Data = MessagePackSerializer.Serialize(payload)
		};

		byte[] bytes = MessagePackSerializer.Serialize(message);
		await _stream.WriteAsync(bytes, 0, bytes.Length);

		GD.Print($"Sent {type}");
	}
}
