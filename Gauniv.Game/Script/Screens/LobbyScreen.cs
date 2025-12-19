using Godot;
using System;

public partial class LobbyScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	// Renseigne ces NodePath dans l'Inspecteur
	[Export] public NodePath GameListPath;
	[Export] public NodePath StatusLabelPath;

<<<<<<< Updated upstream
	private Label _statusLabel;
	private ItemList _gameListUI;
=======
	private ItemList _gameList;
>>>>>>> Stashed changes

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

<<<<<<< Updated upstream
		_statusLabel = GetNode<Label>("Root/Center/Card/CardMargin/MainLayout/Footer/StatusLabel");
		GD.Print($"[LobbyScreen] StatusLabel trouvé? {_statusLabel != null}");

		_gameListUI = GetNode<ItemList>(GameListPath);
		GD.Print($"[LobbyScreen] GameListUI trouvé via NodePath '{GameListPath}'? {_gameListUI != null}");
		GD.Print($"[LobbyScreen] GameListUI est de type GameList? {_gameListUI is GameList}");
=======
		// Initialiser la liste de jeux depuis le NodePath exporté
		if (!GameListPath.IsEmpty)
		{
			_gameList = GetNode<ItemList>(GameListPath);
		}
>>>>>>> Stashed changes

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

		GD.Print("===== [LobbyScreen._Ready] TERMINÉ =====");
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
			_net.GameListReceived -= OnGameListReceived;
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
		await _net.SendJoinGame(_selectedGameId, asSpectator: true);
		_screenManager.GoTo("res://Scenes/Screens/spectate_screen.tscn");
	}

	// --- Callback de la liste (signal Godot) ---
<<<<<<< Updated upstream

	// --- Events réseau ---
=======
	public void OnGameSelected(long index)
	{
		if (_gameList == null) return;

		// On a stocké l'ID dans Metadata
		_selectedGameId = _gameList.GetItemMetadata((int)index).AsString();
	}

	// --- Events réseau ---
	private void OnGameListReceived(GetListGamesResponse list)
	{
		if (_gameList == null) return;

		_gameList.Clear();
		foreach (var game in list.Games)
		{
			int idx = _gameList.AddItem($"{game.Name} ({game.Players.Count}p)");
			_gameList.SetItemMetadata(idx, Variant.From(game.Id));
		}
		
	}
>>>>>>> Stashed changes

	private void OnGameCreated(string gameId)
	{
		_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	private void OnJoinResultReceived(string result)
	{
		// Si le join est OK, on va en match (tu peux affiner plus tard)
		if (result.Contains("success", StringComparison.OrdinalIgnoreCase))
			_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	public async void OnRefreshPressed()
	{
		if (_net == null)
			return;

		GD.Print("[LobbyScreen] Rafraîchissement de la liste des parties...");
		await _net.SendGetGameList();
	}
}
