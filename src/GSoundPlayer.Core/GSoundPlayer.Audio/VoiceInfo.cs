namespace GSoundPlayer.Audio;

public struct VoiceInfo
{
	public double PosS;

	public double DurS;

	public double Gain;

	public double FadeRemainS;

	public VoiceState State;

	public EndReason End;

	public int FadingDown;

	public int FollowFired;

	public int Loop;

	public int Error;
}
