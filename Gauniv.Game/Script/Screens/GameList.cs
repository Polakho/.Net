using Godot;
using System;

public partial class GameList : ItemList
{
	private GameServerClient _net;

	// Le LobbyScreen appellera cette méthode pour nous passer le GameServerClient
	public void SetGameServerClient(GameServerClient net)
	{
		GD.Print("===== [GameList.SetGameServerClient] APPELÉE =====");
		GD.Print($"[GameList] net parameter est null? {net == null}");
		
		_net = net;
		
		if (_net != null)
		{
			GD.Print("[GameList] ✓ GameServerClient reçu, tentative de souscription à l'événement");
			
			// S'abonner à l'événement de réception de liste
			_net.GameListReceived += OnGameListReceived;
			GD.Print("[GameList] ✓ Événement GameListReceived souscrit");
			
			// Afficher la liste actuelle s'il y en a une déjà
			GD.Print($"[GameList] LastGameList est null? {_net.LastGameList == null}");
			if (_net.LastGameList != null)
			{
				GD.Print($"[GameList] LastGameList existe, contient {_net.LastGameList.Games.Count} games");
				DisplayGames(_net.LastGameList);
			}
			else
			{
				GD.Print("[GameList] ⚠ LastGameList est null - en attente d'une réception");
			}
			
			GD.Print("[GameList] ✓ Connecté au GameServerClient");
		}
		else
		{
			GD.PrintErr("[GameList] ✗ ERREUR: GameServerClient reçu est NULL!");
		}
	}

	public override void _Ready()
	{
		GD.Print("===== [GameList._Ready] APPELÉE =====");
		GD.Print($"[GameList] _net est null au Ready? {_net == null}");
		GD.Print("[GameList] En attente de l'appel de SetGameServerClient() depuis LobbyScreen");
	}

	public override void _ExitTree()
	{
		// Déabonner l'événement quand le nœud est supprimé
		if (_net != null)
		{
			GD.Print("[GameList] ✓ Nœud GameList supprimé, déabonnement de l'événement GameListReceived");
			_net.GameListReceived -= OnGameListReceived;
		}
	}

	private void OnGameListReceived(GetListGamesResponse gameList)
	{
		GD.Print("===== [GameList.OnGameListReceived] ÉVÉNEMENT DÉCLENCHÉ =====");
		GD.Print($"[GameList] gameList parameter est null? {gameList == null}");
		if (gameList != null)
		{
			GD.Print($"[GameList] gameList.Games est null? {gameList.Games == null}");
			if (gameList.Games != null)
			{
				GD.Print($"[GameList] Nombre de games reçues: {gameList.Games.Count}");
			}
		}
		DisplayGames(gameList);
	}

	private void DisplayGames(GetListGamesResponse gameList)
	{
		GD.Print("===== [GameList.DisplayGames] APPELÉE =====");
		
		// Vérifier si le nœud est encore valide avant d'accéder à ses propriétés
		if (!IsNodeReady() || IsQueuedForDeletion())
		{
			GD.PrintErr("[GameList] ✗ Le nœud GameList a été supprimé, arrêt de DisplayGames");
			return;
		}
		
		GD.Print($"[GameList] ItemList avant Clear() contient {ItemCount} items");
		
		Clear();
		
		GD.Print($"[GameList] ItemList après Clear() contient {ItemCount} items");

		if (gameList == null)
		{
			GD.PrintErr("[GameList] ✗ gameList est NULL!");
			AddItem("ERROR: gameList is null");
			GD.Print($"[GameList] ItemList après erreur contient {ItemCount} items");
			return;
		}

		if (gameList.Games == null)
		{
			GD.PrintErr("[GameList] ✗ gameList.Games est NULL!");
			AddItem("ERROR: gameList.Games is null");
			GD.Print($"[GameList] ItemList après erreur contient {ItemCount} items");
			return;
		}

		GD.Print($"[GameList] gameList.Games.Count = {gameList.Games.Count}");

		if (gameList.Games.Count == 0)
		{
			GD.Print("[GameList] Aucune game disponible");
			AddItem("No games available");
			GD.Print($"[GameList] ItemList après 'No games' contient {ItemCount} items");
			return;
		}

		int itemIndex = 0;
		foreach (var game in gameList.Games)
		{
			GD.Print($"[GameList] --- Affichage du game ---");
			GD.Print($"[GameList] Game ID: {game.Id}");
			GD.Print($"[GameList] Game Name: {game.Name}");
			GD.Print($"[GameList] Game.Players: {game.Players}");
			GD.Print($"[GameList] Game.Players.Count: {game.Players?.Count ?? -1}");
			if (game.Players != null)
			{
				foreach (var p in game.Players)
				{
					GD.Print($"[GameList]   - Player: {p.Name} ({p.Id})");
				}
			}
			GD.Print($"[GameList] Board Size: {game.BoardSize}");
			
			string displayText = $"[{game.Id}] {game.Name} ({game.Players.Count} players, {game.BoardSize}x{game.BoardSize})";
			AddItem(displayText);
			GD.Print($"[GameList] ✓ Item #{itemIndex} ajouté: {displayText}");
			GD.Print($"[GameList] ItemList contient maintenant {ItemCount} items");
			itemIndex++;
		}

		GD.Print($"[GameList] ✓ Affichage de {gameList.Games.Count} jeu(x) terminé");
		GD.Print($"[GameList] ItemList contient maintenant {ItemCount} items");
	}
}

