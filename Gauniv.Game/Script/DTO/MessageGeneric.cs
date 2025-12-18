using MessagePack;

[MessagePackObject]
public class MessageGeneric
{
	[Key(0)]
	public string Type { get; set; }

	[Key(1)]
	public byte[] Data { get; set; }
}
