using System;

namespace GSoundPlayer.Library;

public sealed class PlaylistException : Exception
{
	public PlaylistException(string message)
		: base(message)
	{
	}
}
