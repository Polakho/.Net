using Godot;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using MessagePack;

public partial class GameServerClient : Node
{
	[Export] public string ServerIp = "127.0.0.1";
	[Export] public int ServerPort = 5000;

	private TcpClient _client;
	private NetworkStream _stream;

	// État dernier connu
	public GetListGamesResponse LastGameList { get; private set; }
	public string CurrentGameId { get; private set; }
	public string LastJoinResult { get; private set; }

	// Events UI (toujours invoqués depuis le thread principal)
	public event Action<GetListGamesResponse> GameListReceived;
	public event Action<string> GameCreated;
	public event Action<string> JoinResultReceived;
	public event Action<GetGameStateResponse> GameStateReceived;
	public event Action<string> WrongMoveReceived;

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

		// Démarrage de la boucle de lecture en tâche de fond
		_ = Task.Run(ReadLoopAsync);

		await SendSetPlayerName("GodotClient");
		await SendGetGameList();
	}

	private async Task ReadLoopAsync()
	{
		if (_stream == null)
		{
			GD.PrintErr("[NET] Stream is null, cannot read.");
			return;
		}

		var buffer = new byte[4096];

		while (_client != null && _client.Connected)
		{
			int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
			if (bytesRead <= 0)
				break;

			var message = MessagePackSerializer.Deserialize<MessageGeneric>(
				new ReadOnlyMemory<byte>(buffer, 0, bytesRead)
			);

			HandleMessage(message);
		}

		GD.Print("[NET] Disconnected from server (ReadLoop ended)");
	}
	
	private void HandleMessage(MessageGeneric message)
	{
		GD.Print($"[NET] Received type = {message.Type}");

		switch (message.Type)
		{
			case MessageType.SetPlayerName:
			{
				var data = MessagePackSerializer.Deserialize<SetPlayerNameRequest>(message.Data);
				GD.Print($"[NET] Server name is: {data.Name}");
				break;
			}
			case MessageType.CreateGame:
			{
				var dict = MessagePackSerializer.Deserialize<Dictionary<string, string>>(message.Data);
				if (dict.TryGetValue("GameId", out var gameId))
				{
					GD.Print($"[NET] Game created with id: {gameId}");
					CurrentGameId = gameId;
					CallDeferred(nameof(EmitGameCreated));  // ← UI plus tard
				}
				break;
			}
			case MessageType.JoinGame:
			{
				var dict = MessagePackSerializer.Deserialize<Dictionary<string, string>>(message.Data);
				if (dict.TryGetValue("Result", out var result))
				{
					GD.Print($"[NET] Join result: {result}");
					LastJoinResult = result;
					CallDeferred(nameof(EmitJoinResult));
				}
				break;
			}
			case MessageType.GetGameList:
			{
				var list = MessagePackSerializer.Deserialize<GetListGamesResponse>(message.Data);
				LastGameList = list;
				GD.Print($"===== [NET.GetGameList] MESSAGE REÇU =====");
				GD.Print($"[NET] LastGameList assigné, list est null? {list == null}");
				if (list != null)
				{
					GD.Print($"[NET] list.Games est null? {list.Games == null}");
					if (list.Games != null)
					{
						GD.Print($"[NET] Nombre de games: {list.Games.Count}");
						foreach (var g in list.Games)
						{
							GD.Print($"[NET]   - Game ID: {g.Id}, Name: {g.Name}");
							GD.Print($"[NET]     Players count: {g.Players?.Count ?? 0}");
							if (g.Players != null)
							{
								foreach (var p in g.Players)
								{
									GD.Print($"[NET]       - Player: {p.Name} ({p.Id})");
								}
							}
							GD.Print($"[NET]     Spectators count: {g.Spectators?.Count ?? 0}");
							if (g.Spectators != null)
							{
								foreach (var s in g.Spectators)
								{
									GD.Print($"[NET]       - Spectator: {s.Name} ({s.Id})");
								}
							}
						}
					}
				}
				GD.Print($"[NET] Appel de CallDeferred(EmitGameListReceived)");
				CallDeferred(nameof(EmitGameListReceived));  // ← UI plus tard
				break;
			}
			case MessageType.GameState:
			{
				var state = MessagePackSerializer.Deserialize<GetGameStateResponse>(message.Data);
				GD.Print($"[NET] GameState for {state.GameId}, size={state.BoardSize}, current={state.currentPlayer}");
				CurrentGameId = state.GameId;
				_lastGameState = state;       // champ à ajouter si tu veux
				CallDeferred(nameof(EmitGameStateReceived));
				break;
			}
			case MessageType.WrongMove:
			{
				var wrong = MessagePackSerializer.Deserialize<WrongMoveResponse>(message.Data);
				GD.Print($"[NET] Wrong move: {wrong.Reason}");
				_lastWrongReason = wrong.Reason;   // champ string à ajouter
				CallDeferred(nameof(EmitWrongMove));
				break;
			}
		}
	}

	// --------------------
	// Envois "haut niveau"
	// --------------------
	public async Task JoinGame(string gameId, bool asSpectator)
	{
		var request = new JoinGameRequest
		{
			GameId = gameId,
			AsSpectator = asSpectator
		};
		await Send(MessageType.JoinGame, request);
	}

	public async Task SendSetPlayerName(string name)
	{
		var payload = new SetPlayerNameRequest { Name = name };
		await Send(MessageType.SetPlayerName, payload);
	}

	public async Task SendCreateGame(int boardSize, string gameName)
	{
		var payload = new CreateGameRequest { BoardSize = boardSize, GameName = gameName };
		await Send(MessageType.CreateGame, payload);
	}

	public async Task SendJoinGame(string gameId, bool asSpectator)
	{
		var payload = new JoinGameRequest { GameId = gameId, AsSpectator = asSpectator };
		await Send(MessageType.JoinGame, payload);
	}

	public async Task SendGetGameList()
	{
		// le serveur s’en fiche des données, on envoie juste une string
		await Send(MessageType.GetGameList, "hello");
	}

	public async Task SendGetGameState(string gameId)
	{
		var payload = new GetGameStateRequest { GameId = gameId };
		await Send(MessageType.GetGameState, payload);
	}

	public async Task SendMakeMove(string gameId, int x, int y, bool isPass)
	{
		var payload = new MakeMoveRequest { GameId = gameId, X = x, Y = y, IsPass = isPass };
		await Send(MessageType.MakeMove, payload);
	}

	// --------------------
	// Envoi générique
	// --------------------

	private async Task Send<T>(string type, T payload)
	{
		if (_stream == null)
		{
			GD.PrintErr("[NET] Cannot send, stream is null");
			return;
		}

		var msg = new MessageGeneric
		{
			Type = type,
			Data = MessagePackSerializer.Serialize(payload)
		};

		byte[] data = MessagePackSerializer.Serialize(msg);
		await _stream.WriteAsync(data, 0, data.Length);

		GD.Print($"[NET] Sent {type}");
	}
	
	private GetGameStateResponse _lastGameState;
	private string _lastWrongReason;

	private void EmitGameListReceived()
	{
		GD.Print("===== [NET.EmitGameListReceived] APPELÉE =====");
		GD.Print($"[NET] LastGameList est null? {LastGameList == null}");
		GD.Print($"[NET] Nombre de souscripteurs à GameListReceived: {GameListReceived?.GetInvocationList().Length ?? 0}");
		if (LastGameList != null && LastGameList.Games != null)
		{
			GD.Print($"[NET] LastGameList contient {LastGameList.Games.Count} games");
		}
		GD.Print($"[NET] Invocation de l'événement GameListReceived");
		GameListReceived?.Invoke(LastGameList);
		GD.Print($"[NET] Événement GameListReceived invoqué");
	}

	private void EmitGameCreated()
	{
		GameCreated?.Invoke(CurrentGameId);
	}

	private void EmitJoinResult()
	{
		JoinResultReceived?.Invoke(LastJoinResult);
	}

	private void EmitGameStateReceived()
	{
		if (_lastGameState != null)
			GameStateReceived?.Invoke(_lastGameState);
	}

	private void EmitWrongMove()
	{
		if (!string.IsNullOrEmpty(_lastWrongReason))
			WrongMoveReceived?.Invoke(_lastWrongReason);
	}
}
