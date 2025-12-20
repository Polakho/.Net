using Godot;

public partial class MatchScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	[Export] public NodePath BoardControllerPath;
	[Export] public string GameStateLabelPath = "Ui/GameStateLabel";
	[Export] public string PlayerCountLabelPath = "Ui/PlayerCountLabel";
	[Export] public float RefreshIntervalSeconds = 1.0f;

	// Overlay d'attente (bloque les inputs tant que la partie n'est pas prête)
	[Export] public string WaitingOverlayPath = "WaitingOverlay";
	[Export] public string OverlayGameStateLabelPath = "WaitingOverlay/Content/VBox/OverlayGameStateLabel";
	[Export] public string OverlayPlayerCountLabelPath = "WaitingOverlay/Content/VBox/OverlayPlayerCountLabel";

	private BoardController _board;
	private Label _gameStateLabel;
	private Label _playerCountLabel;
	private Timer _refreshTimer;

	private Control _waitingOverlay;
	private Label _overlayGameStateLabel;
	private Label _overlayPlayerCountLabel;

	// Node racine du plateau (pour désactiver les clics sans bloquer l'UI)
	private CanvasItem _boardRoot;
	private Node2D _boardRootNode;
	private Sprite2D _gridSprite;
	private Sprite2D _backgroundSprite;

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

		// Overlay d'attente
		_waitingOverlay = GetNodeOrNull<Control>(WaitingOverlayPath);
		_overlayGameStateLabel = GetNodeOrNull<Label>(OverlayGameStateLabelPath);
		_overlayPlayerCountLabel = GetNodeOrNull<Label>(OverlayPlayerCountLabelPath);
		if (_waitingOverlay == null)
			GD.PrintErr("[MatchScreen] WaitingOverlay introuvable.");

		_boardRoot = GetNodeOrNull<CanvasItem>("BoardRoot");
		_boardRootNode = GetNodeOrNull<Node2D>("BoardRoot");
		_gridSprite = GetNodeOrNull<Sprite2D>("BoardRoot/GridSprite");
		_backgroundSprite = GetNodeOrNull<Sprite2D>("BoardRoot/Background");

		CenterBoardRoot();

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
			UpdateWaitingOverlay();

			// Si on connaît déjà la game courante, on demande son état
			if (!string.IsNullOrEmpty(_net.CurrentGameId))
			{
				_ = _net.SendGetGameState(_net.CurrentGameId);
			}

			// Démarrer le timer de refresh automatique
			StartRefreshTimer();
		}
		else
		{
			// Pas de réseau => on bloque le plateau par sécurité
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

	private void CenterBoardRoot()
	{
		if (_boardRootNode == null)
			return;

		Vector2 viewportSize = GetViewportRect().Size;

		// Taille du plateau en pixels écran.
		// Priorité: GridSprite (board.png). Fallback: Background.
		Sprite2D sprite = _gridSprite;
		Vector2 texSize = Vector2.Zero;
		Vector2 scale = Vector2.One;
		Vector2 spriteOffset = Vector2.Zero;

		if (sprite != null && sprite.Texture != null)
		{
			texSize = sprite.Texture.GetSize();
			scale = sprite.GlobalScale.Abs();
			spriteOffset = sprite.Offset;
		}
		else if (_backgroundSprite != null && _backgroundSprite.Texture != null)
		{
			sprite = _backgroundSprite;
			texSize = sprite.Texture.GetSize();
			scale = sprite.GlobalScale.Abs();
			spriteOffset = Vector2.Zero;
		}
		else
		{
			return;
		}

		Vector2 boardSize = texSize * scale;

		// Dans ta scène, GridSprite n'est pas centered et a un offset (64,64).
		// Donc le plateau "visible" commence à BoardRoot + Offset.
		Vector2 desiredTopLeft = (viewportSize - boardSize) / 2f;
		_boardRootNode.GlobalPosition = desiredTopLeft - spriteOffset;
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

		// Mettre à jour les labels + overlay
		UpdateLabels();
		UpdateWaitingOverlay();
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

	private bool ShouldShowWaitingOverlay()
	{
		// Règle principale demandée: tant que 2 joueurs ne sont pas connectés
		if (_net == null) return true;
		return _net.CurrentPlayerCount < 2;
	}

	private void UpdateWaitingOverlay()
	{
		if (_waitingOverlay == null)
			return;

		bool show = ShouldShowWaitingOverlay();
		_waitingOverlay.Visible = show;

		// Important: ne PAS masquer BoardRoot.
		// (On laisse le plateau visible et on bloque les interactions via l'overlay.)
		if (_boardRoot != null)
			_boardRoot.Visible = true;

		// Synchroniser les textes avec les labels existants (si possible)
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
		// Optionnel: afficher le message d'erreur à l'utilisateur
		if (_gameStateLabel != null)
		{
			_gameStateLabel.Text = $"Erreur: {reason}";
		}
		UpdateWaitingOverlay();
	}
}
