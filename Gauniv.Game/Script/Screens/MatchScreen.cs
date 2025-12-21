using Godot;

public partial class MatchScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	[Export] public NodePath BoardControllerPath;
	[Export] public string GameStateLabelPath = "Ui/GameStateLabel";
	[Export] public string PlayerCountLabelPath = "Ui/PlayerCountLabel";
	[Export] public string MyColorLabelPath = "Ui/MyColorLabel";
	[Export] public string TurnLabelPath = "Ui/TurnLabel";
	[Export] public string PassButtonPath = "Ui/PassButton";
	[Export] public float RefreshIntervalSeconds = 0.1f;

	// Overlay d'attente (bloque les inputs tant que la partie n'est pas prête)
	[Export] public string WaitingOverlayPath = "WaitingOverlay";
	[Export] public string OverlayGameStateLabelPath = "WaitingOverlay/Content/VBox/OverlayGameStateLabel";
	[Export] public string OverlayPlayerCountLabelPath = "WaitingOverlay/Content/VBox/OverlayPlayerCountLabel";

	// Popup de fin de partie
	[Export] public string GameOverPopupPath = "GameOverLayer";
	[Export] public string GameOverTitleLabelPath = "GameOverLayer/GameOverPopup/Panel/VBox/TitleLabel";
	[Export] public string GameOverScoreLabelPath = "GameOverLayer/GameOverPopup/Panel/VBox/ScoreLabel";
	[Export] public string GameOverButtonPath = "GameOverLayer/GameOverPopup/Panel/VBox/BackButton";

	private BoardController _board;
	private Label _gameStateLabel;
	private Label _playerCountLabel;
	private Label _myColorLabel;
	private Label _turnLabel;
	private Button _passButton;
	private Timer _refreshTimer;

	private Control _waitingOverlay;
	private Label _overlayGameStateLabel;
	private Label _overlayPlayerCountLabel;

	// Popup de fin de partie
	private CanvasLayer _gameOverPopup;
	private Label _gameOverTitleLabel;
	private Label _gameOverScoreLabel;
	private Button _gameOverButton;

	// Node racine du plateau (pour désactiver les clics sans bloquer l'UI)
	private CanvasItem _boardRoot;
	private Node2D _boardRootNode;
	private Sprite2D _gridSprite;
	private Sprite2D _backgroundSprite;

	// Flag pour éviter de réafficher l'overlay après le début de partie
	private bool _gameHasStarted = false;
	private bool _gameOverShown = false;

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
		_myColorLabel = GetNodeOrNull<Label>(MyColorLabelPath);
		_turnLabel = GetNodeOrNull<Label>(TurnLabelPath);
		_passButton = GetNodeOrNull<Button>(PassButtonPath);

		if (_gameStateLabel == null)
			GD.PrintErr("[MatchScreen] GameStateLabel introuvable.");
		if (_playerCountLabel == null)
			GD.PrintErr("[MatchScreen] PlayerCountLabel introuvable.");
		if (_myColorLabel == null)
			GD.PrintErr("[MatchScreen] MyColorLabel introuvable.");
		if (_turnLabel == null)
			GD.PrintErr("[MatchScreen] TurnLabel introuvable.");
		if (_passButton == null)
			GD.PrintErr("[MatchScreen] PassButton introuvable.");

		// Overlay d'attente
		_waitingOverlay = GetNodeOrNull<Control>(WaitingOverlayPath);
		_overlayGameStateLabel = GetNodeOrNull<Label>(OverlayGameStateLabelPath);
		_overlayPlayerCountLabel = GetNodeOrNull<Label>(OverlayPlayerCountLabelPath);
		if (_waitingOverlay == null)
			GD.PrintErr("[MatchScreen] WaitingOverlay introuvable.");

		// Popup de fin de partie
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

	public override void _Process(double delta)
	{
		// S'assurer que le plateau reste centré à chaque frame
		// Cela garantit qu'aucun décalage ne se produit
		if (_boardRootNode != null)
		{
			Vector2 currentViewportSize = GetViewportRect().Size;
			float boardSizePx = 640f;
			Vector2 expectedPos = (currentViewportSize - new Vector2(boardSizePx, boardSizePx)) / 2f;
			
			// Arrondir pour éviter les positions en demi-pixel qui peuvent causer du flou
			expectedPos = new Vector2(Mathf.Round(expectedPos.X), Mathf.Round(expectedPos.Y));
			
			// Mettre à jour uniquement si nécessaire
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

		// Le plateau (background.png) fait 640x640px
		// On veut simplement le centrer dans la fenêtre
		float boardSizePx = 640f;
		
		// Position pour centrer le plateau (coin supérieur gauche du plateau)
		Vector2 centeredPos = (viewportSize - new Vector2(boardSizePx, boardSizePx)) / 2f;
		
		// Arrondir pour éviter les positions en demi-pixel qui peuvent causer du flou ou des décalages
		centeredPos = new Vector2(Mathf.Round(centeredPos.X), Mathf.Round(centeredPos.Y));
		
		// Le BoardRoot contient Background et GridSprite qui ne sont pas centrés (centered=false)
		// donc ils commencent à la position du BoardRoot
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
		// Pendant la partie, le serveur broadcast déjà les changements d'état
		// Donc on réduit la fréquence des requêtes pour éviter la surcharge
		if (_gameHasStarted)
		{
			// Refresh moins fréquent pendant la partie (toutes les 2 secondes)
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

		// Envoyer le pass au serveur (x=0, y=0 mais isPass=true)
		GD.Print("[MatchScreen] Le joueur passe son tour");
		_ = _net.SendMakeMove(_net.CurrentGameId, 0, 0, true);
	}

	private void OnGameStateReceived(GetGameStateResponse state)
	{
		if (_board == null) return;

		// Mapping des états du serveur vers le client
		string gameStateStatus = state.GameState ?? "Pending";
		if (gameStateStatus == "WaitingForPlayers")
			gameStateStatus = "Pending";
		
		GD.Print($"[MatchScreen] OnGameStateReceived: ServerState={state.GameState}, MappedState={gameStateStatus}");

		// Si la partie est en cours et qu'on a 2 joueurs, marquer que la partie a commencé
		if (gameStateStatus == "InProgress" && state.PlayerCount >= 2)
		{
			_gameHasStarted = true;
		}

		// Conversion GetGameStateResponse -> BoardController.GameState (ton type interne)
		var localState = new BoardController.GameState
		{
			BoardSize = state.BoardSize,
			// Le serveur envoie un playerId, pas une couleur. Pour l'instant on laisse l'affichage du tour au niveau UI,
			// et on garde un mapping simple pour le plateau (1=noir, 2=blanc) basé uniquement sur les pierres.
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

		// Mettre à jour les labels + overlay
		UpdateLabels();
		UpdateWaitingOverlay();

		// Vérifier si la partie est terminée et afficher le popup
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

		// Activer/désactiver le bouton "Passer" selon le contexte
		if (_passButton != null && _net != null)
		{
			// Le bouton est actif seulement si :
			// - La partie est en cours
			// - C'est le tour du joueur local
			// - Il y a 2 joueurs connectés
			bool canPass = _net.CurrentGameState == "InProgress" 
						   && _net.IsLocalPlayersTurn 
						   && _net.CurrentPlayerCount >= 2;
			
			_passButton.Disabled = !canPass;
			
			// Optionnel : changer le texte du bouton pour indiquer pourquoi il est désactivé
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
		// Une fois que la partie a commencé, ne plus afficher l'overlay
		if (_gameHasStarted)
			return false;

		// Règle principale demandée: tant que 2 joueurs ne sont pas connectés
		if (_net == null) return true;
		return _net.CurrentPlayerCount < 2;
	}

	private void UpdateWaitingOverlay()
	{
		if (_waitingOverlay == null)
			return;

		bool show = ShouldShowWaitingOverlay();
		
		// Log pour déboguer le comportement de l'overlay
		if (_waitingOverlay.Visible != show)
		{
			GD.Print($"[MatchScreen] Overlay visibility change: {_waitingOverlay.Visible} → {show} (_gameHasStarted={_gameHasStarted}, PlayerCount={_net?.CurrentPlayerCount})");
		}
		
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
		
		// Demander immédiatement l'état du jeu pour être sûr d'être à jour
		if (!string.IsNullOrEmpty(_net?.CurrentGameId))
		{
			_ = _net.SendGetGameState(_net.CurrentGameId);
		}
		
		UpdateWaitingOverlay();
	}

	private void ShowGameOverPopup(GetGameStateResponse state)
	{
		if (_gameOverPopup == null) return;

		// Déterminer si le joueur local a gagné, perdu ou match nul
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

		// Afficher le titre et les scores
		if (_gameOverTitleLabel != null)
			_gameOverTitleLabel.Text = resultText;

		if (_gameOverScoreLabel != null)
		{
			_gameOverScoreLabel.Text = $"Score Final:\nNoir: {state.BlackScore}\nBlanc: {state.WhiteScore}";
		}

		// Afficher le popup
		_gameOverPopup.Visible = true;
		GD.Print($"[MatchScreen] Affichage du popup de fin de partie: {resultText}");
	}

	private void OnGameOverBackPressed()
	{
		// Masquer le popup
		if (_gameOverPopup != null)
			_gameOverPopup.Visible = false;

		// Retourner au lobby
		OnBackPressed();
	}
}
