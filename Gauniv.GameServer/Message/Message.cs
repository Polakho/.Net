/*
using Gauniv.GameServer.Model;
using MessagePack;

namespace Gauniv.GameServer.Message;

[MessagePackObject]
public class Message
{
    [Key(0)]
    public string Type { get; set; }
    
    [Key(1)]
    public byte[] Data { get; set; }
}

[MessagePackObject]
public class CreateGameRequest
{
    [Key(0)]
    public int BoardSize { get; set; } = 19;
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
public class PlayMoveRequest
{
    [Key(0)]
    public string GameId { get; set; }
    
    [Key(1)]
    public Point? Position { get; set; }
}
*/
