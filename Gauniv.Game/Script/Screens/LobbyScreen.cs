using Godot;
using System;

public partial class LobbyScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	[Export] public NodePath GameListPath;

	private ItemList _gameListUI;
	private Button _joinButton;
	private Button _spectateButton;

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

		// Récupération des boutons
		_joinButton = GetNodeOrNull<Button>("Root/Center/Card/CardMargin/MainLayout/Content/RightPanel/JoinButton");
		_spectateButton = GetNodeOrNull<Button>("Root/Center/Card/CardMargin/MainLayout/Content/RightPanel/SpectateButton");
		
		// Désactiver les boutons au démarrage
		UpdateButtonStates();

		if (_net != null)
		{
			GD.Print("[LobbyScreen] ✓ _net n'est pas null, souscription aux événements");
			_net.GameCreated += OnGameCreated;
			_net.JoinResultReceived += OnJoinResultReceived;

			if (_gameListUI is GameList gameListScript)
			{
				GD.Print("[LobbyScreen] ✓ Appel de gameListScript.SetGameServerClient(_net)");
				gameListScript.SetGameServerClient(_net);
				
				// S'abonner au signal de mise à jour de la liste
				gameListScript.GameListUpdated += OnGameListUpdated;
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

		if (_gameListUI != null)
		{
			GD.Print("[LobbyScreen] ✓ Connexion de l'événement ItemSelected");
			_gameListUI.ItemSelected += OnGameSelected;
		}
		else
		{
			GD.PrintErr("[LobbyScreen] ✗ ERREUR: _gameListUI est NULL, impossible de connecter ItemSelected!");
		}

		VisibilityChanged += OnVisibilityChanged;

		GD.Print("===== [LobbyScreen._Ready] TERMINÉ =====");
	}

	private void OnVisibilityChanged()
	{
		if (Visible)
		{
			GD.Print("[LobbyScreen] ✓ Lobby est devenu visible, rafraîchissement de la liste des games");
			OnRefreshPressed();
		}
	}

	private void OnGameListUpdated()
	{
		GD.Print("[LobbyScreen] Liste des parties mise à jour");
		UpdateButtonStates();
	}

	private void UpdateButtonStates()
	{
		bool hasGames = _net?.LastGameList != null && _net.LastGameList.Games.Count > 0;
		bool hasSelection = !string.IsNullOrEmpty(_selectedGameId);
		
		// Activer les boutons seulement si une partie est sélectionnée OU s'il y a au moins une partie
		bool enableButtons = hasGames;
		
		if (_joinButton != null)
		{
			_joinButton.Disabled = !enableButtons;
		}
		
		if (_spectateButton != null)
		{
			_spectateButton.Disabled = !enableButtons;
		}
		
		GD.Print($"[LobbyScreen] État des boutons: hasGames={hasGames}, hasSelection={hasSelection}, enabled={enableButtons}");
	}

	private void OnGameSelected(long index)
	{
		if (_net?.LastGameList != null && index >= 0 && index < _net.LastGameList.Games.Count)
		{
			_selectedGameId = _net.LastGameList.Games[(int)index].Id;
			GD.Print($"[LobbyScreen] Game sélectionnée: {_selectedGameId}");
			UpdateButtonStates();
		}
	}

	public override void _ExitTree()
	{
		if (_net != null)
		{
			_net.GameCreated -= OnGameCreated;
			_net.JoinResultReceived -= OnJoinResultReceived;
		}
		
		if (_gameListUI is GameList gameListScript)
		{
			gameListScript.GameListUpdated -= OnGameListUpdated;
		}
	}

	public void OnBackPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/main_menu_screen.tscn");
	}

	public async void OnCreatePressed()
	{
		if (_net == null) return;

		await _net.SendCreateGame(9, "Godot Lobby Game");
	}

	public async void OnJoinPressed()
	{
		if (_net == null)
			return;

		if (string.IsNullOrEmpty(_selectedGameId) && _net.LastGameList != null && _net.LastGameList.Games.Count > 0)
		{
			_selectedGameId = _net.LastGameList.Games[0].Id;
		}

		if (string.IsNullOrEmpty(_selectedGameId))
		{
			GD.PrintErr("[LobbyScreen] Aucune partie sélectionnée. Veuillez sélectionner ou créer une partie.");
			return;
		}

		await _net.SendJoinGame(_selectedGameId, asSpectator: false);
	}

	public async void OnSpectatePressed()
	{
		if (_net == null)
			return;

		if (string.IsNullOrEmpty(_selectedGameId) && _net.LastGameList != null && _net.LastGameList.Games.Count > 0)
		{
			_selectedGameId = _net.LastGameList.Games[0].Id;
		}

		if (string.IsNullOrEmpty(_selectedGameId))
		{
			GD.PrintErr("[LobbyScreen] Aucune partie disponible à observer. Veuillez créer ou attendre qu'une partie soit disponible.");
			return;
		}

		_net.IsJoiningAsSpectator = true;
		await _net.SendJoinGame(_selectedGameId, asSpectator: true);
	}

	private void OnGameCreated(string gameId)
	{
		_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	private void OnJoinResultReceived(string result)
	{

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

		}
	}

	public async void OnRefreshPressed()
	{
		if (_net == null)
			return;

		await _net.SendGetGameList();
	}
}
