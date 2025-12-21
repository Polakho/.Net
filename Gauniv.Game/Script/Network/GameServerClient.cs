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

	public GetListGamesResponse LastGameList { get; private set; }
	public string CurrentGameId { get; private set; }
	public string LastJoinResult { get; private set; }
	public string CurrentGameState { get; private set; }
	public int CurrentPlayerCount { get; private set; }
	public int CurrentSpectatorCount { get; private set; }

	public event Action<GetListGamesResponse> GameListReceived;
	public event Action<string> GameCreated;
	public event Action<string> JoinResultReceived;
	public event Action<GetGameStateResponse> GameStateReceived;
	public event Action<string> WrongMoveReceived;

	public string LocalPlayerId { get; private set; }
	public StoneColor? LocalPlayerColor { get; private set; }
	public bool IsLocalPlayersTurn { get; private set; }
	public bool IsJoiningAsSpectator { get; set; }
	private bool _hasInferredColor;

	public void ClearCurrentGameId()
	{
		CurrentGameId = string.Empty;
	}

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
		switch (message.Type)
		{
			case MessageType.SetPlayerName:
			{
				var data = MessagePackSerializer.Deserialize<SetPlayerNameRequest>(message.Data);
				break;
			}
			case MessageType.CreateGame:
			{
				var response = MessagePackSerializer.Deserialize<JoinGameResponse>(message.Data);
				if (response != null && !string.IsNullOrEmpty(response.GameId))
				{
					CurrentGameId = response.GameId;
					LastJoinResult = response.Result;
					LocalPlayerId = response.YourPlayerId;
					LocalPlayerColor = null;
					IsLocalPlayersTurn = false;
					_hasInferredColor = false;
					CallDeferred(nameof(EmitGameCreated));
				}
				break;
			}
			case MessageType.JoinGame:
			{
				var response = MessagePackSerializer.Deserialize<JoinGameResponse>(message.Data);
				if (response != null)
				{
					CurrentGameId = response.GameId;
					LastJoinResult = response.Result;
					LocalPlayerId = response.YourPlayerId;
					LocalPlayerColor = null;
					IsLocalPlayersTurn = false;
					_hasInferredColor = false;

					if (response.GameState != null)
					{
						_lastGameState = response.GameState;
						UpdateGameStateInfo(response.GameState);
						MaybeInferLocalColorAndTurn(response.GameState);
					}
					CallDeferred(nameof(EmitJoinResult));
				}
				break;
			}
			case MessageType.GetGameList:
			{
				var list = MessagePackSerializer.Deserialize<GetListGamesResponse>(message.Data);
				LastGameList = list;
				CallDeferred(nameof(EmitGameListReceived));
				break;
			}
			case MessageType.GameState:
			{
				var state = MessagePackSerializer.Deserialize<GetGameStateResponse>(message.Data);
				CurrentGameId = state.GameId;
				_lastGameState = state;
				UpdateGameStateInfo(state);
				MaybeInferLocalColorAndTurn(state);
				CallDeferred(nameof(EmitGameStateReceived));
				break;
			}
			case MessageType.WrongMove:
			{
				var wrong = MessagePackSerializer.Deserialize<WrongMoveResponse>(message.Data);
				_lastWrongReason = wrong.Reason;
				CallDeferred(nameof(EmitWrongMove));
				break;
			}
		}
	}

	private void MaybeInferLocalColorAndTurn(GetGameStateResponse state)
	{
		if (state == null)
			return;

		if (string.IsNullOrEmpty(LocalPlayerId))
			return;

		bool isMyTurn = state.currentPlayer == LocalPlayerId;
		IsLocalPlayersTurn = isMyTurn;

		if (_hasInferredColor)
			return;

		if (state.PlayerCount >= 2 && !string.IsNullOrEmpty(state.currentPlayer))
		{
			LocalPlayerColor = isMyTurn ? StoneColor.Black : StoneColor.White;
			_hasInferredColor = true;
			GD.Print($"[NET] LocalPlayerColor déduite: {LocalPlayerColor} (LocalPlayerId={LocalPlayerId}, currentPlayer={state.currentPlayer})");
		}
	}

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

	public async Task SendLeaveGame(string gameId)
	{
		var payload = new LeaveGameRequest { GameId = gameId };
		await Send(MessageType.LeaveGame, payload);
	}

	public async Task SendGetGameList()
	{

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
	}
	
	private GetGameStateResponse _lastGameState;
	private string _lastWrongReason;

	private void EmitGameListReceived()
	{
		GameListReceived?.Invoke(LastGameList);
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

	private void UpdateGameStateInfo(GetGameStateResponse state)
	{
		if (state != null)
		{
			CurrentGameState = state.GameState ?? "En cours";
			CurrentPlayerCount = state.PlayerCount;
			CurrentSpectatorCount = state.SpectatorCount;
		}
		else
		{
			CurrentGameState = "En cours";
			CurrentPlayerCount = 0;
			CurrentSpectatorCount = 0;
		}
	}
}
