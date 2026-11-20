using System;
using System.IO;

namespace GSoundPlayer.Library;

public static class AppPaths
{
	public static string Root { get; set; } = ResolveRoot();

	public static string Database => Path.Combine(Root, "kho_nhac.db");

	public static string SettingsFile => Path.Combine(Root, "tuy_chinh.json");

	public static string MusicDir => Path.Combine(Root, "Nhac da chep");

	public static string FxDir => Path.Combine(Root, "Hieu ung");

	public static bool IsInside(string? path, string dir)
	{
		if (string.IsNullOrEmpty(path))
		{
			return false;
		}
		try
		{
			string value = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
			return Path.GetFullPath(path).StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception ex) when ((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? true : false)
		{
			return false;
		}
	}

	private static string ResolveRoot()
	{
		string? overrideRoot = Environment.GetEnvironmentVariable("GSOUND_DATA");
		if (!string.IsNullOrWhiteSpace(overrideRoot))
		{
			return overrideRoot;
		}
		string portable = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
		if (CanWrite(portable))
		{
			return portable;
		}
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CTECSoundPlayer");
	}

	private static bool CanWrite(string dir)
	{
		try
		{
			Directory.CreateDirectory(dir);
			string probe = Path.Combine(dir, ".ctec_write_test");
			File.WriteAllText(probe, string.Empty);
			File.Delete(probe);
			return true;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException) ? true : false)
		{
			return false;
		}
	}

	public static void Ensure()
	{
		Directory.CreateDirectory(Root);
		DataSeeder.EnsureSeeded();
		Directory.CreateDirectory(MusicDir);
		Directory.CreateDirectory(FxDir);
		MigrateLegacy();
	}

	private static void MigrateLegacy()
	{
		try
		{
			string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GSoundPlayer");
			if (!Directory.Exists(legacy))
			{
				return;
			}
			string[] names = new string[3] { "kho_nhac.db", "tuy_chinh.json", "kich_ban.json" };
			foreach (string name in names)
			{
				string source = Path.Combine(legacy, name);
				string destination = Path.Combine(Root, name);
				if (File.Exists(source) && !File.Exists(destination))
				{
					File.Copy(source, destination);
				}
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
	}
}
