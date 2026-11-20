using System;
using System.Collections.Generic;

namespace GSoundPlayer.Library;

public sealed class Playlist
{
	public string Name = "";

	public PlaylistGroup? Group;

	public string SourcePath = "";

	public bool IsPackage;

	public DateTime? Exported;

	public List<PlaylistItem> Items = new List<PlaylistItem>();

	public List<PlaylistFx>? Effects;
}
