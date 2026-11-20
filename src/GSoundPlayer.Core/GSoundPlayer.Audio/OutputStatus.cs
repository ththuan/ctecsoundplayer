namespace GSoundPlayer.Audio;

public struct OutputStatus
{
	public OutputState State;

	public int Exclusive;

	public int ExclusiveFailed;

	public int SampleRate;

	public int DeviceEvents;

	public int Underruns;
}
