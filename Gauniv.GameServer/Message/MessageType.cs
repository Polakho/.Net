namespace Gauniv.GameServer.Message;

public static class MessageType
{
    public const string SetPlayerName = "SetPlayerName";
    public const string CreateGame = "CreateGame";
    public const string JoinGame = "JoinGame";
    public const string GetGameState = "GetGameState";
    public const string GameState = "GameState";
    public const string MakeMove = "MakeMove";
    public const string WrongMove = "WrongMove";
    public const string GetGameList = "GetGameList";
}