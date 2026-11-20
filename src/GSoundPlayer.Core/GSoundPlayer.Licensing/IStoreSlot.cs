namespace GSoundPlayer.Licensing;

public interface IStoreSlot
{
	string? Read();

	void Write(string value);
}
