using System;
using System.IO;

namespace GSoundPlayer.Licensing;

public sealed class FileSlot(string path) : IStoreSlot
{
	public string? Read()
	{
		try
		{
			return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return null;
		}
	}

	public void Write(string value)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			string sourceFileName = path + ".tmp";
			File.WriteAllText(sourceFileName, value);
			File.Move(sourceFileName, path, overwrite: true);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
	}
}
