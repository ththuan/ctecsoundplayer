using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GSoundPlayer.Audio;
using GSoundPlayer.Model;

namespace GSoundPlayer.Playback;

public sealed class PlayerController
{
	private readonly AudioEngine _engine;

	private readonly Func<double> _now;

	public readonly List<Player> Players = new List<Player>();

	public readonly List<Track> Queue = new List<Track>();

	public readonly List<Track> History = new List<Track>();

	public readonly FxSlot[] Fx = (from i in Enumerable.Range(0, 9)
		select new FxSlot
		{
			Index = i
		}).ToArray();

	private readonly Dictionary<Track, (int Voice, (string, double, double, double, double) Sig)> _prepared = new Dictionary<Track, (int, (string, double, double, double, double))>();

	private readonly List<int> _draining = new List<int>();

	private (int Voice, int Next, double At, double Xf)? _armed;

	public Track? Pick { get; private set; }

	public CutInfo? LastCut { get; set; }

	public bool DuckOn { get; private set; }

	public bool AutoLoud { get; private set; } = true;

	public bool AutoQueue { get; set; } = true;

	public bool RemovePlayed { get; private set; }

	public bool LoopQueue { get; set; } = true;

	public Track? QueueAt { get; private set; }

	public double FadeLen { get; set; } = 3.0;

	public double Xfade { get; set; }

	public double FxFade { get; set; }

	public bool JackLost { get; set; }

	public double GoLock { get; private set; }

	public double GoLockLen { get; private set; } = 1000.0;

	public double NowMs => _now();

	public int QueueAtIndex
	{
		get
		{
			if (QueueAt != null)
			{
				return Queue.IndexOf(QueueAt);
			}
			return -1;
		}
	}

	public bool NextWraps
	{
		get
		{
			Track track = NextQ();
			if (track != null && QueueAtIndex >= 0)
			{
				return Queue.IndexOf(track) <= QueueAtIndex;
			}
			return false;
		}
	}

	public bool QueueDone
	{
		get
		{
			if (Queue.Count > 0 && NextQ() == null)
			{
				return Queue.Any((Track t) => !t.Missing);
			}
			return false;
		}
	}

	public Track? NextUp => Pick ?? NextQ();

	public bool Silent => !Players.Any((Player p) => !p.Fading);

	public event Action<string, ToastKind>? Toast;

	public event Action? Changed;

	public event Action? GoDenied;

	public event Action? GoPressed;

	public event Action? PanicPressed;

	public event Action<Track>? Finished;

	public PlayerController(AudioEngine engine, Func<double>? now = null)
	{
		_engine = engine;
		_now = now ?? ((Func<double>)(() => Environment.TickCount64));
	}

	public Track? NextQ()
	{
		int count = Queue.Count;
		int queueAtIndex = QueueAtIndex;
		for (int i = 1; i <= count; i++)
		{
			int num = queueAtIndex + i;
			if (num >= count)
			{
				if (!LoopQueue)
				{
					return null;
				}
				num -= count;
			}
			if (!Queue[num].Missing)
			{
				return Queue[num];
			}
		}
		return null;
	}

	public Player? ActiveP()
	{
		return Players.LastOrDefault((Player p) => !p.Fading);
	}

	public Player? Current()
	{
		for (int num = Players.Count - 1; num >= 0; num--)
		{
			if (!Players[num].Fading)
			{
				return Players[num];
			}
		}
		return Players.LastOrDefault();
	}

	public IEnumerable<Player> UserFades()
	{
		return Players.Where((Player p) => p.Fade == FadeReason.User);
	}

	public bool IsPlaying(Track t)
	{
		return Players.Any((Player p) => p.Item == t && !p.Fading);
	}

	public bool IsSounding(Track t)
	{
		return Players.Any((Player p) => p.Item == t);
	}

	private void Say(string msg, ToastKind k = ToastKind.Normal)
	{
		Toast?.Invoke(msg, k);
	}

	private void Notify()
	{
		Changed?.Invoke();
	}

	public double LoudGain(Track t)
	{
		if (AutoLoud && t.Loud)
		{
			double? lufs = t.Lufs;
			if (lufs.HasValue)
			{
				double valueOrDefault = lufs.GetValueOrDefault();
				double num = Math.Clamp(-16.0 - valueOrDefault, -20.0, 6.0);
				if (num > 0.0)
				{
					lufs = t.TruePeakDb;
					if (lufs.HasValue)
					{
						double valueOrDefault2 = lufs.GetValueOrDefault();
						num = Math.Min(num, Math.Max(0.0, -1.0 - valueOrDefault2));
					}
				}
				return num;
			}
		}
		return 0.0;
	}

	private double TotalGain(Track t)
	{
		return t.Vol + LoudGain(t);
	}

	private VoiceParams ParamsFor(Track t, double start)
	{
		return new VoiceParams
		{
			InS = t.InP,
			OutS = t.OutP.GetValueOrDefault(),
			FadeInS = t.FadeIn,
			FadeOutS = t.FadeOut,
			GainDb = TotalGain(t),
			StartS = start,
			Bus = 0,
			Loop = 0
		};
	}

	private Player NewPlayer(Track t, double pos, double fadeIn, int? existingVoice = null, bool? loop = null)
	{
		int num = existingVoice ?? _engine.CreateVoice(t.FilePath, ParamsFor(t, pos));
		Player player = new Player
		{
			Item = t,
			Voice = num,
			Pos = pos,
			InP = t.InP,
			OutP = t.Out,
			FadeIn = t.FadeIn,
			FadeOut = t.FadeOut,
			Name = t.Name,
			Warn = t.Warn,
			Gain = ((!(fadeIn > 0.0)) ? 1 : 0),
			Loop = loop,
			StartedAt = _now()
		};
		_engine.SetLoop(num, loop == true);
		_engine.SetGainDb(num, TotalGain(t));
		_engine.Play(num, fadeIn);
		Players.Add(player);
		return player;
	}

	private void StartFade(Player p, double sec, FadeReason reason)
	{
		p.Fade = reason;
		p.Paused = false;
		p.FadeRemain = sec;
		_engine.FadeOut(p.Voice, Math.Max(0.1, sec), (reason == FadeReason.Replace) ? FadeCurve.Power : FadeCurve.Db);
		LastCut = new CutInfo(p.Item, p.Pos, p.Loop);
	}

	private void EndPlayer(Player p, EndWhy why)
	{
		if (Players.Remove(p))
		{
			bool flag = (uint)(why - 1) <= 1u;
			if (flag || (why == EndWhy.Replace && !p.Fading))
			{
				LastCut = new CutInfo(p.Item, p.Pos, p.Loop);
			}
			if ((uint)(why - 1) <= 2u)
			{
				StopThenRelease(p.Voice);
			}
			else
			{
				_engine.Release(p.Voice);
			}
			if (_armed?.Voice == p.Voice)
			{
				_armed = null;
			}
			Notify();
			if (why == EndWhy.End || (why == EndWhy.Faded && p.Fade == FadeReason.None))
			{
				Finished?.Invoke(p.Item);
			}
		}
	}

	private void StopThenRelease(int voice)
	{
		_engine.Stop(voice);
		_draining.Add(voice);
	}

	private bool CrossOut()
	{
		List<Player> list = Players.Where((Player p) => !p.Fading).ToList();
		foreach (Player item in list)
		{
			if (Xfade == 0.0)
			{
				EndPlayer(item, EndWhy.Replace);
			}
			else
			{
				StartFade(item, Xfade, FadeReason.Replace);
			}
		}
		return list.Count > 0;
	}

	public void PlayItem(Track it, bool noHist = false)
	{
		if (JackLost)
		{
			Say("Chưa có Output: cắm lại dây jack trước đã", ToastKind.Warn);
			return;
		}
		if (it.Missing || !File.Exists(it.FilePath))
		{
			it.Missing = true;
			Say("“" + it.Name + "” thiếu file nên chưa phát được", ToastKind.Warn);
			Notify();
			return;
		}
		Took(it);
		bool flag = CrossOut();
		int? existingVoice = null;
		if (_prepared.TryGetValue(it, out (int, (string, double, double, double, double)) value))
		{
			(string, double, double, double, double) item = value.Item2;
			(string, double, double, double, double) signature = it.Signature;
			if (item.Item1 == signature.Item1 && item.Item2 == signature.Item2 && item.Item3 == signature.Item3 && item.Item4 == signature.Item4 && item.Item5 == signature.Item5)
			{
				existingVoice = value.Item1;
				_prepared.Remove(it);
			}
		}
		NewPlayer(it, it.InP, (flag && Xfade > 0.0) ? Xfade : 0.0, existingVoice);
		it.LastPlayed = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		if (!noHist)
		{
			PushHistory(it);
		}
		if (Pick == it)
		{
			Pick = null;
		}
		if (DuckOn)
		{
			ToggleDuck();
			Say("Đã tắt MC NÓI: bài mới phát ở mức thường", ToastKind.Ok);
		}
		Notify();
	}

	private void Took(Track it)
	{
		int num = Queue.IndexOf(it);
		if (num < 0)
		{
			return;
		}
		if (!RemovePlayed)
		{
			QueueAt = it;
			return;
		}
		Queue.RemoveAt(num);
		if (LoopQueue)
		{
			Queue.Add(it);
		}
	}

	private void KeepQueueAt(Func<Track, bool> gone)
	{
		if (QueueAt == null || !gone(QueueAt))
		{
			return;
		}
		int num = Queue.IndexOf(QueueAt);
		Track queueAt = null;
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			if (!gone(Queue[num2]))
			{
				queueAt = Queue[num2];
				break;
			}
		}
		QueueAt = queueAt;
	}

	public void SetRemovePlayed(bool on)
	{
		if (on == RemovePlayed)
		{
			return;
		}
		RemovePlayed = on;
		int queueAtIndex = QueueAtIndex;
		QueueAt = null;
		if (on && queueAtIndex >= 0)
		{
			List<Track> collection = Queue.Take(queueAtIndex + 1).ToList();
			Queue.RemoveRange(0, queueAtIndex + 1);
			if (LoopQueue)
			{
				Queue.AddRange(collection);
			}
		}
		Notify();
	}

	public void SetQueueAt(Track? t)
	{
		QueueAt = ((!RemovePlayed && t != null && Queue.Contains(t)) ? t : null);
		Notify();
	}

	private void PushHistory(Track it)
	{
		History.Add(it);
		if (History.Count > 100)
		{
			History.RemoveAt(0);
		}
	}

	public void Go(bool auto = false)
	{
		double num = _now();
		if (!auto && num < GoLock)
		{
			GoDenied?.Invoke();
			return;
		}
		if (JackLost)
		{
			Say("Chưa có Output: cắm lại dây jack trước đã", ToastKind.Warn);
			return;
		}
		Track nextUp = NextUp;
		if (nextUp == null)
		{
			Say(QueueDone ? "Đã phát hết hàng chờ. Bấm một bài trong kho rồi bấm Space, hoặc bấm ▶ ở dòng bài trong hàng chờ" : "Chưa chọn bài. Bấm một bài trong kho rồi bấm Space", ToastKind.Warn);
			return;
		}
		PlayItem(nextUp);
		if (!auto)
		{
			GoLock = num + 1000.0;
			GoLockLen = 1000.0;
			GoPressed?.Invoke();
		}
		Notify();
	}

	public void LockGo(double ms)
	{
		GoLock = _now() + ms;
		GoLockLen = ms;
	}

	public void FadeOutCurrent()
	{
		Player player = ActiveP();
		if (player != null)
		{
			StartFade(player, FadeLen, FadeReason.User);
		}
		Notify();
	}

	public void EscAction()
	{
		List<Player> list = UserFades().ToList();
		if (list.Count > 0)
		{
			foreach (Player item in list)
			{
				_engine.FadeOut(item.Voice, 0.5, FadeCurve.Db);
				item.FadeRemain = 0.5;
			}
			Say("Tắt nhanh trong nửa giây. Muốn giữ lại nhạc: bấm HỦY trên đồng hồ", ToastKind.Warn);
		}
		else
		{
			FadeOutCurrent();
		}
	}

	public void CancelUserFade()
	{
		List<Player> list = UserFades().ToList();
		if (LastCut != null && list.Any((Player p) => p.Item == LastCut.Item))
		{
			LastCut = null;
		}
		foreach (Player item in list)
		{
			item.Fade = FadeReason.None;
			_engine.CancelFade(item.Voice, 0.5);
		}
		Notify();
		Say("Đã hủy tắt dần, nhạc lên lại", ToastKind.Ok);
	}

	public void StopNow()
	{
		if (Players.Count == 0)
		{
			return;
		}
		Player p = ActiveP();
		CutInfo lastCut = LastCut;
		foreach (Player item in Players.Where((Player q) => q != p).ToList())
		{
			EndPlayer(item, EndWhy.Stop);
		}
		if (p != null)
		{
			EndPlayer(p, EndWhy.Stop);
		}
		else if (lastCut != null)
		{
			LastCut = lastCut;
		}
	}

	public bool StopTrack(Track t)
	{
		List<Player> list = Players.Where((Player p) => p.Item == t && !p.Fading).ToList();
		if (list.Count == 0)
		{
			return true;
		}
		if (list.Any((Player p) => _now() - p.StartedAt < 1000.0))
		{
			return false;
		}
		foreach (Player item in list)
		{
			EndPlayer(item, EndWhy.Stop);
		}
		Say("Đã dừng “" + t.Name + "”. Bấm ⌫ để phát lại từ chỗ vừa dừng");
		return true;
	}

	public void TogglePause()
	{
		if (JackLost)
		{
			Say("Chưa có Output: cắm lại dây jack trước đã", ToastKind.Warn);
			return;
		}
		List<Player> list = Players.Where((Player p) => p.Paused).ToList();
		if (list.Count > 0)
		{
			foreach (Player item in list)
			{
				item.Paused = false;
				double fadeIn = (item.JackPaused ? 2.0 : 0.0);
				item.JackPaused = false;
				_engine.Resume(item.Voice, fadeIn);
			}
		}
		else
		{
			Player player = Current();
			if (player != null && !player.Fading)
			{
				player.Paused = true;
				_engine.Pause(player.Voice);
			}
		}
		Notify();
	}

	public void ToggleDuck()
	{
		DuckOn = !DuckOn;
		_engine.SetDuck(DuckOn);
		Notify();
	}

	public void ToggleLoop()
	{
		Player player = ActiveP();
		if (player == null)
		{
			Say("Chỉ bật lặp được khi đang phát một bài", ToastKind.Warn);
			return;
		}
		player.Loop = !player.LoopOn;
		_engine.SetLoop(player.Voice, player.Loop.Value);
		Say(player.Loop.Value ? "LẶP: hết đoạn quay lại từ đầu, không tự dừng. Bấm L lần nữa để tắt" : "Đã tắt lặp: hết bài sẽ dừng", ToastKind.Ok);
		Notify();
	}

	public void Panic()
	{
		PanicPressed?.Invoke();
		Player player = ActiveP();
		CutInfo cutInfo = ((player != null) ? new CutInfo(player.Item, player.Pos, player.Loop) : null);
		foreach (Player item in Players.ToList())
		{
			EndPlayer(item, EndWhy.Panic);
		}
		if (cutInfo != null)
		{
			LastCut = cutInfo;
		}
		StopAllFx(quiet: true);
		if (DuckOn)
		{
			ToggleDuck();
		}
		Say("ĐÃ DỪNG TẤT CẢ. Bấm ⌫ để phát lại bài vừa tắt", ToastKind.Warn);
		Notify();
	}

	public void ResumeCut()
	{
		CutInfo L = LastCut;
		if (L == null)
		{
			Say("Chưa có bài nào vừa bị tắt", ToastKind.Warn);
			return;
		}
		if (JackLost)
		{
			Say("Chưa có Output: cắm lại dây jack trước đã", ToastKind.Warn);
			return;
		}
		if (L.Item.Missing || !File.Exists(L.Item.FilePath))
		{
			Say("“" + L.Item.Name + "” thiếu file nên chưa phát được", ToastKind.Warn);
			return;
		}
		foreach (Player item in Players.Where((Player p) => p.Item == L.Item).ToList())
		{
			StopThenRelease(item.Voice);
			Players.Remove(item);
		}
		LastCut = null;
		foreach (Player item2 in Players.Where((Player p) => !p.Fading).ToList())
		{
			StartFade(item2, 1.0, FadeReason.Replace);
		}
		NewPlayer(L.Item, L.Pos, 1.0, null, L.Loop);
		if (History.LastOrDefault() != L.Item)
		{
			PushHistory(L.Item);
		}
		Say("Phát lại “" + L.Item.Name + "” từ " + Text.Fmt(L.Pos - L.Item.InP) + ", lên dần 1 giây", ToastKind.Ok);
		Notify();
	}

	public void SetPick(Track? t)
	{
		Pick = ((Pick == t) ? null : t);
		Notify();
	}

	public void ForgetCut(Track t)
	{
		if (LastCut?.Item == t)
		{
			LastCut = null;
			Notify();
		}
	}

	public void ClearPickIf(Track t)
	{
		if (Pick == t)
		{
			Pick = null;
			Notify();
		}
	}

	public void Forget(IEnumerable<Track> tracks)
	{
		HashSet<Track> hashSet = tracks.ToHashSet();
		KeepQueueAt(hashSet.Contains);
		Queue.RemoveAll(hashSet.Contains);
		History.RemoveAll(hashSet.Contains);
		if (Pick != null && hashSet.Contains(Pick))
		{
			Pick = null;
		}
		if (LastCut != null && hashSet.Contains(LastCut.Item))
		{
			LastCut = null;
		}
		foreach (Track item in hashSet)
		{
			if (_prepared.Remove(item, out (int, (string, double, double, double, double)) value))
			{
				if (_armed?.Next == value.Item1)
				{
					_engine.Unfollow(_armed.Value.Voice);
					_armed = null;
				}
				_engine.Release(value.Item1);
			}
		}
		Notify();
	}

	public void Enqueue(Track t)
	{
		if (!Queue.Contains(t))
		{
			Queue.Add(t);
		}
		Say("Đã xếp “" + t.Name + "” vào hàng chờ, thứ " + (Queue.IndexOf(t) + 1), ToastKind.Ok);
		Notify();
	}

	public void MoveToFront(Track t)
	{
		KeepQueueAt((Track q) => q == t);
		Queue.Remove(t);
		Queue.Insert(QueueAtIndex + 1, t);
		Pick = null;
		Say("Đã đưa “" + t.Name + "” lên " + ((QueueAtIndex >= 0) ? "làm bài chờ kế tiếp" : "đầu hàng chờ") + ". Bấm Space để phát", ToastKind.Ok);
		Notify();
	}

	public void SwapQueue(int a, int b)
	{
		if (a >= 0 && b >= 0 && a < Queue.Count && b < Queue.Count)
		{
			List<Track> queue = Queue;
			List<Track> queue2 = Queue;
			Track value = Queue[b];
			Track value2 = Queue[a];
			queue[a] = value;
			queue2[b] = value2;
			Notify();
		}
	}

	public void RemoveQueueAt(int i)
	{
		if (i >= 0 && i < Queue.Count)
		{
			Track t = Queue[i];
			KeepQueueAt((Track q) => q == t);
			Queue.RemoveAt(i);
			Notify();
		}
	}

	public void PlaceInQueue(Track t, int slot)
	{
		int num = Queue.IndexOf(t);
		slot = Math.Clamp(slot, 0, Queue.Count);
		if (num >= 0)
		{
			if (slot == num || slot == num + 1)
			{
				return;
			}
			Queue.RemoveAt(num);
			if (slot > num)
			{
				slot--;
			}
		}
		Queue.Insert(slot, t);
		Notify();
	}

	public void InsertQueue(int i, Track t)
	{
		Queue.Insert(Math.Clamp(i, 0, Queue.Count), t);
		Notify();
	}

	public void ReplaceQueue(IEnumerable<Track> tracks)
	{
		Queue.Clear();
		Queue.AddRange(tracks);
		if (QueueAt != null && !Queue.Contains(QueueAt))
		{
			QueueAt = null;
		}
		Notify();
	}

	public void SeekTo(Player p, double t, bool keepUndo)
	{
		if (keepUndo)
		{
			LastCut = new CutInfo(p.Item, p.Pos, p.Loop);
		}
		double seconds = (p.Pos = Math.Max(p.InP, Math.Min(p.OutP - 0.5, t)));
		p.AutoFired = false;
		_engine.Seek(p.Voice, seconds);
		Notify();
	}

	public void SeekBy(double d)
	{
		Player player = ActiveP();
		if (player != null)
		{
			SeekTo(player, player.Pos + d, keepUndo: false);
		}
	}

	public void PrevTrack()
	{
		if (JackLost)
		{
			Say("Chưa có Output: cắm lại dây jack trước đã", ToastKind.Warn);
			return;
		}
		Player player = ActiveP();
		double num = _now();
		if (player != null && player.Pos - player.InP > 3.0)
		{
			SeekTo(player, player.InP, keepUndo: true);
			Say("Về đầu bài. Nhầm thì bấm nút ↶ để về chỗ cũ");
		}
		else
		{
			if (num < GoLock)
			{
				return;
			}
			Track track = player?.Item;
			object obj;
			if (track == null)
			{
				obj = History.LastOrDefault();
			}
			else if (History.Count < 2)
			{
				obj = null;
			}
			else
			{
				List<Track> history = History;
				obj = history[history.Count - 2];
			}
			Track track2 = (Track)obj;
			if (track2 == null)
			{
				Say("Chưa có bài trước", ToastKind.Warn);
				return;
			}
			if (track != null)
			{
				History.RemoveAt(History.Count - 1);
				if (RemovePlayed)
				{
					Queue.Remove(track);
					Queue.Insert(0, track);
				}
			}
			PlayItem(track2, noHist: true);
			if (track != null && !RemovePlayed)
			{
				int num2 = Queue.IndexOf(track);
				if (num2 >= 0)
				{
					QueueAt = ((num2 > 0) ? Queue[num2 - 1] : null);
				}
				else
				{
					Queue.Insert(QueueAtIndex + 1, track);
				}
				Notify();
			}
			GoLock = num + 1000.0;
			GoLockLen = 1000.0;
		}
	}

	public void SyncItem(Track it)
	{
		foreach (Player item in Players.Where((Player p) => p.Item == it))
		{
			double num = ((it.InP <= item.Pos) ? it.InP : item.InP);
			double num2 = ((it.Out > item.Pos + 1.0) ? it.Out : item.OutP);
			item.Name = it.Name;
			item.InP = num;
			item.OutP = num2;
			item.FadeIn = it.FadeIn;
			item.FadeOut = it.FadeOut;
			item.Warn = it.Warn;
			_engine.SetRegion(item.Voice, num, num2);
			_engine.SetFades(item.Voice, it.FadeIn, it.FadeOut);
			_engine.SetGainDb(item.Voice, TotalGain(it));
		}
		Notify();
	}

	public void SetAutoLoud(bool on)
	{
		AutoLoud = on;
		foreach (Player player in Players)
		{
			_engine.SetGainDb(player.Voice, TotalGain(player.Item));
		}
		Notify();
	}

	public void PlayFx(int index)
	{
		if (index < 0 || index >= Fx.Length || JackLost)
		{
			return;
		}
		FxSlot fxSlot = Fx[index];
		if (fxSlot.Empty)
		{
			Say("Phím " + fxSlot.Key + " chưa có hiệu ứng. Bấm vào ô để chọn file", ToastKind.Warn);
			return;
		}
		if (!fxSlot.Loaded)
		{
			Say("Chưa đọc được file của hiệu ứng “" + fxSlot.Name + "”", ToastKind.Warn);
			return;
		}
		if (fxSlot.Voice != 0)
		{
			StopThenRelease(fxSlot.Voice);
		}
		FxSlot[] fx = Fx;
		foreach (FxSlot fxSlot2 in fx)
		{
			if (fxSlot2 != fxSlot && fxSlot2.Voice != 0)
			{
				if (FxFade <= 0.0)
				{
					StopFx(fxSlot2);
				}
				else if (!fxSlot2.Fading)
				{
					_engine.FadeOut(fxSlot2.Voice, FxFade, FadeCurve.Db);
					fxSlot2.Fading = true;
				}
			}
		}
		fxSlot.Voice = _engine.PlaySample(fxSlot.Sample, 0.0);
		fxSlot.Pos = 0.0;
		fxSlot.Fading = false;
	}

	public void StopFx(FxSlot e)
	{
		if (e.Voice != 0)
		{
			StopThenRelease(e.Voice);
			e.Voice = 0;
			e.Pos = 0.0;
			e.Fading = false;
		}
	}

	public void StopAllFx(bool quiet = false)
	{
		bool flag = false;
		FxSlot[] fx = Fx;
		foreach (FxSlot fxSlot in fx)
		{
			if (fxSlot.Voice != 0)
			{
				flag = true;
				StopFx(fxSlot);
			}
		}
		if (flag && !quiet)
		{
			Say("Đã tắt mọi hiệu ứng");
		}
	}

	public void OnOutputLost()
	{
		if (JackLost)
		{
			return;
		}
		JackLost = true;
		foreach (Player player in Players)
		{
			if (!player.Paused)
			{
				player.Paused = true;
				player.JackPaused = true;
				_engine.Pause(player.Voice);
			}
		}
		StopAllFx(quiet: true);
		ReleasePrepared();
		Notify();
	}

	public void OnOutputBack()
	{
		JackLost = false;
		Notify();
	}

	public void ReleasePrepared()
	{
		foreach (var (_, tuple2) in _prepared)
		{
			_engine.Release(tuple2.Item1);
		}
		_prepared.Clear();
		_armed = null;
	}

	public void Tick()
	{
		for (int num = _draining.Count - 1; num >= 0; num--)
		{
			VoiceState state = _engine.GetInfo(_draining[num]).State;
			if ((state == VoiceState.Invalid || (uint)(state - 5) <= 1u) ? true : false)
			{
				if (state != VoiceState.Invalid)
				{
					_engine.Release(_draining[num]);
				}
				_draining.RemoveAt(num);
			}
		}
		foreach (Player item in Players.ToList())
		{
			VoiceInfo info = _engine.GetInfo(item.Voice);
			if (info.FollowFired != 0 && !item.AutoFired)
			{
				OnFollowFired(item, info.FollowFired);
			}
			VoiceState state2 = info.State;
			if ((state2 == VoiceState.Invalid || (uint)(state2 - 5) <= 1u) ? true : false)
			{
				if (info.State == VoiceState.Error)
				{
					item.Item.Missing = !File.Exists(item.Item.FilePath);
					Say("Không phát được “" + item.Item.Name + "”", ToastKind.Warn);
					EndPlayer(item, EndWhy.Error);
				}
				else
				{
					EndPlayer(item, (info.End == EndReason.Fade) ? EndWhy.Faded : EndWhy.End);
				}
				continue;
			}
			item.Pos = info.PosS;
			item.Gain = info.Gain;
			item.FadeRemain = info.FadeRemainS;
			if (item.OutP <= item.InP && (item.Item.Dur > 0.0 || info.DurS > 0.0))
			{
				item.OutP = ((item.Item.Dur > 0.0) ? item.Item.Dur : info.DurS);
			}
		}
		FxSlot[] fx = Fx;
		foreach (FxSlot fxSlot in fx)
		{
			if (fxSlot.Voice != 0)
			{
				VoiceInfo info2 = _engine.GetInfo(fxSlot.Voice);
				VoiceState state2 = info2.State;
				if ((state2 == VoiceState.Invalid || (uint)(state2 - 5) <= 1u) ? true : false)
				{
					_engine.Release(fxSlot.Voice);
					fxSlot.Voice = 0;
					fxSlot.Pos = 0.0;
					fxSlot.Fading = false;
				}
				else
				{
					fxSlot.Pos = info2.PosS;
				}
			}
		}
		if (!JackLost)
		{
			EnsurePrepared();
			SyncFollow();
		}
	}

	private void OnFollowFired(Player p, int nextVoice)
	{
		p.AutoFired = true;
		if (_armed?.Voice == p.Voice)
		{
			_armed = null;
		}
		Track key = _prepared.FirstOrDefault((KeyValuePair<Track, (int Voice, (string, double, double, double, double) Sig)> kv) => kv.Value.Voice == nextVoice).Key;
		if (key != null)
		{
			_prepared.Remove(key);
			if (Xfade > 0.0)
			{
				p.Fade = FadeReason.Replace;
				LastCut = new CutInfo(p.Item, p.Pos, p.Loop);
			}
			Took(key);
			Player item = new Player
			{
				Item = key,
				Voice = nextVoice,
				Pos = key.InP,
				InP = key.InP,
				OutP = key.Out,
				FadeIn = key.FadeIn,
				FadeOut = key.FadeOut,
				Name = key.Name,
				Warn = key.Warn,
				Gain = ((!(Xfade > 0.0)) ? 1 : 0),
				StartedAt = _now()
			};
			Players.Add(item);
			key.LastPlayed = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			PushHistory(key);
			if (Pick == key)
			{
				Pick = null;
			}
			Notify();
		}
	}

	private void EnsurePrepared()
	{
		Track pick = Pick;
		Track track = NextQ();
		foreach (var (track3, tuple2) in _prepared.ToList())
		{
			if (track3 == pick || track3 == track)
			{
				(string, double, double, double, double) item = tuple2.Item2;
				(string, double, double, double, double) signature = track3.Signature;
				if (!(item.Item1 != signature.Item1) && item.Item2 == signature.Item2 && item.Item3 == signature.Item3 && item.Item4 == signature.Item4 && item.Item5 == signature.Item5)
				{
					_engine.SetGainDb(tuple2.Item1, TotalGain(track3));
					if (_engine.GetInfo(tuple2.Item1).State == VoiceState.Error)
					{
						track3.Missing = true;
						if (_armed?.Next == tuple2.Item1)
						{
							_engine.Unfollow(_armed.Value.Voice);
							_armed = null;
						}
						_engine.Release(tuple2.Item1);
						_prepared.Remove(track3);
						Notify();
					}
					continue;
				}
			}
			if (_armed?.Next == tuple2.Item1)
			{
				_engine.Unfollow(_armed.Value.Voice);
				_armed = null;
			}
			_engine.Release(tuple2.Item1);
			_prepared.Remove(track3);
		}
		Prepare(pick);
		Prepare(track);
	}

	private void Prepare(Track? t)
	{
		if (t != null && !t.Missing && !_prepared.ContainsKey(t))
		{
			int num = _engine.CreateVoice(t.FilePath, ParamsFor(t, t.InP));
			if (num != 0)
			{
				_prepared[t] = (num, t.Signature);
			}
		}
	}

	private void SyncFollow()
	{
		Player player = ActiveP();
		Track track = NextQ();
		(int, int, double, double)? tuple = null;
		if (player != null && track != null && player.OutP > player.InP && AutoQueue && !player.LoopOn && !player.AutoFired && _prepared.TryGetValue(track, out (int, (string, double, double, double, double)) value))
		{
			double num = Math.Min(Xfade, (player.OutP - player.InP) / 2.0);
			tuple = (player.Voice, value.Item1, player.OutP - num, Xfade);
		}
		(int, int, double, double)? tuple2 = tuple;
		(int, int, double, double)? armed = _armed;
		bool hasValue = tuple2.HasValue;
		if (hasValue == armed.HasValue)
		{
			if (!hasValue)
			{
				return;
			}
			(int, int, double, double) valueOrDefault = tuple2.GetValueOrDefault();
			(int, int, double, double) valueOrDefault2 = armed.GetValueOrDefault();
			if (valueOrDefault.Item1 == valueOrDefault2.Item1 && valueOrDefault.Item2 == valueOrDefault2.Item2 && valueOrDefault.Item3 == valueOrDefault2.Item3 && valueOrDefault.Item4 == valueOrDefault2.Item4)
			{
				return;
			}
		}
		armed = _armed;
		if (armed.HasValue)
		{
			(int, int, double, double) valueOrDefault3 = armed.GetValueOrDefault();
			if (!tuple.HasValue || tuple.Value.Item1 != valueOrDefault3.Item1)
			{
				_engine.Unfollow(valueOrDefault3.Item1);
			}
		}
		_armed = tuple;
		if (tuple.HasValue)
		{
			(int, int, double, double) valueOrDefault4 = tuple.GetValueOrDefault();
			_engine.Follow(valueOrDefault4.Item1, valueOrDefault4.Item2, valueOrDefault4.Item3, valueOrDefault4.Item4);
		}
	}

	public void Shutdown()
	{
		foreach (Player item in Players.ToList())
		{
			_engine.Release(item.Voice);
		}
		Players.Clear();
		FxSlot[] fx = Fx;
		foreach (FxSlot fxSlot in fx)
		{
			if (fxSlot.Voice != 0)
			{
				_engine.Release(fxSlot.Voice);
			}
			fxSlot.Voice = 0;
			fxSlot.Fading = false;
		}
		foreach (int item2 in _draining)
		{
			_engine.Release(item2);
		}
		_draining.Clear();
		ReleasePrepared();
	}
}
