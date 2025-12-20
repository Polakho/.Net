﻿using Gauniv.GameServer.Model;
using MessagePack;

namespace Gauniv.GameServer.Message;

[MessagePackObject]
public class MessageGeneric
{
    [Key(0)]
    public string Type { get; set; }
    
    [Key(1)]
    public byte[] Data { get; set; }
}

[MessagePackObject]
public class SetPlayerNameRequest
{
    [Key(0)]
    public string Name { get; set; }
}

[MessagePackObject]
public class GameInfo
{
    [Key(0)]
    public string Id { get; set; }

    [Key(1)]
    public string Name { get; set; }

    [Key(2)]
    public List<PlayerInfo> Players { get; set; }

    [Key(3)]
    public List<PlayerInfo> Spectators { get; set; }

    [Key(4)]
    public Game.GameState State { get; set; }
    
    [Key(5)]
    public int BoardSize { get; set; }
}

[MessagePackObject]
public class GetListGamesResponse
{
    [Key(0)]
    public List<GameInfo> Games { get; set; }
}

[MessagePackObject]
public class CreateGameRequest
{
    [Key(0)]
    public int BoardSize { get; set; } = 19;
    [Key(1)]
    public string GameName { get; set; }
}

[MessagePackObject]
public class JoinGameRequest
{
    [Key(0)]
    public string GameId { get; set; }
    
    [Key(1)]
    public bool AsSpectator { get; set; }
}

[MessagePackObject]
public class LeaveGameRequest
{
    [Key(0)]
    public string GameId { get; set; }
}

[MessagePackObject]
public class LeaveGameResponse
{
    [Key(0)]
    public string GameId { get; set; }
    
    [Key(1)]
    public string Result { get; set; }
}

[MessagePackObject]
public class JoinGameResponse
{
    [Key(0)]
    public string GameId { get; set; }
    
    [Key(1)]
    public string Result { get; set; }
    
    [Key(2)]
    public GetGameStateResponse GameState { get; set; }

    [Key(3)]
    public string YourPlayerId { get; set; }
}

[MessagePackObject]
public class GetGameStateRequest 
{
    [Key(0)]
    public string GameId { get; set; }
}

[MessagePackObject]
public class GetGameStateResponse
{
    [Key(0)]
    public string GameId { get; set; }
    
    [Key(1)]
    public int BoardSize { get; set; }
    
    [Key(2)]
    public String currentPlayer { get; set; }
    
    [Key(3)]
    public StoneColor?[,] Board { get; set; }

    [Key(4)]
    public string GameState { get; set; }

    [Key(5)]
    public int PlayerCount { get; set; }
}

[MessagePackObject]
public class PlayerInfo
{
    [Key(0)]
    public string Id { get; set; }
    
    [Key(1)]
    public string Name { get; set; }
    
    [Key(2)]
    public bool IsSpectator { get; set; }
}

[MessagePackObject]
public class MakeMoveRequest
{
    [Key(0)]
    public string GameId { get; set; }
    
    [Key(1)]
    public int X { get; set; }
    
    [Key(2)]
    public int Y { get; set; }

    [Key(3)] public bool IsPass { get; set; }
}

[MessagePackObject]
public class WrongMoveResponse
{
    [Key(0)]
    public string Reason { get; set; }
}