using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using GSoundPlayer.Model;

namespace GSoundPlayer.Library;

public sealed class SourceWatcher : IDisposable
{
	private readonly Settings _settings;

	private readonly object _lock = new object();

	private readonly Dictionary<string, Source> _sources = new Dictionary<string, Source>();

	private readonly HashSet<string> _dismissed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();

	private readonly Timer _driveTimer;

	private readonly Timer _settleTimer;

	private readonly Dictionary<string, (long Size, int Stable)> _settling = new Dictionary<string, (long, int)>(StringComparer.OrdinalIgnoreCase);

	private readonly DateTime _startedAt = DateTime.Now;

	private HashSet<string> _knownDrives = new HashSet<string>();

	private FileSystemWatcher? _zalo;

	public string ZaloDir => _settings.ZaloDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Zalo Received Files");

	public int ZaloCount
	{
		get
		{
			lock (_lock)
			{
				Source value;
				return _sources.TryGetValue("zalo", out value) ? value.Files.Count : 0;
			}
		}
	}

	public event Action? SourcesChanged;

	public event Action<string, string>? WatchedFileReady;

	public SourceWatcher(Settings settings)
	{
		_settings = settings;
		_driveTimer = new Timer((object? _) =>
		{
			ScanDrives();
		}, null, 500, 2000);
		_settleTimer = new Timer((object? _) =>
		{
			CheckSettling();
		}, null, 1000, 1000);
	}

	public IReadOnlyList<Source> Snapshot()
	{
		lock (_lock)
		{
			return _sources.Values.Where((Source s) => s.Files.Count > 0).ToList();
		}
	}

	public void Start()
	{
		StartZalo();
		foreach (string watchFolder in _settings.WatchFolders)
		{
			WatchFolder(watchFolder);
		}
	}

	public void StartZalo()
	{
		_zalo?.Dispose();
		_zalo = null;
		lock (_lock)
		{
			_sources.Remove("zalo");
		}
		string zaloDir = ZaloDir;
		if (Directory.Exists(zaloDir))
		{
			lock (_lock)
			{
				_sources["zalo"] = new Source
				{
					Key = "zalo",
					Title = "Zalo",
					Where = zaloDir
				};
			}
			FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(zaloDir)
			{
				IncludeSubdirectories = true,
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite)
			};
			fileSystemWatcher.Created += (object _, FileSystemEventArgs e) =>
			{
				Track(e.FullPath);
			};
			fileSystemWatcher.Renamed += (object _, RenamedEventArgs e) =>
			{
				Track(e.FullPath);
			};
			fileSystemWatcher.Changed += (object _, FileSystemEventArgs e) =>
			{
				Track(e.FullPath);
			};
			fileSystemWatcher.EnableRaisingEvents = true;
			_zalo = fileSystemWatcher;
		}
	}

	private void Track(string path)
	{
		if (!Catalog.IsAudio(path))
		{
			return;
		}
		lock (_lock)
		{
			_settling[path] = (-1L, 0);
		}
	}

	private void CheckSettling()
	{
		List<string> list = new List<string>();
		lock (_lock)
		{
			foreach (var (text2, tuple2) in _settling.ToList())
			{
				long length;
				try
				{
					length = new FileInfo(text2).Length;
				}
				catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
				{
					_settling.Remove(text2);
					continue;
				}
				if (length > 0 && length == tuple2.Item1)
				{
					if (tuple2.Item2 >= 1)
					{
						_settling.Remove(text2);
						list.Add(text2);
					}
					else
					{
						_settling[text2] = (length, tuple2.Item2 + 1);
					}
				}
				else
				{
					_settling[text2] = (length, 0);
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		bool flag = false;
		foreach (string path in list)
		{
			string text3 = null;
			lock (_lock)
			{
				text3 = _settings.WatchFolders.FirstOrDefault((string f) => path.StartsWith(f, StringComparison.OrdinalIgnoreCase));
				if (text3 == null && _sources.TryGetValue("zalo", out Source value) && path.StartsWith(value.Where, StringComparison.OrdinalIgnoreCase) && !_dismissed.Contains(path) && !value.Files.Any((SourceFile f) => f.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
				{
					value.Files.Add(new SourceFile
					{
						Path = path,
						Size = new FileInfo(path).Length
					});
					flag = true;
				}
			}
			if (text3 != null)
			{
				WatchedFileReady?.Invoke(text3, path);
			}
		}
		if (flag)
		{
			SourcesChanged?.Invoke();
		}
	}

	private void ScanDrives()
	{
		DriveInfo[] drives;
		try
		{
			drives = DriveInfo.GetDrives();
		}
		catch (IOException)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(from d in drives
			where d.DriveType == DriveType.Removable && d.IsReady
			select d.Name);
		if (hashSet.SetEquals(_knownDrives))
		{
			return;
		}
		List<string> list = hashSet.Except(_knownDrives).ToList();
		List<string> list2 = _knownDrives.Except(hashSet).ToList();
		_knownDrives = hashSet;
		foreach (string item2 in list2)
		{
			lock (_lock)
			{
				_sources.Remove("usb:" + item2);
			}
		}
		foreach (string item3 in list)
		{
			List<string> item = MusicLibrary.Expand(new string[1] { item3 }).Audio;
			if (item.Count != 0)
			{
				string value = "";
				try
				{
					value = new DriveInfo(item3).VolumeLabel;
				}
				catch (IOException)
				{
				}
				Source source = new Source
				{
					Key = "usb:" + item3,
					Title = $"USB {value} ({item3.TrimEnd('\\')})".Replace("  ", " "),
					Where = item3,
					Files = item.Select((string p) => new SourceFile
					{
						Path = p
					}).ToList()
				};
				lock (_lock)
				{
					_sources[source.Key] = source;
				}
			}
		}
		SourcesChanged?.Invoke();
	}

	public void ClearSource(string key, IEnumerable<string> paths)
	{
		lock (_lock)
		{
			if (_sources.TryGetValue(key, out Source value))
			{
				HashSet<string> set = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
				foreach (string item in set)
				{
					_dismissed.Add(item);
				}
				value.Files.RemoveAll((SourceFile f) => set.Contains(f.Path));
			}
		}
		SourcesChanged?.Invoke();
	}

	public void WatchFolder(string path)
	{
		if (!Directory.Exists(path))
		{
			return;
		}
		FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(path)
		{
			IncludeSubdirectories = true,
			NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite)
		};
		fileSystemWatcher.Created += (object _, FileSystemEventArgs e) =>
		{
			Track(e.FullPath);
		};
		fileSystemWatcher.Renamed += (object _, RenamedEventArgs e) =>
		{
			Track(e.FullPath);
		};
		fileSystemWatcher.EnableRaisingEvents = true;
		lock (_lock)
		{
			_watchers.Add(fileSystemWatcher);
		}
	}

	public void UnwatchFolder(string path)
	{
		lock (_lock)
		{
			foreach (FileSystemWatcher item in _watchers.Where((FileSystemWatcher w) => w.Path.Equals(path, StringComparison.OrdinalIgnoreCase)).ToList())
			{
				item.Dispose();
				_watchers.Remove(item);
			}
		}
	}

	public void Dispose()
	{
		_driveTimer.Dispose();
		_settleTimer.Dispose();
		_zalo?.Dispose();
		lock (_lock)
		{
			foreach (FileSystemWatcher watcher in _watchers)
			{
				watcher.Dispose();
			}
			_watchers.Clear();
		}
	}
}
