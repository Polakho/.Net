using MessagePack;

[MessagePackObject]
public class JoinGameRequest
{
	[Key(0)]
	public string GameId { get; set; }

	[Key(1)]
	public bool AsSpectator { get; set; }
}
