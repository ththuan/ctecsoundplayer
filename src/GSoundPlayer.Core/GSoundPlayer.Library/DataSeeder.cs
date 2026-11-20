using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Data.Sqlite;

namespace GSoundPlayer.Library;

internal static class DataSeeder
{
	private const string ResourceName = "du_lieu.zip";

	private const string MarkerFile = "du_lieu.seed";

	private const string SeedVersion = "2";

	private const string RootToken = "{ROOT}";

	public static void EnsureSeeded()
	{
		try
		{
			RenameLegacyData();
			string marker = Path.Combine(AppPaths.Root, MarkerFile);
			if (File.Exists(marker) && File.ReadAllText(marker).Trim() == SeedVersion)
			{
				return;
			}
			Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(DataSeeder).Assembly;
			using Stream? payload = assembly.GetManifestResourceStream(ResourceName);
			if (payload == null)
			{
				return;
			}
			string zipPath = Path.Combine(Path.GetTempPath(), "ctec-du-lieu-" + Guid.NewGuid().ToString("N") + ".zip");
			try
			{
				using (FileStream file = File.Create(zipPath))
				{
					payload.CopyTo(file);
				}
				using (ZipArchive archive = ZipFile.OpenRead(zipPath))
				{
					string root = Path.GetFullPath(AppPaths.Root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
					foreach (ZipArchiveEntry entry in archive.Entries)
					{
						if (string.IsNullOrEmpty(entry.Name))
						{
							continue;
						}
						string target = Path.GetFullPath(Path.Combine(root, entry.FullName));
						if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string? dir = Path.GetDirectoryName(target);
						if (dir != null)
						{
							Directory.CreateDirectory(dir);
						}
						if (!File.Exists(target))
						{
							entry.ExtractToFile(target, overwrite: false);
						}
					}
				}
				RewritePaths();
				File.WriteAllText(marker, SeedVersion);
			}
			finally
			{
				try
				{
					File.Delete(zipPath);
				}
				catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
				{
				}
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException) ? true : false)
		{
		}
	}

	private static void RenameLegacyData()
	{
		try
		{
			string oldDir = Path.Combine(AppPaths.Root, "Nhac da chep", "T-Sound");
			string newDir = Path.Combine(AppPaths.Root, "Nhac da chep", "Nhac su kien");
			if (Directory.Exists(oldDir) && !Directory.Exists(newDir))
			{
				Directory.Move(oldDir, newDir);
			}
			string oldCatalog = Path.Combine(AppPaths.Root, "tsound-catalog.json");
			string newCatalog = Path.Combine(AppPaths.Root, "danh_muc_mau.json");
			if (File.Exists(oldCatalog) && !File.Exists(newCatalog))
			{
				File.Move(oldCatalog, newCatalog);
			}
			if (!File.Exists(AppPaths.Database))
			{
				return;
			}
			using SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
			{
				DataSource = AppPaths.Database,
				Pooling = false
			}.ToString());
			sqliteConnection.Open();
			using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
			sqliteCommand.CommandText = "UPDATE tracks SET path=replace(path,$old,$new) WHERE instr(path,$old)>0;\r\nUPDATE kv SET value=replace(value,'TS · ','CTEC · ') WHERE key='nhom';";
			sqliteCommand.Parameters.AddWithValue("$old", Path.Combine(Path.DirectorySeparatorChar.ToString(), "Nhac da chep", "T-Sound", ""));
			sqliteCommand.Parameters.AddWithValue("$new", Path.Combine(Path.DirectorySeparatorChar.ToString(), "Nhac da chep", "Nhac su kien", ""));
			sqliteCommand.ExecuteNonQuery();
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is SqliteException) ? true : false)
		{
		}
	}

	private static void RewritePaths()
	{
		string database = AppPaths.Database;
		if (File.Exists(database))
		{
			using SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
			{
				DataSource = database,
				Pooling = false
			}.ToString());
			sqliteConnection.Open();
			using SqliteCommand sqliteCommand = sqliteConnection.CreateCommand();
			sqliteCommand.CommandText = "UPDATE tracks SET path=replace(path,$t,$r) WHERE instr(path,$t)>0;\r\nUPDATE tracks SET watch_root=replace(watch_root,$t,$r) WHERE watch_root IS NOT NULL AND instr(watch_root,$t)>0;";
			sqliteCommand.Parameters.AddWithValue("$t", RootToken);
			sqliteCommand.Parameters.AddWithValue("$r", AppPaths.Root);
			sqliteCommand.ExecuteNonQuery();
		}
		string credits = Path.Combine(AppPaths.Root, "nhac_mien_phi_nguon.json");
		if (File.Exists(credits))
		{
			string text = File.ReadAllText(credits);
			if (text.Contains(RootToken, StringComparison.Ordinal))
			{
				File.WriteAllText(credits, text.Replace(RootToken, AppPaths.Root));
			}
		}
	}
}
