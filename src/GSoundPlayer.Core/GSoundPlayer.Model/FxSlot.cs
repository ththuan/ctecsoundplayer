namespace GSoundPlayer.Model;

public sealed class FxSlot
{
	public int Index;

	public string Name = "";

	public string Color = "green";

	public string? FilePath;

	public double Dur;

	public nint Sample;

	public int Voice;

	public double Pos;

	public bool Fading;

	public string Key => (Index + 1).ToString();

	public bool Playing => Voice != 0;

	public bool Loaded => Sample != 0;

	public bool Empty => string.IsNullOrEmpty(FilePath);
}
