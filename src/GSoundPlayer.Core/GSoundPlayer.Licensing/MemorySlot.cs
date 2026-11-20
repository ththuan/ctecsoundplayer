namespace GSoundPlayer.Licensing;

public sealed class MemorySlot : IStoreSlot
{
	public string? Value { get; set; }

	public string? Read()
	{
		return Value;
	}

	public void Write(string value)
	{
		Value = value;
	}
}
