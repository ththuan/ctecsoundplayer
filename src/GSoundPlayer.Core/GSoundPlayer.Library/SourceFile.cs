using System.IO;

namespace GSoundPlayer.Library;

public sealed class SourceFile
{
	public required string Path;

	public long Size;

	public string FileName => System.IO.Path.GetFileName(Path);
}
