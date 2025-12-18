using Godot;
using System;

public partial class LobbyScreen : Control
{
	private ScreenManager _screenManager;
	private GameServerClient _net;

	// Renseigne ces NodePath dans l'Inspecteur
	[Export] public NodePath StatusLabelPath;
	[Export] public NodePath GameListPath;

	private Label _statusLabel;
	private ItemList _gameList;

	private string _selectedGameId;

public override void _Ready()
	{
		_screenManager = GetParent<ScreenManager>();

		var main = _screenManager.GetParent();
		if (main != null && main.HasNode("NetClient"))
		{
			_net = main.GetNode<GameServerClient>("NetClient");
		}

		_statusLabel = GetNode<Label>("Root/Center/Card/CardMargin/MainLayout/Footer/StatusLabel");

		if (_net != null)
		{
			_net.GameListReceived += OnGameListReceived;
			_net.GameCreated += OnGameCreated;
			_net.JoinResultReceived += OnJoinResultReceived;
		}
	}

	public void OnBackPressed()
	{
		_screenManager.GoTo("res://Scenes/Screens/main_menu_screen.tscn");
	}

	public async void OnCreatePressed()
	{
		if (_net == null) return;

		_statusLabel.Text = "Status: Creating game...";
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

		if (string.IsNullOrEmpty(_selectedGameId))
		{
			if (_statusLabel != null)
				_statusLabel.Text = "Status: select a game first";
			return;
		}

		if (_statusLabel != null)
			_statusLabel.Text = $"Status: Joining game {_selectedGameId}...";

		await _net.SendJoinGame(_selectedGameId, asSpectator: false);
	}


	public async void OnSpectatePressed()
	{
		if (_net == null || string.IsNullOrEmpty(_selectedGameId))
		{
			if (_statusLabel != null)
				_statusLabel.Text = "Status: select a game first";
			return;
		}

		_statusLabel.Text = $"Status: Spectating game {_selectedGameId}...";
		await _net.SendJoinGame(_selectedGameId, asSpectator: true);
		_screenManager.GoTo("res://Scenes/Screens/spectate_screen.tscn");
	}

	// --- Callback de la liste (signal Godot) ---
	public void OnGameSelected(long index)
	{
		if (_gameList == null) return;

		// On a stocké l'ID dans Metadata
		_selectedGameId = _gameList.GetItemMetadata((int)index).AsString();
		if (_statusLabel != null)
			_statusLabel.Text = $"Status: Selected game: {_selectedGameId}";
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

		if (_statusLabel != null)
			_statusLabel.Text = $"Status: {list.Games.Count} games available";
	}

	private void OnGameCreated(string gameId)
	{
		if (_statusLabel != null)
			_statusLabel.Text = $"Status: Game created ({gameId})";

		_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}

	private void OnJoinResultReceived(string result)
	{
		if (_statusLabel != null)
			_statusLabel.Text = $"Status: {result}";

		// Si le join est OK, on va en match (tu peux affiner plus tard)
		if (result.Contains("success", StringComparison.OrdinalIgnoreCase))
			_screenManager.GoTo("res://Scenes/Screens/match_screen.tscn");
	}
}
