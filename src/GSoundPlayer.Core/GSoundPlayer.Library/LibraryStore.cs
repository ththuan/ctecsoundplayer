using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using GSoundPlayer.Model;
using Microsoft.Data.Sqlite;

namespace GSoundPlayer.Library;

public sealed class LibraryStore : IDisposable
{
	private sealed record Row(long Id, string Path, string FileName, string Name, string Tags, bool Fav, double Dur, double? Lufs, double? Tp, double InP, double? OutP, double Fi, double Fo, double Vol, int End, int Warn, bool Loud, string Note, bool Edited, bool Junk, bool AutoTrim, long Last, long Imported, string Hash, string? Root, byte[]? Wave, bool Analyzed);

	private readonly SqliteConnection _db;

	private readonly BlockingCollection<Action> _writes = new BlockingCollection<Action>();

	private readonly Thread _writer;

	private readonly object _dirtyLock = new object();

	private readonly HashSet<Track> _dirty = new HashSet<Track>();

	private LibraryStore(SqliteConnection db)
	{
		_db = db;
		_writer = new Thread(WriterLoop)
		{
			IsBackground = true,
			Name = "GhiKhoNhac",
			Priority = ThreadPriority.BelowNormal
		};
		_writer.Start();
	}

	public static LibraryStore Open(string path)
	{
		SqliteConnection sqliteConnection = new SqliteConnection(new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Pooling = false
		}.ToString());
		sqliteConnection.Open();
		using (SqliteCommand sqliteCommand = sqliteConnection.CreateCommand())
		{
			sqliteCommand.CommandText = "PRAGMA journal_mode=WAL;\r\nPRAGMA synchronous=NORMAL;\r\nCREATE TABLE IF NOT EXISTS tracks(\r\n  id INTEGER PRIMARY KEY AUTOINCREMENT,\r\n  path TEXT NOT NULL, file_name TEXT NOT NULL, name TEXT NOT NULL,\r\n  tags TEXT NOT NULL DEFAULT '', fav INTEGER NOT NULL DEFAULT 0,\r\n  dur REAL NOT NULL DEFAULT 0, lufs REAL, true_peak REAL,\r\n  in_p REAL NOT NULL DEFAULT 0, out_p REAL,\r\n  fade_in REAL NOT NULL DEFAULT 0, fade_out REAL NOT NULL DEFAULT 0, vol REAL NOT NULL DEFAULT 0,\r\n  end_mode INTEGER NOT NULL DEFAULT 0, warn INTEGER NOT NULL DEFAULT 10, loud INTEGER NOT NULL DEFAULT 1,\r\n  note TEXT NOT NULL DEFAULT '', edited INTEGER NOT NULL DEFAULT 0, junk INTEGER NOT NULL DEFAULT 0,\r\n  auto_trim INTEGER NOT NULL DEFAULT 0, last_played INTEGER NOT NULL DEFAULT 0, imported INTEGER NOT NULL DEFAULT 0,\r\n  quick_hash TEXT NOT NULL DEFAULT '', watch_root TEXT, wave BLOB, analyzed INTEGER NOT NULL DEFAULT 0\r\n);\r\nCREATE INDEX IF NOT EXISTS idx_tracks_hash ON tracks(quick_hash);\r\nCREATE TABLE IF NOT EXISTS kv(key TEXT PRIMARY KEY, value TEXT NOT NULL);";
			sqliteCommand.ExecuteNonQuery();
		}
		return new LibraryStore(sqliteConnection);
	}

	private void WriterLoop()
	{
		foreach (Action item in _writes.GetConsumingEnumerable())
		{
			try
			{
				item();
			}
			catch (SqliteException)
			{
			}
		}
	}

	public List<Track> LoadAll()
	{
		List<Track> list = new List<Track>();
		using SqliteCommand sqliteCommand = _db.CreateCommand();
		sqliteCommand.CommandText = "SELECT id,path,file_name,name,tags,fav,dur,lufs,true_peak,in_p,out_p,fade_in,fade_out,vol,end_mode,warn,loud,note,edited,junk,auto_trim,last_played,imported,quick_hash,watch_root,wave,analyzed FROM tracks";
		using SqliteDataReader sqliteDataReader = sqliteCommand.ExecuteReader();
		while (sqliteDataReader.Read())
		{
			Track track = new Track
			{
				Id = sqliteDataReader.GetInt64(0),
				FilePath = sqliteDataReader.GetString(1),
				FileName = sqliteDataReader.GetString(2),
				Name = sqliteDataReader.GetString(3),
				Tags = sqliteDataReader.GetString(4).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList(),
				Fav = (sqliteDataReader.GetInt32(5) != 0),
				Dur = sqliteDataReader.GetDouble(6),
				Lufs = (sqliteDataReader.IsDBNull(7) ? ((double?)null) : new double?(sqliteDataReader.GetDouble(7))),
				TruePeakDb = (sqliteDataReader.IsDBNull(8) ? ((double?)null) : new double?(sqliteDataReader.GetDouble(8))),
				InP = sqliteDataReader.GetDouble(9),
				OutP = (sqliteDataReader.IsDBNull(10) ? ((double?)null) : new double?(sqliteDataReader.GetDouble(10))),
				FadeIn = sqliteDataReader.GetDouble(11),
				FadeOut = sqliteDataReader.GetDouble(12),
				Vol = sqliteDataReader.GetDouble(13),
				Warn = sqliteDataReader.GetInt32(15),
				Loud = (sqliteDataReader.GetInt32(16) != 0),
				Note = sqliteDataReader.GetString(17),
				Edited = (sqliteDataReader.GetInt32(18) != 0),
				Junk = (sqliteDataReader.GetInt32(19) != 0),
				AutoTrim = (sqliteDataReader.GetInt32(20) != 0),
				LastPlayed = sqliteDataReader.GetInt64(21),
				Imported = sqliteDataReader.GetInt64(22),
				QuickHash = sqliteDataReader.GetString(23),
				WatchRoot = (sqliteDataReader.IsDBNull(24) ? null : sqliteDataReader.GetString(24)),
				Analyzed = (sqliteDataReader.GetInt32(26) != 0)
			};
			if (!sqliteDataReader.IsDBNull(25))
			{
				track.Wave = BytesToWave((byte[])sqliteDataReader[25]);
			}
			list.Add(track);
		}
		return list;
	}

	private static float[] BytesToWave(byte[] b)
	{
		float[] array = new float[b.Length];
		for (int i = 0; i < b.Length; i++)
		{
			array[i] = (float)(int)b[i] / 255f;
		}
		return array;
	}

	private static byte[]? WaveToBytes(float[]? w)
	{
		if (w == null)
		{
			return null;
		}
		byte[] array = new byte[w.Length];
		for (int i = 0; i < w.Length; i++)
		{
			array[i] = (byte)Math.Clamp((int)(w[i] * 255f + 0.5f), 0, 255);
		}
		return array;
	}

	private static object Db(object? v)
	{
		return v ?? DBNull.Value;
	}

	private static Row Snapshot(Track t)
	{
		return new Row(t.Id, t.FilePath, t.FileName, t.Name, string.Join(' ', t.Tags), t.Fav, t.Dur, t.Lufs, t.TruePeakDb, t.InP, t.OutP, t.FadeIn, t.FadeOut, t.Vol, 0, t.Warn, t.Loud, t.Note, t.Edited, t.Junk, t.AutoTrim, t.LastPlayed, t.Imported, t.QuickHash, t.WatchRoot, WaveToBytes(t.Wave), t.Analyzed);
	}

	public void Insert(IReadOnlyList<Track> tracks)
	{
		if (tracks.Count == 0)
		{
			return;
		}
		List<Row> rows = tracks.Select(Snapshot).ToList();
		ManualResetEventSlim done = new ManualResetEventSlim();
		try
		{
			_writes.Add(() =>
			{
				try
				{
					using SqliteTransaction sqliteTransaction = _db.BeginTransaction();
					using SqliteCommand sqliteCommand = _db.CreateCommand();
					sqliteCommand.Transaction = sqliteTransaction;
					sqliteCommand.CommandText = "INSERT INTO tracks(path,file_name,name,tags,fav,dur,lufs,true_peak,in_p,out_p,fade_in,fade_out,vol,end_mode,warn,loud,note,edited,junk,auto_trim,last_played,imported,quick_hash,watch_root,wave,analyzed)\r\nVALUES($p,$f,$n,$t,$fav,$d,$l,$tp,$in,$out,$fi,$fo,$v,$e,$w,$lo,$no,$ed,$j,$a,$la,$im,$h,$r,$wa,$an);\r\nSELECT last_insert_rowid();";
					for (int i = 0; i < rows.Count; i++)
					{
						sqliteCommand.Parameters.Clear();
						Bind(sqliteCommand, rows[i]);
						tracks[i].Id = Convert.ToInt64(sqliteCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
					}
					sqliteTransaction.Commit();
				}
				finally
				{
					done.Set();
				}
			});
			done.Wait();
		}
		finally
		{
			if (done != null)
			{
				((IDisposable)done).Dispose();
			}
		}
	}

	private static void Bind(SqliteCommand c, Row r)
	{
		c.Parameters.AddWithValue("$p", r.Path);
		c.Parameters.AddWithValue("$f", r.FileName);
		c.Parameters.AddWithValue("$n", r.Name);
		c.Parameters.AddWithValue("$t", r.Tags);
		c.Parameters.AddWithValue("$fav", r.Fav ? 1 : 0);
		c.Parameters.AddWithValue("$d", r.Dur);
		c.Parameters.AddWithValue("$l", Db(r.Lufs));
		c.Parameters.AddWithValue("$tp", Db(r.Tp));
		c.Parameters.AddWithValue("$in", r.InP);
		c.Parameters.AddWithValue("$out", Db(r.OutP));
		c.Parameters.AddWithValue("$fi", r.Fi);
		c.Parameters.AddWithValue("$fo", r.Fo);
		c.Parameters.AddWithValue("$v", r.Vol);
		c.Parameters.AddWithValue("$e", r.End);
		c.Parameters.AddWithValue("$w", r.Warn);
		c.Parameters.AddWithValue("$lo", r.Loud ? 1 : 0);
		c.Parameters.AddWithValue("$no", r.Note);
		c.Parameters.AddWithValue("$ed", r.Edited ? 1 : 0);
		c.Parameters.AddWithValue("$j", r.Junk ? 1 : 0);
		c.Parameters.AddWithValue("$a", r.AutoTrim ? 1 : 0);
		c.Parameters.AddWithValue("$la", r.Last);
		c.Parameters.AddWithValue("$im", r.Imported);
		c.Parameters.AddWithValue("$h", r.Hash);
		c.Parameters.AddWithValue("$r", Db(r.Root));
		c.Parameters.Add("$wa", SqliteType.Blob).Value = Db(r.Wave);
		c.Parameters.AddWithValue("$an", r.Analyzed ? 1 : 0);
	}

	public void Touch(Track t)
	{
		lock (_dirtyLock)
		{
			_dirty.Add(t);
		}
	}

	public void Flush()
	{
		List<Row> rows;
		lock (_dirtyLock)
		{
			if (_dirty.Count == 0)
			{
				return;
			}
			rows = _dirty.Where((Track t) => t.Id != 0).Select(Snapshot).ToList();
			_dirty.Clear();
		}
		if (rows.Count == 0)
		{
			return;
		}
		_writes.Add(() =>
		{
			using SqliteTransaction sqliteTransaction = _db.BeginTransaction();
			using SqliteCommand sqliteCommand = _db.CreateCommand();
			sqliteCommand.Transaction = sqliteTransaction;
			sqliteCommand.CommandText = "UPDATE tracks SET path=$p,file_name=$f,name=$n,tags=$t,fav=$fav,dur=$d,lufs=$l,true_peak=$tp,in_p=$in,out_p=$out,fade_in=$fi,fade_out=$fo,vol=$v,\r\nend_mode=$e,warn=$w,loud=$lo,note=$no,edited=$ed,junk=$j,auto_trim=$a,last_played=$la,imported=$im,quick_hash=$h,watch_root=$r,wave=$wa,analyzed=$an WHERE id=$id";
			foreach (Row item in rows)
			{
				sqliteCommand.Parameters.Clear();
				Bind(sqliteCommand, item);
				sqliteCommand.Parameters.AddWithValue("$id", item.Id);
				sqliteCommand.ExecuteNonQuery();
			}
			sqliteTransaction.Commit();
		});
	}

	public void Delete(IEnumerable<long> ids)
	{
		List<long> list = ids.ToList();
		_writes.Add(() =>
		{
			using SqliteTransaction sqliteTransaction = _db.BeginTransaction();
			using SqliteCommand sqliteCommand = _db.CreateCommand();
			sqliteCommand.Transaction = sqliteTransaction;
			sqliteCommand.CommandText = "DELETE FROM tracks WHERE id=$id";
			SqliteParameter sqliteParameter = sqliteCommand.Parameters.Add("$id", SqliteType.Integer);
			foreach (long item in list)
			{
				sqliteParameter.Value = item;
				sqliteCommand.ExecuteNonQuery();
			}
			sqliteTransaction.Commit();
		});
	}

	public void SetKv(string key, string value)
	{
		_writes.Add(() =>
		{
			using SqliteCommand sqliteCommand = _db.CreateCommand();
			sqliteCommand.CommandText = "INSERT INTO kv(key,value) VALUES($k,$v) ON CONFLICT(key) DO UPDATE SET value=excluded.value";
			sqliteCommand.Parameters.AddWithValue("$k", key);
			sqliteCommand.Parameters.AddWithValue("$v", value);
			sqliteCommand.ExecuteNonQuery();
		});
	}

	public string? GetKv(string key)
	{
		using SqliteCommand sqliteCommand = _db.CreateCommand();
		sqliteCommand.CommandText = "SELECT value FROM kv WHERE key=$k";
		sqliteCommand.Parameters.AddWithValue("$k", key);
		return sqliteCommand.ExecuteScalar() as string;
	}

	public void Dispose()
	{
		Flush();
		_writes.CompleteAdding();
		_writer.Join(TimeSpan.FromSeconds(5L));
		_db.Dispose();
	}
}
