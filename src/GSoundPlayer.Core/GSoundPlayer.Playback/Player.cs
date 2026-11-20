using GSoundPlayer.Model;

namespace GSoundPlayer.Playback;

public sealed class Player
{
	public required Track Item;

	public int Voice;

	public double Pos;

	public double InP;

	public double OutP;

	public double FadeIn;

	public double FadeOut;

	public string Name = "";

	public int Warn;

	public bool Paused;

	public bool JackPaused;

	public FadeReason Fade;

	public double Gain = 1.0;

	public double FadeRemain;

	public bool? Loop;

	public bool AutoFired;

	public double StartedAt;

	public bool Fading => Fade != FadeReason.None;

	public bool LoopOn => Loop == true;
}
