namespace GSoundPlayer.Library;

public sealed class PlaylistItem
{
	public string Name = "";

	public string File = "";

	public string? Entry;

	public string? ResolvedPath;

	public string Hash = "";

	public double Dur;

	public double InP;

	public double FadeIn;

	public double FadeOut;

	public double Vol;

	public double? OutP;

	public int Warn = 10;

	public bool Loud = true;

	public double? Lufs;

	public string Note = "";

	public bool AutoTrim;

	public bool Plain;
}
