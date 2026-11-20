using System;

namespace GSoundPlayer.Licensing;

public sealed class FuncSlot(Func<string?> read, Action<string> write) : IStoreSlot
{
	public string? Read()
	{
		return read();
	}

	public void Write(string value)
	{
		write(value);
	}
}
