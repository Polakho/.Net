using Godot;
using System;

public partial class GameListRefreshService : Node
{
	[Export] public float RefreshIntervalSeconds = 5.0f;
	[Export] public bool AutoStartRefresh = true;

	private GameServerClient _gameServerClient;
	private Timer _refreshTimer;
	private bool _isRefreshing = false;

	public event Action<bool> RefreshStarted;
	public event Action<bool> RefreshStopped;

	public override void _Ready()
	{

		Node current = GetParent();
		while (current != null && _gameServerClient == null)
		{
			if (current.HasNode("NetClient"))
			{
				_gameServerClient = current.GetNode<GameServerClient>("NetClient");
				break;
			}
			current = current.GetParent();
		}

		if (_gameServerClient == null)
		{
			GD.PrintErr("[GameListRefreshService] GameServerClient non trouvé");
			return;
		}

		GD.Print("[GameListRefreshService] GameServerClient trouvé");

		_refreshTimer = new Timer();
		_refreshTimer.WaitTime = Mathf.Max(1.0f, RefreshIntervalSeconds);
		_refreshTimer.Timeout += OnRefreshTimerTimeout;
		AddChild(_refreshTimer);

		if (AutoStartRefresh)
		{
			StartRefresh();
		}
	}

	public override void _ExitTree()
	{
		StopRefresh();
		if (_refreshTimer != null)
		{
			_refreshTimer.QueueFree();
		}
	}

	public void StartRefresh()
	{
		if (_isRefreshing || _refreshTimer == null || _gameServerClient == null)
			return;

		_isRefreshing = true;
		_refreshTimer.Start();

		GD.Print($"[GameListRefreshService] Refresh démarré (intervalle: {RefreshIntervalSeconds}s)");
		RefreshStarted?.Invoke(true);

		_ = _gameServerClient.SendGetGameList();
	}

	public void StopRefresh()
	{
		if (!_isRefreshing || _refreshTimer == null)
			return;

		_isRefreshing = false;
		_refreshTimer.Stop();

		GD.Print("[GameListRefreshService] Refresh arrêté");
		RefreshStopped?.Invoke(true);
	}

	public void ToggleRefresh()
	{
		if (_isRefreshing)
			StopRefresh();
		else
			StartRefresh();
	}

	public async void ForceRefreshNow()
	{
		if (_gameServerClient == null)
		{
			GD.PrintErr("[GameListRefreshService] Impossible de rafraîchir: GameServerClient non disponible");
			return;
		}

		GD.Print("[GameListRefreshService] Refresh immédiat demandé");
		await _gameServerClient.SendGetGameList();
	}

	private async void OnRefreshTimerTimeout()
	{
		if (_gameServerClient != null)
		{
			await _gameServerClient.SendGetGameList();
		}
	}

	public bool IsRefreshing => _isRefreshing;

	public void SetRefreshInterval(float seconds)
	{
		RefreshIntervalSeconds = Mathf.Max(1.0f, seconds);
		if (_refreshTimer != null)
		{
			_refreshTimer.WaitTime = RefreshIntervalSeconds;
		}
	}
}

