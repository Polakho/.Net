using Godot;
using System;

public partial class LobbyScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	// Renseigne ces NodePath dans l'Inspecteur
	[Export] public NodePath GameListPath;

	private ItemList _gameListUI;

	private string _selectedGameId;

	public override void _Ready()
	{
		GD.Print("===== [LobbyScreen._Ready] DÉMARRAGE =====");
		
		_screenManager = GetParent<ScreenManager>();
		GD.Print($"[LobbyScreen] ScreenManager trouvé? {_screenManager != null}");

		var main = _screenManager.GetParent();
		GD.Print($"[LobbyScreen] main (parent du ScreenManager) trouvé? {main != null}");
		GD.Print($"[LobbyScreen] main.HasNode('NetClient')? {main != null && main.HasNode("NetClient")}");
		
		if (main != null && main.HasNode("NetClient"))
		{
			_net = main.GetNode<GameServerClient>("NetClient");
			GD.Print($"[LobbyScreen] ✓ GameServerClient récupéré: {_net != null}");
		}
		else
		{
			GD.PrintErr("[LobbyScreen] ✗ ERREUR: Impossible de trouver NetClient!");
		}


		_gameListUI = GetNode<ItemList>(GameListPath);
		GD.Print($"[LobbyScreen] GameListUI trouvé via NodePath '{GameListPath}'? {_gameListUI != null}");
		GD.Print($"[LobbyScreen] GameListUI est de type GameList? {_gameListUI is GameList}");

		if (_net != null)
		{
			GD.Print("[LobbyScreen] ✓ _net n'est pas null, souscription aux événements");
			_net.GameCreated += OnGameCreated;
			_net.JoinResultReceived += OnJoinResultReceived;
			
			// Passer le GameServerClient au GameList
			if (_gameListUI is GameList gameListScript)
			{
				GD.Print("[LobbyScreen] ✓ Appel de gameListScript.SetGameServerClient(_net)");
				gameListScript.SetGameServerClient(_net);
			}
			else
			{
				GD.PrintErr("[LobbyScreen] ✗ ERREUR: _gameListUI n'est pas castable en GameList!");
				GD.Print($"[LobbyScreen] Type réel de _gameListUI: {_gameListUI?.GetType().Name}");
			}
		}
		else
		{
			GD.PrintErr("[LobbyScreen] ✗ ERREUR: _net est NULL!");
		}

		// Connecter l'événement de sélection dans la liste
		if (_gameListUI != null)
		{
			GD.Print("[LobbyScreen] ✓ Connexion de l'événement ItemSelected");
			_gameListUI.ItemSelected += OnGameSelected;
		}
		else
		{
			GD.PrintErr("[LobbyScreen] ✗ ERREUR: _gameListUI est NULL, impossible de connecter ItemSelected!");
		}

		// Connecter l'événement de visibilité pour rafraîchir la liste quand le lobby devient visible
		VisibilityChanged += OnVisibilityChanged;

		GD.Print("===== [LobbyScreen._Ready] TERMINÉ =====");
	}

	// Rafraîchir la liste des games automatiquement quand le lobby devient visible
	private void OnVisibilityChanged()
	{
		if (Visible)
		{
			GD.Print("[LobbyScreen] ✓ Lobby est devenu visible, rafraîchissement de la liste des games");
			OnRefreshPressed();
		}
	}

	// Callback quand on sélectionne une game dans la liste
	private void OnGameSelected(long index)
	{
		if (_net?.LastGameList != null && index >= 0 && index < _net.LastGameList.Games.Count)
		{
			_selectedGameId = _net.LastGameList.Games[(int)index].Id;
			GD.Print($"[LobbyScreen] Game sélectionnée: {_selectedGameId}");
		}
	}

	public override void _ExitTree()
	{
		if (_net != null)
		{
			_net.GameCreated -= OnGameCreated;
			_net.JoinResultReceived -= OnJoinResultReceived;
		}
	}

	public void OnBackPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/main_menu_screen.tscn");
	}

	public async void OnCreatePressed()
	{
		if (_net == null) return;

		// Pour l'instant, board 9x9 et nom fixe
		await _net.SendCreateGame(9, "Godot Lobby Game");
	}

	public async void OnJoinPressed()
	{
		if (_net == null)
			return;

		// Si rien de sélectionné mais qu'on a une liste de parties, on prend la première
		if (string.IsNullOrEmpty(_selectedGameId) && _net.LastGameList != null && _net.LastGameList.Games.Count > 0)
		{
			_selectedGameId = _net.LastGameList.Games[0].Id;
		}

		await _net.SendJoinGame(_selectedGameId, asSpectator: false);
	}


	public async void OnSpectatePressed()
	{
		if (_net == null)
			return;

		// Si rien de sélectionné mais qu'on a une liste de parties, on prend la première
		if (string.IsNullOrEmpty(_selectedGameId) && _net.LastGameList != null && _net.LastGameList.Games.Count > 0)
		{
			_selectedGameId = _net.LastGameList.Games[0].Id;
		}

		// Marquer qu'on va rejoindre en tant que spectateur
		_net.IsJoiningAsSpectator = true;
		await _net.SendJoinGame(_selectedGameId, asSpectator: true);
	}

	// --- Callback de la liste (signal Godot) ---

	// --- Events réseau ---

	private void OnGameCreated(string gameId)
	{
		_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	private void OnJoinResultReceived(string result)
	{
		// Si le join est OK, on va en match ou en spectate selon le mode
		if (!string.IsNullOrEmpty(result) && (result.Contains("success", StringComparison.OrdinalIgnoreCase) || result.Contains("Joined", StringComparison.OrdinalIgnoreCase)))
		{
			if (_net.IsJoiningAsSpectator)
			{
				_net.IsJoiningAsSpectator = false; // Réinitialiser le flag
				_screenManager.GoTo("res://Scenes/Screens/spectate_screen.tscn");
			}
			else
			{
				_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
			}
		}
		else
		{
			// Afficher un message d'erreur approprié
			string errorMessage = "Impossible de rejoindre la partie.";
			if (result.Contains("full", StringComparison.OrdinalIgnoreCase))
			{
				errorMessage = "La partie est complète (2 joueurs maximum).";
			}
			else if (result.Contains("started", StringComparison.OrdinalIgnoreCase))
			{
				errorMessage = "La partie a déjà commencé.";
			}
			else if (result.Contains("not found", StringComparison.OrdinalIgnoreCase))
			{
				errorMessage = "Partie introuvable.";
			}
			
			GD.PrintErr($"[LobbyScreen] Erreur de jointure: {errorMessage} (Résultat: {result})");
			// TODO: Afficher un popup ou un label d'erreur à l'utilisateur
		}
	}

	public async void OnRefreshPressed()
	{
		if (_net == null)
			return;

		GD.Print("[LobbyScreen] Rafraîchissement de la liste des parties...");
		await _net.SendGetGameList();
	}
}
