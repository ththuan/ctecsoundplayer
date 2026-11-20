using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using GSoundPlayer.Library;

namespace GSoundPlayer.Cues;

public sealed class CueItem
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
	public string Title { get; set; } = "";
	public long TrackId { get; set; }
	public string TrackName { get; set; } = "";
	public string Note { get; set; } = "";
	public bool Follow { get; set; }
}

public sealed class CueShow
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
	public string Title { get; set; } = "Sự kiện chính";
	public List<CueItem> Cues { get; set; } = new List<CueItem>();

	public override string ToString()
	{
		return Title;
	}
}

public sealed class CueLibrary
{
	public int Version { get; set; } = 1;
	public string CurrentShowId { get; set; } = "";
	public int MasterMinutes { get; set; } = 5;
	public List<CueShow> Shows { get; set; } = new List<CueShow>();

	public static CueLibrary CreateDefault()
	{
		CueShow show = new CueShow
		{
			Title = "Sự kiện chính"
		};
		return new CueLibrary
		{
			CurrentShowId = show.Id,
			Shows = new List<CueShow> { show }
		};
	}
}

public static class CueStore
{
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		WriteIndented = true,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public static string FilePath => Path.Combine(AppPaths.Root, "kich_ban.json");

	public static CueLibrary Load()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				CueLibrary library = JsonSerializer.Deserialize<CueLibrary>(File.ReadAllText(FilePath), Options);
				if (library != null && library.Shows.Count > 0)
				{
					library.Shows.RemoveAll(s => s == null);
					foreach (CueShow show in library.Shows)
					{
						show.Cues ??= new List<CueItem>();
					}
					if (library.Shows.Count > 0 && library.Shows.Find(s => s.Id == library.CurrentShowId) == null)
					{
						library.CurrentShowId = library.Shows[0].Id;
					}
					return library;
				}
			}
		}
		catch
		{
		}
		return CueLibrary.CreateDefault();
	}

	public static void Save(CueLibrary library)
	{
		try
		{
			Directory.CreateDirectory(AppPaths.Root);
			string temp = FilePath + ".tmp";
			File.WriteAllText(temp, JsonSerializer.Serialize(library, Options));
			File.Move(temp, FilePath, overwrite: true);
		}
		catch
		{
		}
	}
}
