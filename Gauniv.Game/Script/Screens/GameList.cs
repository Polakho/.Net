using Godot;
using System;

public partial class GameList : ItemList
{
	private GameServerClient _net;

	[Signal]
	public delegate void GameListUpdatedEventHandler();

	public void SetGameServerClient(GameServerClient net)
	{
		GD.Print("===== [GameList.SetGameServerClient] APPEL�E =====");
		GD.Print($"[GameList] net parameter est null? {net == null}");
		
		_net = net;
		
		if (_net != null)
		{
			GD.Print("[GameList] ? GameServerClient re�u, tentative de souscription � l'�v�nement");

			_net.GameListReceived += OnGameListReceived;
			GD.Print("[GameList] ? �v�nement GameListReceived souscrit");

			GD.Print($"[GameList] LastGameList est null? {_net.LastGameList == null}");
			if (_net.LastGameList != null)
			{
				GD.Print($"[GameList] LastGameList existe, contient {_net.LastGameList.Games.Count} games");
				DisplayGames(_net.LastGameList);
			}
			else
			{
				GD.Print("[GameList] ? LastGameList est null - en attente d'une r�ception");
			}
			
			GD.Print("[GameList] ? Connect� au GameServerClient");
		}
		else
		{
			GD.PrintErr("[GameList] ? ERREUR: GameServerClient re�u est NULL!");
		}
	}

	public override void _Ready()
	{
		GD.Print("===== [GameList._Ready] APPEL�E =====");
		GD.Print($"[GameList] _net est null au Ready? {_net == null}");
		GD.Print("[GameList] En attente de l'appel de SetGameServerClient() depuis LobbyScreen");
	}

	public override void _ExitTree()
	{

		if (_net != null)
		{
			GD.Print("[GameList] ? N�ud GameList supprim�, d�abonnement de l'�v�nement GameListReceived");
			_net.GameListReceived -= OnGameListReceived;
		}
	}

	private void OnGameListReceived(GetListGamesResponse gameList)
	{
		GD.Print("===== [GameList.OnGameListReceived] �V�NEMENT D�CLENCH� =====");
		GD.Print($"[GameList] gameList parameter est null? {gameList == null}");
		if (gameList != null)
		{
			GD.Print($"[GameList] gameList.Games est null? {gameList.Games == null}");
			if (gameList.Games != null)
			{
				GD.Print($"[GameList] Nombre de games re�ues: {gameList.Games.Count}");
			}
		}
		DisplayGames(gameList);
	}

	private void DisplayGames(GetListGamesResponse gameList)
	{

		if (!IsNodeReady() || IsQueuedForDeletion())
		{
			GD.PrintErr("[GameList] Le n�ud GameList a �t� supprim�, arr�t de DisplayGames");
			return;
		}
		
		Clear();

		if (gameList == null)
		{
			GD.PrintErr("[GameList] gameList est NULL!");
			AddItem("ERROR: gameList is null");
			EmitSignal(SignalName.GameListUpdated);
			return;
		}

		if (gameList.Games == null)
		{
			GD.PrintErr("[GameList] gameList.Games est NULL!");
			AddItem("ERROR: gameList.Games is null");
			EmitSignal(SignalName.GameListUpdated);
			return;
		}

		if (gameList.Games.Count == 0)
		{
			AddItem("No games available");
			EmitSignal(SignalName.GameListUpdated);
			return;
		}

		foreach (var game in gameList.Games)
		{
			int playerCount = game.Players?.Count ?? 0;
			int spectatorCount = game.Spectators?.Count ?? 0;
			string statusText = playerCount >= 2 ? "[COMPLET]" : $"[{playerCount}/2]";
			string spectatorText = spectatorCount > 0 ? $" ({spectatorCount} spectateur{(spectatorCount > 1 ? "s" : "")})" : "";
			string displayText = $"{statusText} {game.Name} - {game.BoardSize}x{game.BoardSize}{spectatorText}";
			
			AddItem(displayText);
		}
		
		// Notifier que la liste a été mise à jour
		EmitSignal(SignalName.GameListUpdated);
	}
}

