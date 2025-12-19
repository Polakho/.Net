using Godot;

public partial class MatchScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	[Export] public NodePath BoardControllerPath;
	[Export] public string GameStateLabelPath = "Ui/GameStateLabel";
	[Export] public string PlayerCountLabelPath = "Ui/PlayerCountLabel";
	[Export] public float RefreshIntervalSeconds = 1.0f;

	private BoardController _board;
	private Label _gameStateLabel;
	private Label _playerCountLabel;
	private Timer _refreshTimer;

	public override void _Ready()
	{
		_screenManager = GetParent<ScreenManager>();
		_net = _screenManager.NetClient;

		_board = GetNodeOrNull<BoardController>(BoardControllerPath);
		if (_board == null)
			GD.PrintErr("[MatchScreen] BoardController introuvable, renseigne BoardControllerPath.");

		// Récupérer les labels
		_gameStateLabel = GetNodeOrNull<Label>(GameStateLabelPath);
		_playerCountLabel = GetNodeOrNull<Label>(PlayerCountLabelPath);

		if (_gameStateLabel == null)
			GD.PrintErr("[MatchScreen] GameStateLabel introuvable.");
		if (_playerCountLabel == null)
			GD.PrintErr("[MatchScreen] PlayerCountLabel introuvable.");

		if (_net != null)
		{
			_net.GameStateReceived += OnGameStateReceived;
			_net.WrongMoveReceived += OnWrongMoveReceived;

			// Passer le NetworkClient au BoardController
			if (_board != null)
			{
				_board.SetNetworkClient(_net);
			}

			// Mettre à jour les labels avec les infos actuelles
			UpdateLabels();

			// Si on connaît déjà la game courante, on demande son état
			if (!string.IsNullOrEmpty(_net.CurrentGameId))
			{
				_ = _net.SendGetGameState(_net.CurrentGameId);
			}

			// Démarrer le timer de refresh automatique
			StartRefreshTimer();
		}
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
			_net.WrongMoveReceived -= OnWrongMoveReceived;
		}
	}

	public void OnBackPressed()
	{
		// Déconnecter le joueur de la partie
		if (!string.IsNullOrEmpty(_net?.CurrentGameId))
		{
			_ = _net.SendLeaveGame(_net.CurrentGameId);
		}

		// Réinitialiser l'ID de la partie courante
		_net?.ClearCurrentGameId();

		// Retour au lobby
		_screenManager.GoTo("res://Scenes/Screens/lobby_screen.tscn");
	}

	private void OnGameStateReceived(GetGameStateResponse state)
	{
		if (_board == null) return;

		// Conversion GetGameStateResponse -> BoardController.GameState (ton type interne)
		var localState = new BoardController.GameState
		{
			BoardSize = state.BoardSize,
			CurrentPlayer = state.currentPlayer == "Black" ? 1 : 2,
			GameStateStatus = state.GameState ?? "Pending" // "Pending", "InProgress", "Finished"
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

		// Mettre à jour les labels
		UpdateLabels();
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
	}

	private void OnWrongMoveReceived(string reason)
	{
		GD.PrintErr($"[MatchScreen] Coup invalide: {reason}");
		// Optionnel: afficher le message d'erreur à l'utilisateur
		if (_gameStateLabel != null)
		{
			_gameStateLabel.Text = $"Erreur: {reason}";
		}
	}
}
