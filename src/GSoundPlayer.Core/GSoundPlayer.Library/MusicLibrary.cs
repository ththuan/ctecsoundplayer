using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using GSoundPlayer.Audio;
using GSoundPlayer.Model;

namespace GSoundPlayer.Library;

public sealed class MusicLibrary : IDisposable
{
	public sealed record ImportOptions
	{
		public bool CopyToLibrary { get; init; }

		public string? WatchRoot { get; init; }

		public Func<string, string>? NameOverride { get; init; }

		public Func<string, List<string>?>? TagOverride { get; init; }
	}

	public const int WavePoints = 1200;

	public readonly List<Track> Tracks = new List<Track>();

	private readonly LibraryStore _store;

	private readonly Dictionary<string, Track> _byHash = new Dictionary<string, Track>();

	private readonly Queue<Track> _analysisQueue = new Queue<Track>();

	private readonly object _lock = new object();

	private readonly SynchronizationContext? _ui;

	private readonly Thread _analyzer;

	private readonly AutoResetEvent _wake = new AutoResetEvent(initialState: false);

	private volatile bool _quit;

	public int AnalysisTotal { get; private set; }

	public int AnalysisDone { get; private set; }

	public event Action? Updated;

	public event Action? AnalysisProgress;

	public MusicLibrary(LibraryStore store, SynchronizationContext? ui)
	{
		_store = store;
		_ui = ui;
		Tracks.AddRange(store.LoadAll());
		foreach (Track track in Tracks)
		{
			if (track.QuickHash.Length > 0)
			{
				_byHash[track.QuickHash] = track;
			}
			if (!track.Analyzed)
			{
				EnqueueAnalysis(track);
			}
		}
		_analyzer = new Thread(AnalyzerLoop)
		{
			IsBackground = true,
			Name = "PhanTichNhac",
			Priority = ThreadPriority.BelowNormal
		};
		_analyzer.Start();
	}

	private void Post(Action a)
	{
		if (_ui != null)
		{
			_ui.Post((object? _) =>
			{
				a();
			}, null);
		}
		else
		{
			a();
		}
	}

	public Track? FindById(long id)
	{
		return Tracks.FirstOrDefault((Track t) => t.Id == id);
	}

	public Track? FindByHash(string hash)
	{
		return _byHash.GetValueOrDefault(hash);
	}

	public List<Track> View(string query, string chip, IReadOnlyList<long>? groupOrder = null)
	{
		string[] words = Text.Vn(query.Trim()).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		IEnumerable<Track> enumerable = Tracks.Where((Track t) => InChip(t, chip));
		if (words.Length != 0)
		{
			enumerable = enumerable.Where((Track t) => words.All((string w) => t.SearchKey.Contains(w, StringComparison.Ordinal)));
		}
		if (chip == "recent")
		{
			return enumerable.OrderByDescending((Track t) => t.LastPlayed).ToList();
		}
		if (chip == "new")
		{
			return enumerable.OrderByDescending((Track t) => t.Imported).ThenBy((Track t) => t.SearchKey, StringComparer.Ordinal).ToList();
		}
		if (groupOrder != null && words.Length == 0)
		{
			return GroupOrder.Sort(enumerable, groupOrder);
		}
		if (words.Length != 0)
		{
			string first = words[0];
			return enumerable.OrderBy((Track t) => (!t.SearchKey.StartsWith(first, StringComparison.Ordinal)) ? 1 : 0).ThenBy((Track t) => t.SearchKey, StringComparer.Ordinal).ToList();
		}
		return enumerable.OrderBy((Track t) => t.SearchKey, StringComparer.Ordinal).ToList();
	}

	public static bool InChip(Track t, string k)
	{
		return k switch
		{
			"all" => true, 
			"none" => t.Tags.Count == 0, 
			"fav" => t.Fav, 
			"recent" => t.LastPlayed > 0, 
			"new" => t.Imported > 0 && DateTimeOffset.Now.ToUnixTimeMilliseconds() - t.Imported < 86400000, 
			_ => t.Tags.Contains(k), 
		};
	}

	public int Count(string chip)
	{
		return Enumerable.Count(Tracks, (Track t) => InChip(t, chip));
	}

	public static string QuickHash(string path)
	{
		using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096);
		long length = fileStream.Length;
		using IncrementalHash incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		incrementalHash.AppendData(BitConverter.GetBytes(length));
		byte[] array = new byte[65536];
		int count = fileStream.Read(array, 0, 65536);
		incrementalHash.AppendData(array, 0, count);
		if (length > 65536)
		{
			fileStream.Seek(Math.Max(65536L, length - 65536), SeekOrigin.Begin);
			count = fileStream.Read(array, 0, 65536);
			incrementalHash.AppendData(array, 0, count);
		}
		return Convert.ToHexString(incrementalHash.GetHashAndReset());
	}

	public static (List<string> Audio, int Skipped) Expand(IEnumerable<string> paths)
	{
		List<string> list = new List<string>();
		int num = 0;
		foreach (string path in paths)
		{
			if (Directory.Exists(path))
			{
				try
				{
					foreach (string item in Directory.EnumerateFiles(path, "*", new EnumerationOptions
					{
						RecurseSubdirectories = true,
						IgnoreInaccessible = true
					}))
					{
						if (Catalog.IsAudio(item))
						{
							list.Add(item);
						}
					}
				}
				catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
				{
					num++;
				}
			}
			else if (File.Exists(path))
			{
				if (Catalog.IsAudio(path))
				{
					list.Add(path);
				}
				else
				{
					num++;
				}
			}
		}
		return (Audio: list, Skipped: num);
	}

	public async Task<ImportResult> ImportAsync(IEnumerable<string> paths, ImportOptions? opts = null)
	{
		if ((object)opts == null)
		{
			opts = new ImportOptions();
		}
		List<string> input = paths.ToList();
		(List<(string, string, bool, string)>, int) tuple = await Task.Run(() =>
		{
			(List<string> Audio, int Skipped) tuple5 = Expand(input);
			List<string> item = tuple5.Audio;
			int item2 = tuple5.Skipped;
			List<(string, string, bool, string)> list = new List<(string, string, bool, string)>();
			foreach (string item4 in item.Distinct(StringComparer.OrdinalIgnoreCase))
			{
				try
				{
					string item3 = (Text.CleanName(Path.GetFileName(item4)).Junk ? AudioEngine.ReadTitle(item4) : null);
					list.Add((item4, QuickHash(item4), false, item3));
				}
				catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
				{
					list.Add((item4, "", true, null));
				}
			}
			return ((List<(string Path, string Hash, bool Error, string Title)> rows, int skipped))(rows: list, skipped: item2);
		});
		List<Track> added = new List<Track>();
		int dup = 0;
		int bad = tuple.Item2;
		HashSet<string> seen = new HashSet<string>();
		long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		foreach (var item5 in tuple.Item1)
		{
			var (path, text, flag, text2) = item5;
			if (flag)
			{
				bad++;
				continue;
			}
			if (_byHash.TryGetValue(text, out Track existing))
			{
				dup++;
				if (!existing.Missing && File.Exists(existing.FilePath))
				{
					continue;
				}
				bool flag2 = true;
				if (Catalog.IsVideo(path))
				{
					flag2 = await Task.Run(() => ExtractInto(existing, path)) == 0;
				}
				else
				{
					existing.FilePath = path;
				}
				if (flag2)
				{
					existing.Missing = false;
					_store.Touch(existing);
				}
				continue;
			}
			if (!seen.Add(text))
			{
				dup++;
				continue;
			}
			var (text3, flag3) = Text.CleanName(Path.GetFileName(path));
			if (flag3 && !string.IsNullOrEmpty(text2))
			{
				text3 = text2;
				flag3 = false;
			}
			string text4 = opts.NameOverride?.Invoke(path) ?? text3;
			List<string> tags = opts.TagOverride?.Invoke(path) ?? new List<string>();
			added.Add(new Track
			{
				FilePath = path,
				FileName = Path.GetFileName(path),
				Name = text4,
				Tags = tags,
				Junk = (flag3 && text4 == text3),
				Imported = now,
				QuickHash = text,
				WatchRoot = opts.WatchRoot,
				Dur = 0.0,
				Analyzing = true
			});
		}
		List<string> noAudio = new List<string>();
		if (added.Any((Track t) => Catalog.IsVideo(t.FilePath)))
		{
			foreach (var (track, num) in await Task.Run(() => (from t in added
				where Catalog.IsVideo(t.FilePath)
				select (Track: t, Rc: ExtractInto(t, t.FilePath)) into x
				where x.Rc != 0
				select x).ToList()))
			{
				added.Remove(track);
				if (num == -9)
				{
					noAudio.Add(track.FileName);
				}
				else
				{
					bad++;
				}
			}
		}
		if (opts.CopyToLibrary && added.Count > 0)
		{
			await Task.Run(() =>
			{
				foreach (Track item6 in added)
				{
					if (!item6.FilePath.StartsWith(AppPaths.MusicDir, StringComparison.OrdinalIgnoreCase))
					{
						try
						{
							string text5 = UniquePath(AppPaths.MusicDir, Text.SafeFileName(item6.FileName));
							File.Copy(item6.FilePath, text5);
							item6.FilePath = text5;
						}
						catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
						{
						}
					}
				}
			});
		}
		if (added.Count > 0)
		{
			await Task.Run(() =>
			{
				_store.Insert(added);
			});
			foreach (Track item7 in added)
			{
				Tracks.Add(item7);
				_byHash[item7.QuickHash] = item7;
				EnqueueAnalysis(item7);
			}
			Updated?.Invoke();
		}
		return new ImportResult(added, dup, bad, noAudio);
	}

	public async Task<string?> ReplaceFileAsync(Track t, string path)
	{
		if (!File.Exists(path) || !Catalog.IsAudio(path))
		{
			return "File này không phải nhạc";
		}
		string hash;
		string title;
		try
		{
			(hash, title) = await Task.Run(() => (QuickHash(path), Text.CleanName(Path.GetFileName(path)).Junk ? AudioEngine.ReadTitle(path) : null));
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return "Không đọc được file này";
		}
		if (hash == t.QuickHash)
		{
			return "Đây chính là file đang dùng của bài này";
		}
		if (_byHash.TryGetValue(hash, out Track value) && value != t)
		{
			return "Bài này đã có trong kho: “" + value.Name + "”";
		}
		Track holder = new Track
		{
			FilePath = path,
			FileName = Path.GetFileName(path)
		};
		if (Catalog.IsVideo(path))
		{
			switch (await Task.Run(() => ExtractInto(holder, path)))
			{
			case -9:
				return "Video này chỉ có hình, không có tiếng";
			default:
				return "Không lấy được phần tiếng của video này";
			case 0:
				break;
			}
		}
		float[] wave = new float[1200];
		var (num, r) = await Task.Run(() => (code: AudioEngine.Analyze(holder.FilePath, out var r2, wave), res: r2));
		if (num != 0)
		{
			return "File này hỏng hoặc không phát được";
		}
		var (name, flag) = Text.CleanName(Path.GetFileName(path));
		if (flag && !string.IsNullOrEmpty(title))
		{
			name = title;
			flag = false;
		}
		if (t.QuickHash.Length > 0)
		{
			_byHash.Remove(t.QuickHash);
		}
		_byHash[hash] = t;
		t.QuickHash = hash;
		t.FilePath = holder.FilePath;
		t.FileName = holder.FileName;
		t.Name = name;
		t.Junk = flag;
		t.InP = 0.0;
		t.OutP = null;
		t.FadeIn = 0.0;
		t.FadeOut = 0.0;
		t.Vol = 0.0;
		t.Edited = false;
		t.AutoTrim = false;
		t.Missing = false;
		t.Analyzing = false;
		ApplyAnalysis(t, r, wave);
		_store.Touch(t);
		Updated?.Invoke();
		return null;
	}

	private static int ExtractInto(Track t, string videoPath)
	{
		string text = UniquePath(AppPaths.MusicDir, Text.SafeFileName(Path.GetFileNameWithoutExtension(videoPath)) + ".m4a");
		int num = AudioEngine.ExtractAudio(videoPath, text);
		if (num != 0)
		{
			return num;
		}
		t.FilePath = text;
		t.FileName = Path.GetFileName(text);
		return 0;
	}

	public async Task ConvertVideosAsync()
	{
		List<Track> list = Tracks.Where((Track track) => Catalog.IsVideo(track.FilePath) && File.Exists(track.FilePath)).ToList();
		foreach (Track t in list)
		{
			string path = t.FilePath;
			if (await Task.Run(() => ExtractInto(t, path)) == 0)
			{
				_store.Touch(t);
			}
		}
	}

	public static string UniquePath(string dir, string fileName)
	{
		string text = Path.Combine(dir, fileName);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		string extension = Path.GetExtension(fileName);
		int num = 2;
		while (File.Exists(text))
		{
			text = Path.Combine(dir, $"{fileNameWithoutExtension} ({num}){extension}");
			num++;
		}
		return text;
	}

	private void EnqueueAnalysis(Track t)
	{
		t.Analyzing = true;
		lock (_lock)
		{
			_analysisQueue.Enqueue(t);
			AnalysisTotal++;
		}
		_wake.Set();
	}

	private void AnalyzerLoop()
	{
		while (!_quit)
		{
			Track t = null;
			lock (_lock)
			{
				if (_analysisQueue.Count > 0)
				{
					t = _analysisQueue.Dequeue();
				}
			}
			if (t == null)
			{
				_wake.WaitOne(1000);
				continue;
			}
			float[] wave = new float[1200];
			int rc = -1;
			AnalysisResult r = default;
			if (File.Exists(t.FilePath))
			{
				rc = AudioEngine.Analyze(t.FilePath, out r, wave);
			}
			bool missing = !File.Exists(t.FilePath);
			Post(() =>
			{
				t.Analyzing = false;
				if (rc == 0)
				{
					ApplyAnalysis(t, r, wave);
				}
				else
				{
					t.Missing = missing || rc != 0;
				}
				_store.Touch(t);
				lock (_lock)
				{
					int analysisDone = AnalysisDone;
					AnalysisDone = analysisDone + 1;
					if (AnalysisDone >= AnalysisTotal)
					{
						AnalysisDone = 0;
						AnalysisTotal = 0;
					}
				}
				AnalysisProgress?.Invoke();
				Updated?.Invoke();
			});
		}
	}

	private static void ApplyAnalysis(Track t, AnalysisResult r, float[] wave)
	{
		t.Dur = r.DurationS;
		t.Lufs = ((r.Lufs > -69.9) ? new double?(r.Lufs) : ((double?)null));
		t.TruePeakDb = r.TruePeakDb;
		t.Wave = wave;
		t.Analyzed = true;
		if (!t.Edited && t.InP == 0.0 && r.SilenceHeadS >= 0.3 && r.SilenceHeadS < r.DurationS - 1.0)
		{
			t.InP = Math.Round(r.SilenceHeadS, 2);
			t.AutoTrim = true;
		}
		else if (!t.AutoTrim && t.InP > 0.0 && Math.Abs(t.InP - Math.Round(r.SilenceHeadS, 2)) < 0.015)
		{
			t.AutoTrim = true;
		}
	}

	public void Save(Track t)
	{
		_store.Touch(t);
	}

	public void Flush()
	{
		_store.Flush();
	}

	public void Detach(IEnumerable<Track> tracks)
	{
		HashSet<Track> hashSet = tracks.ToHashSet();
		if (hashSet.Count == 0)
		{
			return;
		}
		Tracks.RemoveAll(hashSet.Contains);
		foreach (Track item in hashSet)
		{
			if (item.QuickHash.Length > 0 && _byHash.GetValueOrDefault(item.QuickHash) == item)
			{
				_byHash.Remove(item.QuickHash);
			}
		}
		Updated?.Invoke();
	}

	public List<Track> Reattach(IEnumerable<Track> tracks)
	{
		List<Track> list = new List<Track>();
		foreach (Track track in tracks)
		{
			if (!Tracks.Contains(track) && (track.QuickHash.Length <= 0 || !_byHash.ContainsKey(track.QuickHash)))
			{
				Tracks.Add(track);
				if (track.QuickHash.Length > 0)
				{
					_byHash[track.QuickHash] = track;
				}
				list.Add(track);
			}
		}
		if (list.Count > 0)
		{
			Updated?.Invoke();
		}
		return list;
	}

	public Task Delete(IEnumerable<Track> tracks)
	{
		HashSet<Track> hashSet = tracks.ToHashSet();
		if (hashSet.Count == 0)
		{
			return Task.CompletedTask;
		}
		Tracks.RemoveAll(hashSet.Contains);
		foreach (Track item in hashSet)
		{
			if (item.QuickHash.Length > 0 && _byHash.GetValueOrDefault(item.QuickHash) == item)
			{
				_byHash.Remove(item.QuickHash);
			}
		}
		_store.Delete(from t in hashSet
			where t.Id != 0
			select t.Id);
		HashSet<string> still = Tracks.Select((Track t) => t.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
		List<string> files = (from t in hashSet
			select t.FilePath into p
			where AppPaths.IsInside(p, AppPaths.MusicDir) && !still.Contains(p)
			select p).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		Updated?.Invoke();
		if (files.Count != 0)
		{
			return Task.Run(() =>
			{
				DeleteFiles(files);
			});
		}
		return Task.CompletedTask;
	}

	public static void DeleteFiles(List<string> files)
	{
		for (int i = 0; i < 6; i++)
		{
			if (files.Count <= 0)
			{
				break;
			}
			if (i > 0)
			{
				Thread.Sleep(500);
			}
			files.RemoveAll((string f) =>
			{
				try
				{
					File.Delete(f);
					return true;
				}
				catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
				{
					return false;
				}
			});
		}
	}

	public async Task<int> CheckMissingAsync()
	{
		List<Track> snapshot = Tracks.ToList();
		HashSet<Track> hashSet = await Task.Run(() => snapshot.Where((Track t) => !File.Exists(t.FilePath)).ToHashSet());
		int num = 0;
		foreach (Track item in snapshot)
		{
			bool flag = hashSet.Contains(item);
			if (flag)
			{
				num++;
			}
			if (item.Missing != flag)
			{
				item.Missing = flag;
			}
		}
		if (num > 0)
		{
			Updated?.Invoke();
		}
		return num;
	}

	public string? GetKv(string k)
	{
		return _store.GetKv(k);
	}

	public void SetKv(string k, string v)
	{
		_store.SetKv(k, v);
	}

	public void Dispose()
	{
		_quit = true;
		_wake.Set();
		_analyzer.Join(TimeSpan.FromSeconds(2L));
		_wake.Dispose();
	}
}
