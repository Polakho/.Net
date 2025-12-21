using Godot;

public partial class MatchScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	[Export] public NodePath BoardControllerPath;
	[Export] public string GameStateLabelPath = "Ui/InfoPanel/InfoMargin/InfoLayout/GameStateLabel";
	[Export] public string PlayerCountLabelPath = "Ui/InfoPanel/InfoMargin/InfoLayout/PlayerCountLabel";
	[Export] public string SpectatorCountLabelPath = "Ui/InfoPanel/InfoMargin/InfoLayout/SpectatorCountLabel";
	[Export] public string MyColorLabelPath = "Ui/InfoPanel/InfoMargin/InfoLayout/MyColorLabel";
	[Export] public string TurnLabelPath = "Ui/InfoPanel/InfoMargin/InfoLayout/TurnLabel";
	[Export] public string PassButtonPath = "Ui/PassButton";
	[Export] public float RefreshIntervalSeconds = 0.1f;

	[Export] public string WaitingOverlayPath = "WaitingOverlay";
	[Export] public string OverlayGameStateLabelPath = "WaitingOverlay/Content/ContentMargin/VBox/OverlayGameStateLabel";
	[Export] public string OverlayPlayerCountLabelPath = "WaitingOverlay/Content/ContentMargin/VBox/OverlayPlayerCountLabel";

	[Export] public string GameOverPopupPath = "GameOverLayer";
	[Export] public string GameOverTitleLabelPath = "GameOverLayer/GameOverPopup/Panel/PanelMargin/VBox/TitleLabel";
	[Export] public string GameOverScoreLabelPath = "GameOverLayer/GameOverPopup/Panel/PanelMargin/VBox/ScoreLabel";
	[Export] public string GameOverButtonPath = "GameOverLayer/GameOverPopup/Panel/PanelMargin/VBox/BackButton";

	private BoardController _board;
	private Label _gameStateLabel;
	private Label _playerCountLabel;
	private Label _spectatorCountLabel;
	private Label _myColorLabel;
	private Label _turnLabel;
	private Button _passButton;
	private Timer _refreshTimer;

	private Control _waitingOverlay;
	private Label _overlayGameStateLabel;
	private Label _overlayPlayerCountLabel;

	private CanvasLayer _gameOverPopup;
	private Label _gameOverTitleLabel;
	private Label _gameOverScoreLabel;
	private Button _gameOverButton;

	private CanvasItem _boardRoot;
	private Node2D _boardRootNode;
	private Sprite2D _gridSprite;
	private Sprite2D _backgroundSprite;

	private bool _gameHasStarted = false;
	private bool _gameOverShown = false;

	public override void _Ready()
	{
		_screenManager = GetParent<ScreenManager>();
		_net = _screenManager.NetClient;

		_board = GetNodeOrNull<BoardController>(BoardControllerPath);
		if (_board == null)
			GD.PrintErr("[MatchScreen] BoardController introuvable, renseigne BoardControllerPath.");

		_gameStateLabel = GetNodeOrNull<Label>(GameStateLabelPath);
		_playerCountLabel = GetNodeOrNull<Label>(PlayerCountLabelPath);
		_spectatorCountLabel = GetNodeOrNull<Label>(SpectatorCountLabelPath);
		_myColorLabel = GetNodeOrNull<Label>(MyColorLabelPath);
		_turnLabel = GetNodeOrNull<Label>(TurnLabelPath);
		_passButton = GetNodeOrNull<Button>(PassButtonPath);

		if (_gameStateLabel == null)
			GD.PrintErr("[MatchScreen] GameStateLabel introuvable.");
		if (_playerCountLabel == null)
			GD.PrintErr("[MatchScreen] PlayerCountLabel introuvable.");
		if (_spectatorCountLabel == null)
			GD.PrintErr("[MatchScreen] SpectatorCountLabel introuvable.");
		if (_myColorLabel == null)
			GD.PrintErr("[MatchScreen] MyColorLabel introuvable.");
		if (_turnLabel == null)
			GD.PrintErr("[MatchScreen] TurnLabel introuvable.");
		if (_passButton == null)
			GD.PrintErr("[MatchScreen] PassButton introuvable.");

		_waitingOverlay = GetNodeOrNull<Control>(WaitingOverlayPath);
		_overlayGameStateLabel = GetNodeOrNull<Label>(OverlayGameStateLabelPath);
		_overlayPlayerCountLabel = GetNodeOrNull<Label>(OverlayPlayerCountLabelPath);
		if (_waitingOverlay == null)
			GD.PrintErr("[MatchScreen] WaitingOverlay introuvable.");

		_gameOverPopup = GetNodeOrNull<CanvasLayer>(GameOverPopupPath);
		_gameOverTitleLabel = GetNodeOrNull<Label>(GameOverTitleLabelPath);
		_gameOverScoreLabel = GetNodeOrNull<Label>(GameOverScoreLabelPath);
		_gameOverButton = GetNodeOrNull<Button>(GameOverButtonPath);
		if (_gameOverPopup == null)
			GD.PrintErr("[MatchScreen] GameOverPopup introuvable.");
		else
			_gameOverPopup.Visible = false; // Masquer au démarrage
		
		if (_gameOverButton != null)
			_gameOverButton.Pressed += OnGameOverBackPressed;

		_boardRoot = GetNodeOrNull<CanvasItem>("BoardRoot");
		_boardRootNode = GetNodeOrNull<Node2D>("BoardRoot");
		_gridSprite = GetNodeOrNull<Sprite2D>("BoardRoot/GridSprite");
		_backgroundSprite = GetNodeOrNull<Sprite2D>("BoardRoot/Background");

		CenterBoardRoot();

		if (_net != null)
		{
			_net.GameStateReceived += OnGameStateReceived;
			_net.WrongMoveReceived += OnWrongMoveReceived;

			if (_board != null)
			{
				_board.SetNetworkClient(_net);
			}

			UpdateLabels();
			UpdateWaitingOverlay();

			if (!string.IsNullOrEmpty(_net.CurrentGameId))
			{
				_ = _net.SendGetGameState(_net.CurrentGameId);
			}

			StartRefreshTimer();
		}
		else
		{

			UpdateWaitingOverlay();
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
		
		GD.Print($"[MatchScreen.CenterBoardRoot] viewportSize={viewportSize}, centeredPos={centeredPos}");
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

		if (_gameHasStarted)
		{

			if (_refreshTimer != null)
				_refreshTimer.WaitTime = 2.0f;
		}
		
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
			_net.WrongMoveReceived -= OnWrongMoveReceived;
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

	public void OnPassButtonPressed()
	{
		if (_net == null || string.IsNullOrEmpty(_net.CurrentGameId))
		{
			GD.PrintErr("[MatchScreen] Impossible de passer : pas de partie en cours");
			return;
		}

		if (!_net.IsLocalPlayersTurn)
		{
			GD.Print("[MatchScreen] Ce n'est pas votre tour, impossible de passer");
			return;
		}

		if (_net.CurrentGameState != "InProgress")
		{
			GD.Print("[MatchScreen] La partie n'est pas en cours, impossible de passer");
			return;
		}

		GD.Print("[MatchScreen] Le joueur passe son tour");
		_ = _net.SendMakeMove(_net.CurrentGameId, 0, 0, true);
	}

	private void OnGameStateReceived(GetGameStateResponse state)
	{
		if (_board == null) return;

		string gameStateStatus = state.GameState ?? "Pending";
		if (gameStateStatus == "WaitingForPlayers")
			gameStateStatus = "Pending";
		
		GD.Print($"[MatchScreen] OnGameStateReceived: ServerState={state.GameState}, MappedState={gameStateStatus}");

		if (gameStateStatus == "InProgress" && state.PlayerCount >= 2)
		{
			_gameHasStarted = true;
		}

		var localState = new BoardController.GameState
		{
			BoardSize = state.BoardSize,

			CurrentPlayer = 0,
			GameStateStatus = gameStateStatus // "Pending", "InProgress", "Finished"
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
		UpdateWaitingOverlay();

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

		if (_myColorLabel != null && _net != null)
		{
			_myColorLabel.Text = _net.LocalPlayerColor switch
			{
				StoneColor.Black => "Vous jouez: Noir",
				StoneColor.White => "Vous jouez: Blanc",
				_ => "Vous jouez: (en attente...)"
			};
		}

		if (_turnLabel != null && _net != null)
		{
			if (_net.CurrentPlayerCount < 2)
				_turnLabel.Text = "En attente d'un adversaire...";
			else
				_turnLabel.Text = _net.IsLocalPlayersTurn ? "Votre tour" : "Tour de l'adversaire";
		}

		if (_passButton != null && _net != null)
		{

			bool canPass = _net.CurrentGameState == "InProgress" 
						   && _net.IsLocalPlayersTurn 
						   && _net.CurrentPlayerCount >= 2;
			
			_passButton.Disabled = !canPass;

			if (_net.CurrentPlayerCount < 2)
				_passButton.Text = "Passer (en attente...)";
			else if (_net.CurrentGameState != "InProgress")
				_passButton.Text = "Passer (partie terminée)";
			else if (!_net.IsLocalPlayersTurn)
				_passButton.Text = "Passer (pas votre tour)";
			else
				_passButton.Text = "Passer mon tour";
		}
	}

	private bool ShouldShowWaitingOverlay()
	{

		if (_gameHasStarted)
			return false;

		if (_net == null) return true;
		return _net.CurrentPlayerCount < 2;
	}

	private void UpdateWaitingOverlay()
	{
		if (_waitingOverlay == null)
			return;

		bool show = ShouldShowWaitingOverlay();

		if (_waitingOverlay.Visible != show)
		{
			GD.Print($"[MatchScreen] Overlay visibility change: {_waitingOverlay.Visible} → {show} (_gameHasStarted={_gameHasStarted}, PlayerCount={_net?.CurrentPlayerCount})");
		}
		
		_waitingOverlay.Visible = show;

		if (_boardRoot != null)
			_boardRoot.Visible = true;

		if (_overlayGameStateLabel != null)
		{
			_overlayGameStateLabel.Text = _gameStateLabel?.Text ?? $"État: {_net?.CurrentGameState}";
		}
		if (_overlayPlayerCountLabel != null)
		{
			_overlayPlayerCountLabel.Text = _playerCountLabel?.Text ?? $"Joueurs: {_net?.CurrentPlayerCount ?? 0}/2";
		}
	}

	private void OnWrongMoveReceived(string reason)
	{
		GD.PrintErr($"[MatchScreen] Coup invalide: {reason}");

		if (_gameStateLabel != null)
		{
			_gameStateLabel.Text = $"Erreur: {reason}";
		}

		if (!string.IsNullOrEmpty(_net?.CurrentGameId))
		{
			_ = _net.SendGetGameState(_net.CurrentGameId);
		}
		
		UpdateWaitingOverlay();
	}

	private void ShowGameOverPopup(GetGameStateResponse state)
	{
		if (_gameOverPopup == null) return;

		string resultText;
		if (string.IsNullOrEmpty(state.WinnerId))
		{
			resultText = "Match Nul !";
		}
		else if (state.WinnerId == _net.LocalPlayerId)
		{
			resultText = "Victoire !";
		}
		else
		{
			resultText = "Défaite !";
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
