using MessagePack;
using System.Collections.Generic;
using System;

// Même enum que coté serveur
public static class MessageType
{
	public const string SetPlayerName = "SetPlayerName";
	public const string CreateGame    = "CreateGame";
	public const string JoinGame      = "JoinGame";
	public const string GetGameState  = "GetGameState";
	public const string GameState     = "GameState";
	public const string MakeMove      = "MakeMove";
	public const string WrongMove     = "WrongMove";
	public const string GetGameList   = "GetGameList";
}

[MessagePackObject]
public class MessageGeneric
{
	[Key(0)] public string Type { get; set; }
	[Key(1)] public byte[] Data { get; set; }
}

[MessagePackObject]
public class SetPlayerNameRequest
{
	[Key(0)] public string Name { get; set; }
}

[MessagePackObject]
public class CreateGameRequest
{
	[Key(0)] public int BoardSize { get; set; } = 19;
	[Key(1)] public string GameName { get; set; }
}

[MessagePackObject]
public class JoinGameRequest
{
	[Key(0)] public string GameId { get; set; }
	[Key(1)] public bool AsSpectator { get; set; }
}

[MessagePackObject]
public class GetGameStateRequest
{
	[Key(0)] public string GameId { get; set; }
}

[MessagePackObject]
public class GetGameStateResponse
{
	[Key(0)] public string GameId { get; set; }
	[Key(1)] public int BoardSize { get; set; }
	[Key(2)] public string currentPlayer { get; set; }
	[Key(3)] public StoneColor?[,] Board { get; set; }
}

// Enum local identique à celui du serveur
public enum StoneColor
{
	Black = 0,
	White = 1
}

[MessagePackObject]
public class GameInfo
{
	[Key(0)] public string Id { get; set; }
	[Key(1)] public string Name { get; set; }
	[Key(2)] public List<PlayerInfo> Players { get; set; }
	[Key(3)] public List<PlayerInfo> Spectators { get; set; }
	[Key(4)] public int State { get; set; }      // Game.GameState enum côté serveur
	[Key(5)] public int BoardSize { get; set; }
}

[MessagePackObject]
public class GetListGamesResponse
{
	[Key(0)] public List<GameInfo> Games { get; set; }
}

[MessagePackObject]
public class PlayerInfo
{
	[Key(0)] public string Id { get; set; }
	[Key(1)] public string Name { get; set; }
	[Key(2)] public bool IsSpectator { get; set; }
}

[MessagePackObject]
public class MakeMoveRequest
{
	[Key(0)] public string GameId { get; set; }
	[Key(1)] public int X { get; set; }
	[Key(2)] public int Y { get; set; }
	[Key(3)] public bool IsPass { get; set; }
}

[MessagePackObject]
public class WrongMoveResponse
{
	[Key(0)] public string Reason { get; set; }
}
