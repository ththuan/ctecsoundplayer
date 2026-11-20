using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using GSoundPlayer.Model;

namespace GSoundPlayer.Library;

public static class PlaylistIO
{
	public sealed record ExportEntry(Track Track);

	public sealed record Progress(int Done, int Total, string Text);

	public const int Format = 1;

	private const string App = "CTECSoundPlayer";

	private const string Manifest = "playlist.json";

	public static long EstimateBytes(IEnumerable<Track> tracks, IEnumerable<FxSlot> fx)
	{
		long num = 0L;
		foreach (string item in tracks.Select((Track t) => t.FilePath).Concat(from e in fx
			where !e.Empty
			select e.FilePath).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				num += new FileInfo(item).Length;
			}
			catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
			{
			}
		}
		return num;
	}

	public static void Export(string destPath, string name, IReadOnlyList<Track> tracks, IReadOnlyList<FxSlot> fx, IProgress<Progress>? progress, CancellationToken ct, PlaylistGroup? group = null)
	{
		string text = destPath + ".tmp";
		try
		{
			List<Track> list = tracks.DistinctBy((Track t) => t.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
			List<FxSlot> list2 = fx.Where((FxSlot e) => !e.Empty && File.Exists(e.FilePath)).ToList();
			int total = list.Count + list2.Count;
			int num = 0;
			using (FileStream stream = new FileStream(text, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1048576))
			{
				using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
				Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				for (int num2 = 0; num2 < list.Count; num2++)
				{
					ct.ThrowIfCancellationRequested();
					string text2 = $"music/{num2 + 1:000000}{Path.GetExtension(list[num2].FilePath).ToLowerInvariant()}";
					map[list[num2].FilePath] = text2;
					CopyIn(zipArchive, list[num2].FilePath, text2, ct);
					progress?.Report(new Progress(++num, total, $"Đang chép bài {num2 + 1}/{list.Count}"));
				}
				Dictionary<FxSlot, string> fxEntries = new Dictionary<FxSlot, string>();
				for (int num3 = 0; num3 < list2.Count; num3++)
				{
					ct.ThrowIfCancellationRequested();
					string text3 = $"fx/{list2[num3].Index + 1:00}{Path.GetExtension(list2[num3].FilePath).ToLowerInvariant()}";
					fxEntries[list2[num3]] = text3;
					CopyIn(zipArchive, list2[num3].FilePath, text3, ct);
					progress?.Report(new Progress(++num, total, $"Đang chép hiệu ứng {num3 + 1}/{list2.Count}"));
				}
				JsonObject jsonObject = new JsonObject
				{
					["app"] = "CTECSoundPlayer",
					["format"] = 1,
					["kind"] = "queue",
					["name"] = name,
					["exported"] = DateTime.Now.ToString("o"),
					["withFiles"] = true,
					["items"] = new JsonArray(((IEnumerable<Track>)tracks).Select((Func<Track, JsonNode>)((Track t) => ItemJson(t, map[t.FilePath]))).ToArray()),
					["effects"] = new JsonArray(fx.Where((FxSlot e) => !e.Empty).Select((Func<FxSlot, JsonNode>)((FxSlot e) => new JsonObject
					{
						["key"] = e.Key,
						["name"] = e.Name,
						["dur"] = e.Dur,
						["color"] = e.Color,
						["entry"] = fxEntries.GetValueOrDefault(e)
					})).ToArray())
				};
				if (group != null)
				{
					jsonObject["group"] = new JsonObject
					{
						["name"] = group.Name,
						["color"] = group.Color
					};
				}
				using Stream stream2 = zipArchive.CreateEntry("playlist.json", CompressionLevel.NoCompression).Open();
				stream2.Write(Encoding.UTF8.GetBytes(jsonObject.ToJsonString(new JsonSerializerOptions
				{
					WriteIndented = true
				})));
			}
			File.Move(text, destPath, overwrite: true);
		}
		finally
		{
			try
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
			catch (IOException)
			{
			}
		}
	}

	private static JsonObject ItemJson(Track t, string entry)
	{
		JsonObject jsonObject = new JsonObject
		{
			["type"] = "file",
			["name"] = t.Name,
			["file"] = t.FileName,
			["entry"] = entry,
			["hash"] = t.QuickHash,
			["dur"] = t.Dur,
			["lufs"] = t.Lufs,
			["inP"] = t.InP,
			["outP"] = t.Out,
			["fadeIn"] = t.FadeIn,
			["fadeOut"] = t.FadeOut,
			["vol"] = t.Vol,
			["warn"] = t.Warn,
			["loud"] = t.Loud,
			["note"] = t.Note
		};
		if (t.AutoTrim && t.InP > 0.0)
		{
			jsonObject["autoTrim"] = true;
		}
		return jsonObject;
	}

	private static void CopyIn(ZipArchive zip, string path, string entry, CancellationToken ct)
	{
		ZipArchiveEntry zipArchiveEntry = zip.CreateEntry(entry, CompressionLevel.NoCompression);
		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1048576);
		using Stream stream = zipArchiveEntry.Open();
		byte[] array = new byte[1048576];
		int count;
		while ((count = fileStream.Read(array, 0, array.Length)) > 0)
		{
			ct.ThrowIfCancellationRequested();
			stream.Write(array, 0, count);
		}
	}

	private static double Num(JsonNode? n, double d, double lo, double hi)
	{
		if (n is JsonValue jsonValue)
		{
			if (jsonValue.TryGetValue<double>(out var value) && double.IsFinite(value))
			{
				return Math.Clamp(value, lo, hi);
			}
			if (jsonValue.TryGetValue<long>(out var value2))
			{
				return Math.Clamp(value2, lo, hi);
			}
		}
		return d;
	}

	private static string Str(JsonNode? n, int max)
	{
		string text = ((n is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value)) ? (value ?? "") : "");
		if (text.Length <= max)
		{
			return text;
		}
		return text.Substring(0, max);
	}

	private static PlaylistItem? NormItem(JsonNode? x)
	{
		if (!(x is JsonObject jsonObject))
		{
			return null;
		}
		string text = Str(jsonObject["type"], 16);
		if ((text == "fadeall" || text == "playlist") ? true : false)
		{
			return null;
		}
		string text2 = Str(jsonObject["file"], 260);
		string text3 = Str(jsonObject["name"], 200);
		if (text2.Length == 0 && text3.Length == 0)
		{
			return null;
		}
		if (text2.Length == 0)
		{
			text2 = text3;
		}
		text2 = Path.GetFileName(text2.Replace('\\', '/'));
		PlaylistItem playlistItem = new PlaylistItem
		{
			Name = ((text3.Length > 0) ? text3 : Path.GetFileNameWithoutExtension(text2)),
			File = text2,
			Entry = Str(jsonObject["entry"], 64),
			Hash = Str(jsonObject["hash"], 64),
			Dur = Num(jsonObject["dur"], 0.0, 0.0, 86400.0)
		};
		double num = ((playlistItem.Dur > 0.0) ? playlistItem.Dur : 86400.0);
		playlistItem.InP = Num(jsonObject["inP"], 0.0, 0.0, Math.Max(0.0, num - 1.0));
		if (jsonObject["outP"] != null)
		{
			double num2 = Num(jsonObject["outP"], num, Math.Min(playlistItem.InP + 0.1, num), num);
			playlistItem.OutP = ((playlistItem.Dur > 0.0 && num2 >= playlistItem.Dur - 0.05) ? ((double?)null) : new double?(num2));
		}
		playlistItem.FadeIn = Num(jsonObject["fadeIn"], 0.0, 0.0, 30.0);
		playlistItem.FadeOut = Num(jsonObject["fadeOut"], 0.0, 0.0, 30.0);
		playlistItem.Vol = Num(jsonObject["vol"], 0.0, -40.0, 12.0);
		playlistItem.Warn = (int)Num(jsonObject["warn"], 10.0, 5.0, 60.0);
		playlistItem.Note = Str(jsonObject["note"], 500);
		if (jsonObject["loud"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var value))
		{
			playlistItem.Loud = value;
		}
		if (jsonObject["autoTrim"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<bool>(out var value2))
		{
			playlistItem.AutoTrim = value2 && playlistItem.InP > 0.0;
		}
		if (jsonObject["lufs"] != null)
		{
			playlistItem.Lufs = Num(jsonObject["lufs"], -16.0, -70.0, 6.0);
		}
		return playlistItem;
	}

	private static List<PlaylistFx>? NormFx(JsonNode? a)
	{
		if (!(a is JsonArray source))
		{
			return null;
		}
		List<PlaylistFx> list = new List<PlaylistFx>();
		foreach (JsonNode item in source.Take(9))
		{
			if (item is JsonObject jsonObject)
			{
				string text = Str(jsonObject["name"], 40);
				if (text.Length != 0)
				{
					string text2 = Str(jsonObject["color"], 16);
					list.Add(new PlaylistFx
					{
						Name = text,
						Dur = Num(jsonObject["dur"], 3.0, 0.1, 600.0),
						Color = (Catalog.FxColors.ContainsKey(text2) ? text2 : "green"),
						Entry = Str(jsonObject["entry"], 64)
					});
				}
			}
		}
		if (list.Count <= 0)
		{
			return null;
		}
		return list;
	}

	private static PlaylistGroup? NormGroup(JsonNode? n)
	{
		if (!(n is JsonObject jsonObject))
		{
			return null;
		}
		string text = Str(jsonObject["name"], 60).Trim();
		if (text.Length == 0)
		{
			return null;
		}
		string text2 = Str(jsonObject["color"], 16);
		bool flag = text2.Length == 7 && text2[0] == '#' && text2.Substring(1).All(Uri.IsHexDigit);
		return new PlaylistGroup(text, flag ? text2.ToLowerInvariant() : Catalog.GroupColors[0]);
	}

	public static Playlist ReadPackage(string path)
	{
		string fileName = Path.GetFileName(path);
		try
		{
			using ZipArchive zipArchive = ZipFile.OpenRead(path);
			ZipArchiveEntry? zipArchiveEntry = zipArchive.GetEntry("playlist.json") ?? throw new PlaylistException("File “" + fileName + "” không phải danh sách của CTECSoundPlayer");
			if (zipArchiveEntry.Length > 32000000)
			{
				throw new PlaylistException("File “" + fileName + "” bị hỏng");
			}
			JsonNode jsonNode;
			using (Stream utf8Json = zipArchiveEntry.Open())
			{
				jsonNode = JsonNode.Parse(utf8Json);
			}
			string appName = (jsonNode is JsonObject obj) ? Str(obj["app"], 32) : "";
			if (!(jsonNode is JsonObject jsonObject) || (appName != "CTECSoundPlayer" && appName != "GSound Player") || !(jsonObject["items"] is JsonArray source))
			{
				throw new PlaylistException("File “" + fileName + "” không phải danh sách của CTECSoundPlayer");
			}
			if (Num(jsonObject["format"], 1.0, 0.0, 1000.0) > 1.0)
			{
				throw new PlaylistException("File này tạo từ bản CTECSoundPlayer mới hơn. Cập nhật phần mềm rồi nhập lại");
			}
			Playlist playlist = new Playlist
			{
				Name = Str(jsonObject["name"], 120),
				SourcePath = path,
				IsPackage = true,
				Items = source.Select(NormItem).OfType<PlaylistItem>().ToList(),
				Effects = NormFx(jsonObject["effects"]),
				Group = NormGroup(jsonObject["group"])
			};
			if (playlist.Name.Length == 0)
			{
				playlist.Name = Path.GetFileNameWithoutExtension(path);
			}
			if (DateTime.TryParse(Str(jsonObject["exported"], 40), out var result))
			{
				playlist.Exported = result;
			}
			if (playlist.Items.Count == 0)
			{
				throw new PlaylistException("Danh sách trong file này đang trống");
			}
			foreach (PlaylistItem item in playlist.Items)
			{
				if (string.IsNullOrEmpty(item.Entry) || zipArchive.GetEntry(item.Entry) == null)
				{
					item.Entry = null;
				}
			}
			return playlist;
		}
		catch (Exception ex) when ((ex is InvalidDataException || ex is JsonException) ? true : false)
		{
			throw new PlaylistException("File “" + fileName + "” bị hỏng hoặc không phải danh sách phát");
		}
	}

	public static string DecodeText(byte[] b)
	{
		if (b.Length >= 3 && b[0] == 239 && b[1] == 187 && b[2] == 191)
		{
			return Encoding.UTF8.GetString(b, 3, b.Length - 3);
		}
		if (b.Length >= 2 && b[0] == byte.MaxValue && b[1] == 254)
		{
			return Encoding.Unicode.GetString(b, 2, b.Length - 2);
		}
		try
		{
			return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(b);
		}
		catch (DecoderFallbackException)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			return Encoding.GetEncoding(1258).GetString(b);
		}
	}

	public static Playlist ParseM3u(string text, string fileName, string? baseDir)
	{
		Playlist playlist = new Playlist
		{
			Name = Path.GetFileNameWithoutExtension(fileName),
			SourcePath = fileName
		};
		(double, string)? tuple = null;
		string[] array = text.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text2 = array[i].Trim();
			if (text2.Length == 0)
			{
				continue;
			}
			if (text2.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
			{
				int num = text2.IndexOf(',');
				tuple = null;
				if (num > 8 && double.TryParse(text2.Substring(8, num - 8).Split(' ')[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					tuple = (Math.Max(0.0, result), text2.Substring(num + 1).Trim());
				}
			}
			else
			{
				if (text2[0] == '#')
				{
					continue;
				}
				if (text2.StartsWith("http:", StringComparison.OrdinalIgnoreCase) || text2.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
				{
					tuple = null;
					continue;
				}
				string text3 = text2;
				if (text3.Contains('%'))
				{
					try
					{
						text3 = Uri.UnescapeDataString(text3);
					}
					catch (UriFormatException)
					{
					}
				}
				if (text3.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(text3, UriKind.Absolute, out Uri result2))
				{
					text3 = result2.LocalPath;
				}
				string fileName2 = Path.GetFileName(text3.Replace('\\', '/'));
				string resolvedPath = null;
				try
				{
					if (Path.IsPathFullyQualified(text3) && File.Exists(text3))
					{
						resolvedPath = text3;
					}
					else if (baseDir != null)
					{
						string fullPath = Path.GetFullPath(Path.Combine(baseDir, text3));
						if (File.Exists(fullPath))
						{
							resolvedPath = fullPath;
						}
					}
				}
				catch (Exception ex2) when ((ex2 is ArgumentException || ex2 is NotSupportedException || ex2 is PathTooLongException) ? true : false)
				{
				}
				List<PlaylistItem> items = playlist.Items;
				PlaylistItem playlistItem = new PlaylistItem();
				string text4 = tuple?.Item2;
				playlistItem.Name = ((text4 != null && text4.Length > 0) ? text4 : Path.GetFileNameWithoutExtension(fileName2));
				playlistItem.File = fileName2;
				playlistItem.Dur = tuple?.Item1 ?? 0.0;
				playlistItem.ResolvedPath = resolvedPath;
				playlistItem.Plain = true;
				items.Add(playlistItem);
				tuple = null;
			}
		}
		if (playlist.Items.Count == 0)
		{
			throw new PlaylistException("File M3U này không có bài nào");
		}
		return playlist;
	}

	public static Playlist Read(string path)
	{
		if (Catalog.IsPackage(path))
		{
			return ReadPackage(path);
		}
		FileInfo fileInfo = new FileInfo(path);
		if (fileInfo.Length > 5000000)
		{
			throw new PlaylistException("File “" + fileInfo.Name + "” quá lớn, không phải danh sách phát");
		}
		return ParseM3u(DecodeText(File.ReadAllBytes(path)), fileInfo.Name, fileInfo.DirectoryName);
	}

	public static void Extract(string packagePath, IEnumerable<(string Entry, string Dest)> list, IProgress<Progress>? progress, CancellationToken ct)
	{
		using ZipArchive zipArchive = ZipFile.OpenRead(packagePath);
		List<(string, string)> list2 = list.ToList();
		int num = 0;
		foreach (var (entryName, path) in list2)
		{
			ct.ThrowIfCancellationRequested();
			using (Stream stream = (zipArchive.GetEntry(entryName) ?? throw new PlaylistException("File bị thiếu nhạc bên trong")).Open())
			{
				using FileStream destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1048576);
				stream.CopyTo(destination);
			}
			progress?.Report(new Progress(++num, list2.Count, $"Đang chép bài {num}/{list2.Count}"));
		}
	}

	public static bool SameParams(Track t, PlaylistItem x)
	{
		if (Eq(t.InP, x.InP) && Eq(t.Out, x.OutP ?? t.Dur) && Eq(t.FadeIn, x.FadeIn) && Eq(t.FadeOut, x.FadeOut) && Eq(t.Vol, x.Vol) && t.Warn == x.Warn && t.Loud == x.Loud)
		{
			return t.Note == x.Note;
		}
		return false;
		static bool Eq(double a, double b)
		{
			return Math.Abs(a - b) < 0.05;
		}
	}

	public static void ApplyParams(Track t, PlaylistItem x)
	{
		t.InP = x.InP;
		t.OutP = x.OutP;
		t.FadeIn = x.FadeIn;
		t.FadeOut = x.FadeOut;
		t.Vol = x.Vol;
		t.Warn = x.Warn;
		t.Loud = x.Loud;
		t.Note = x.Note;
		t.AutoTrim = x.AutoTrim && x.InP > 0.0;
		t.Edited = true;
	}
}
