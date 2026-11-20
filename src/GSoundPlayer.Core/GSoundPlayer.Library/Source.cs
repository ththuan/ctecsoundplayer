using System.Collections.Generic;

namespace GSoundPlayer.Library;

public sealed class Source
{
	public required string Key;

	public required string Title;

	public required string Where;

	public bool CopyByDefault = true;

	public List<SourceFile> Files = new List<SourceFile>();
}
