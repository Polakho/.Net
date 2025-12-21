using Godot;

public partial class SpectateScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	[Export] public NodePath BoardControllerPath;
	[Export] public string GameStateLabelPath = "Ui/GameStateLabel";
	[Export] public string PlayerCountLabelPath = "Ui/PlayerCountLabel";
	[Export] public string SpectatorCountLabelPath = "Ui/SpectatorCountLabel";
	[Export] public string SpectatorLabelPath = "Ui/SpectatorLabel";
	[Export] public float RefreshIntervalSeconds = 2.0f;

	[Export] public string GameOverPopupPath = "GameOverLayer";
	[Export] public string GameOverTitleLabelPath = "GameOverLayer/GameOverPopup/Panel/VBox/TitleLabel";
	[Export] public string GameOverScoreLabelPath = "GameOverLayer/GameOverPopup/Panel/VBox/ScoreLabel";
	[Export] public string GameOverButtonPath = "GameOverLayer/GameOverPopup/Panel/VBox/BackButton";

	private BoardController _board;
	private Label _gameStateLabel;
	private Label _playerCountLabel;
	private Label _spectatorCountLabel;
	private Label _spectatorLabel;
	private Timer _refreshTimer;

	private CanvasLayer _gameOverPopup;
	private Label _gameOverTitleLabel;
	private Label _gameOverScoreLabel;
	private Button _gameOverButton;

	private Node2D _boardRootNode;
	private bool _gameOverShown = false;

	public override void _Ready()
	{
		_screenManager = GetParent<ScreenManager>();
		_net = _screenManager.NetClient;

		_board = GetNodeOrNull<BoardController>(BoardControllerPath);
		if (_board == null)
			GD.PrintErr("[SpectateScreen] BoardController introuvable, renseigne BoardControllerPath.");

		_gameStateLabel = GetNodeOrNull<Label>(GameStateLabelPath);
		_playerCountLabel = GetNodeOrNull<Label>(PlayerCountLabelPath);
		_spectatorCountLabel = GetNodeOrNull<Label>(SpectatorCountLabelPath);
		_spectatorLabel = GetNodeOrNull<Label>(SpectatorLabelPath);

		if (_gameStateLabel == null)
			GD.PrintErr("[SpectateScreen] GameStateLabel introuvable.");
		if (_playerCountLabel == null)
			GD.PrintErr("[SpectateScreen] PlayerCountLabel introuvable.");
		if (_spectatorCountLabel == null)
			GD.PrintErr("[SpectateScreen] SpectatorCountLabel introuvable.");
		if (_spectatorLabel == null)
			GD.PrintErr("[SpectateScreen] SpectatorLabel introuvable.");

		_gameOverPopup = GetNodeOrNull<CanvasLayer>(GameOverPopupPath);
		_gameOverTitleLabel = GetNodeOrNull<Label>(GameOverTitleLabelPath);
		_gameOverScoreLabel = GetNodeOrNull<Label>(GameOverScoreLabelPath);
		_gameOverButton = GetNodeOrNull<Button>(GameOverButtonPath);
		if (_gameOverPopup == null)
			GD.PrintErr("[SpectateScreen] GameOverPopup introuvable.");
		else
			_gameOverPopup.Visible = false;
		
		if (_gameOverButton != null)
			_gameOverButton.Pressed += OnGameOverBackPressed;

		_boardRootNode = GetNodeOrNull<Node2D>("BoardRoot");

		CenterBoardRoot();

		if (_net != null)
		{
			_net.GameStateReceived += OnGameStateReceived;

			if (_board != null)
			{
				_board.SetNetworkClient(_net);
				_board.SetSpectatorMode(true);
			}

			UpdateLabels();

			if (!string.IsNullOrEmpty(_net.CurrentGameId))
			{
				_ = _net.SendGetGameState(_net.CurrentGameId);
			}

			StartRefreshTimer();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMSizeChanged)
		{
			CenterBoardRoot();
		}
	}

	public override void _Process(double delta)
	{

		if (_boardRootNode != null)
		{
			Vector2 currentViewportSize = GetViewportRect().Size;
			float boardSizePx = 640f;
			Vector2 expectedPos = (currentViewportSize - new Vector2(boardSizePx, boardSizePx)) / 2f;
			
			expectedPos = new Vector2(Mathf.Round(expectedPos.X), Mathf.Round(expectedPos.Y));
			
			if (_boardRootNode.Position != expectedPos)
			{
				_boardRootNode.Position = expectedPos;
			}
		}
	}

	private void CenterBoardRoot()
	{
		if (_boardRootNode == null)
			return;

		Vector2 viewportSize = GetViewportRect().Size;
		float boardSizePx = 640f;
		
		Vector2 centeredPos = (viewportSize - new Vector2(boardSizePx, boardSizePx)) / 2f;
		centeredPos = new Vector2(Mathf.Round(centeredPos.X), Mathf.Round(centeredPos.Y));
		
		_boardRootNode.Position = centeredPos;
		
		GD.Print($"[SpectateScreen.CenterBoardRoot] viewportSize={viewportSize}, centeredPos={centeredPos}");
	}

	private void StartRefreshTimer()
	{
		if (_refreshTimer != null)
			_refreshTimer.QueueFree();

		_refreshTimer = new Timer();
		AddChild(_refreshTimer);
		_refreshTimer.WaitTime = RefreshIntervalSeconds;
		_refreshTimer.Timeout += OnRefreshTimerTimeout;
		_refreshTimer.Start();
	}

	private void OnRefreshTimerTimeout()
	{
		if (!string.IsNullOrEmpty(_net?.CurrentGameId))
		{
			_ = _net.SendGetGameState(_net.CurrentGameId);
		}
	}

	public override void _ExitTree()
	{
		if (_refreshTimer != null)
		{
			_refreshTimer.QueueFree();
			_refreshTimer = null;
		}
		
		if (_net != null)
		{
			_net.GameStateReceived -= OnGameStateReceived;
		}
	}

	public void OnBackPressed()
	{

		if (!string.IsNullOrEmpty(_net?.CurrentGameId))
		{
			_ = _net.SendLeaveGame(_net.CurrentGameId);
		}

		_net?.ClearCurrentGameId();

		_screenManager.GoTo("res://Scenes/Screens/lobby_screen.tscn");
	}

	private void OnGameStateReceived(GetGameStateResponse state)
	{
		if (_board == null) return;

		string gameStateStatus = state.GameState ?? "Pending";
		if (gameStateStatus == "WaitingForPlayers")
			gameStateStatus = "Pending";
		
		GD.Print($"[SpectateScreen] OnGameStateReceived: ServerState={state.GameState}, MappedState={gameStateStatus}");

		var localState = new BoardController.GameState
		{
			BoardSize = state.BoardSize,
			CurrentPlayer = 0,
			GameStateStatus = gameStateStatus
		};

		for (int x = 0; x < state.BoardSize; x++)
		for (int y = 0; y < state.BoardSize; y++)
		{
			var cell = state.Board[x, y];
			if (cell == null) continue;

			int player = cell == StoneColor.Black ? 1 : 2;
			localState.Stones.Add(new BoardController.StoneState(x, y, player));
		}

		_board.ApplyGameState(localState);

		UpdateLabels();

		if (gameStateStatus == "Finished" && !_gameOverShown)
		{
			ShowGameOverPopup(state);
			_gameOverShown = true;
		}
	}

	private void UpdateLabels()
	{
		if (_gameStateLabel != null && _net != null)
		{
			_gameStateLabel.Text = $"État: {_net.CurrentGameState}";
		}

		if (_playerCountLabel != null && _net != null)
		{
			_playerCountLabel.Text = $"Joueurs: {_net.CurrentPlayerCount}/2";
		}

		if (_spectatorCountLabel != null && _net != null)
		{
			_spectatorCountLabel.Text = $"Spectateurs: {_net.CurrentSpectatorCount}";
		}

		if (_spectatorLabel != null)
		{
			_spectatorLabel.Text = "Mode: Spectateur";
		}
	}

	private void ShowGameOverPopup(GetGameStateResponse state)
	{
		if (_gameOverPopup == null) return;

		string resultText;
		if (string.IsNullOrEmpty(state.WinnerId))
		{
			resultText = "Match Nul !";
		}
		else
		{
			resultText = "Partie Terminée !";
		}

		if (_gameOverTitleLabel != null)
			_gameOverTitleLabel.Text = resultText;

		if (_gameOverScoreLabel != null)
		{
			_gameOverScoreLabel.Text = $"Score Final:\nNoir: {state.BlackScore}\nBlanc: {state.WhiteScore}";
		}

		_gameOverPopup.Visible = true;
	}

	private void OnGameOverBackPressed()
	{

		if (_gameOverPopup != null)
			_gameOverPopup.Visible = false;

		OnBackPressed();
	}
}
