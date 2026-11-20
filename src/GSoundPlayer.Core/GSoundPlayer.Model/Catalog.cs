using System;
using System.Collections.Generic;
using System.IO;

namespace GSoundPlayer.Model;

public static class Catalog
{
	public static readonly (string Key, string Label)[] Chips = new (string, string)[5]
	{
		("all", "Tất cả"),
		("none", "Chưa có nhóm"),
		("new", "Vừa nạp"),
		("fav", "Hay dùng"),
		("recent", "Vừa phát")
	};

	public const int MaxGroups = 12;

	public static readonly string[] GroupColors = new string[8] { "#2cc7b0", "#ec6aa0", "#f2b43a", "#4ea1ff", "#9b7bff", "#e5484d", "#12a150", "#7d8b85" };

	public static readonly Dictionary<string, string> FxColors = new Dictionary<string, string>
	{
		["green"] = "#12a150",
		["gold"] = "#f2b43a",
		["pink"] = "#ec6aa0",
		["blue"] = "#4ea1ff",
		["violet"] = "#9b7bff",
		["teal"] = "#2cc7b0",
		["red"] = "#e5484d",
		["gray"] = "#7d8b85"
	};

	public static readonly string[] AudioExtensions = new string[6] { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".wma" };

	public static readonly string[] VideoExtensions = new string[7] { ".mp4", ".m4v", ".mov", ".mkv", ".avi", ".wmv", ".3gp" };

	public static bool IsAudio(string path)
	{
		if (!AudioExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
		{
			return IsVideo(path);
		}
		return true;
	}

	public static bool IsVideo(string path)
	{
		return VideoExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
	}

	public static bool IsPlaylist(string path)
	{
		switch (Path.GetExtension(path).ToLowerInvariant())
		{
		case ".gsplayer":
		case ".m3u":
		case ".m3u8":
			return true;
		default:
			return false;
		}
	}

	public static bool IsPackage(string path)
	{
		return Path.GetExtension(path).Equals(".gsplayer", StringComparison.OrdinalIgnoreCase);
	}
}
