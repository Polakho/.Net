using MessagePack;

[MessagePackObject]
public class CreateGameRequest
{
	[Key(0)]
	public int BoardSize { get; set; }
}
