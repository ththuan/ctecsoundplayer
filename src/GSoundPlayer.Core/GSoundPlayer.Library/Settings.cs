using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GSoundPlayer.Library;

public sealed class Settings
{
	public string Theme { get; set; } = "dark";

	public bool Exclusive { get; set; }

	public bool ExclusiveChosen { get; set; }

	public string? DeviceName { get; set; }

	public double FadeLen { get; set; } = 3.0;

	public double Xfade { get; set; }

	public double FxFade { get; set; }

	public double MasterDb { get; set; }

	public bool AutoLoud { get; set; } = true;

	public bool AutoQueue { get; set; } = true;

	public bool QueueRemovePlayed { get; set; }

	public bool QueueLoop { get; set; } = true;

	public string? ZaloDir { get; set; }

	public List<string> WatchFolders { get; set; } = new List<string>();

	public static Settings Load()
	{
		try
		{
			if (File.Exists(AppPaths.SettingsFile))
			{
				return FromJson(File.ReadAllText(AppPaths.SettingsFile));
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is JsonException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
		return new Settings();
	}

	public static Settings FromJson(string json)
	{
		Settings settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
		if (!settings.ExclusiveChosen)
		{
			settings.Exclusive = false;
		}
		return settings;
	}

	public void Save()
	{
		try
		{
			string text = AppPaths.SettingsFile + ".tmp";
			File.WriteAllText(text, JsonSerializer.Serialize(this, new JsonSerializerOptions
			{
				WriteIndented = true
			}));
			File.Move(text, AppPaths.SettingsFile, overwrite: true);
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
	}
}
