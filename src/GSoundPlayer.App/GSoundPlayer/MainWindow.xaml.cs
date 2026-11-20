using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using GSoundPlayer.Audio;
using GSoundPlayer.Controls;
using GSoundPlayer.Library;
using GSoundPlayer.Licensing;
using GSoundPlayer.Model;
using GSoundPlayer.Playback;
using Microsoft.Win32;

namespace GSoundPlayer;

public partial class MainWindow : Window, IComponentConnector
{
	private sealed class PendingDelete
	{
		public List<GSoundPlayer.Model.Track> Tracks = new List<GSoundPlayer.Model.Track>();

		public List<(int Index, GSoundPlayer.Model.Track T)> QueuePos = new List<(int, GSoundPlayer.Model.Track)>();

		public GSoundPlayer.Model.Track? Pick;

		public List<(int Index, string Name, string Color, string? File, double Dur, nint Sample)> Fx = new List<(int, string, string, string, double, nint)>();

		public DispatcherTimer? Timer;
	}

	private sealed record Mi(string Text, Action Do, string? Icon = null, bool Danger = false);

	private sealed class ImpLine
	{
		public required SourceFile File;

		public bool Checked = true;

		public string Name = "";

		public string Tag = "";
	}

	private sealed record PlUndo(List<GSoundPlayer.Model.Track> Queue, List<(GSoundPlayer.Model.Track T, double In, double? Out, double Fi, double Fo, double Vol, int Warn, bool Loud, string Note, bool Edited, bool Auto)> Params, List<(string Name, string Color, string? File, double Dur)>? Fx, string? GroupKey = null, Group? GroupBefore = null, List<GSoundPlayer.Model.Track>? GroupAdded = null);

	private sealed class GroupEd
	{
		public required Group G;

		public required ListBox Left;

		public required ListBox Right;

		public required TextBox LeftFind;

		public required TextBox RightFind;

		public required ComboBox Source;

		public required CheckBox HideIn;

		public required TextBlock Title;

		public required TextBlock LeftHead;

		public required TextBlock RightHead;

		public required TextBlock Status;

		public required Button AddBtn;

		public required Button RemoveBtn;

		public required Button UpBtn;

		public required Button DownBtn;

		public required Button QueueBtn;

		public required Button ReplaceBtn;

		public required List<(string Key, string Label)> Sources;
	}

	private sealed record FxView(Button B, TextBlock Name, TextBlock Dur, Border Fill, Border KeyBox);

	private sealed record FxSaved(int Index, string Name, string Color, string? File, double Dur);

	private sealed record Scrub(Player P, double X0)
	{
		public bool Moved;
	}

	private sealed record SessionData(long[] Queue, long? Pick, long? CurId, double CurPos, long? CutId, double CutPos, long? QueueAt = null);

	private const int UndoMs = 10000;

	private PendingDelete? _pendingDel;

	private Action? _onModalClose;

	private SegGroup _setFade;

	private SegGroup _setXf;

	private SegGroup _setFxFade;

	private SegGroup _setEx;

	private TextBlock _exText;

	private StackPanel _exAskBar;

	private ComboBox _devBox;

	private bool? _exAsk;

	private bool _devFilling;

	private PlUndo? _plUndo;

	private bool _fxLoading;

	private const string PickAdd = "GSoundPlayer.PickAdd";

	private const string PickRemove = "GSoundPlayer.PickRemove";

	private const string PickMove = "GSoundPlayer.PickMove";

	private GroupEd? _ged;

	private const string GroupsKv = "nhom";

	private List<Group> _groups = new List<Group>();

	private double _escGuard;

	private double _typeGuard;

	private double _hintAt = -1000000000.0;

	private DispatcherTimer? _escHold;

	private DispatcherTimer? _keyHold;

	private Key _keyHoldKey;

	private Action? _keyHoldTap;

	private bool _keyHoldFired;

	private bool _enterExtended;

	private readonly Dictionary<string, Button> _chipButtons = new Dictionary<string, Button>();

	private string _queueKey = "";

	private const int TileMax = 18;

	private const string TrackDrag = "GSoundPlayer.Track";

	private readonly FxView[] _fxViews = new FxView[9];

	private const string AudioFilter = "Nhạc và video|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.mp4;*.m4v;*.mov;*.mkv;*.avi;*.wmv;*.3gp|Tất cả|*.*";

	private GSoundPlayer.Model.Track? _editItem;

	private string? _iDrag;

	private SegGroup _iWarn;

	private TextBox _iFi;

	private TextBox _iFo;

	private Slider _iVol;

	private TextBlock _iVolV;

	private TextBlock _iLoudT;

	private CheckBox _iLoud;

	private bool _iFilling;

	private const string Author = "Tiến Vinh | GSound Karaoke";

	private const string ZaloPhone = "0973630052";

	private const string ZaloName = "Bùi Linh";

	private const string TikTokName = "@tienvinhaudio";

	private const string ZaloUrl = "https://zalo.me/0973630052";

	private const string TikTokUrl = "https://www.tiktok.com/@tienvinhaudio";

	private const string Price = "100.000đ";

	private static readonly string[] Perks = new string[3] { "Cập nhật miễn phí mọi bản mới", "Hỗ trợ cài đặt, sử dụng qua Zalo", "Mỗi key dùng cho một máy" };

	private GSoundPlayer.Licensing.LicenseManager _lic;


	private bool _lockShown;

	private bool _licDev;

	private TextBlock _licSetText;

	private readonly double[] _lvl = new double[2];

	private readonly double[] _peak = new double[2];

	private double _duckVisual;

	private string _lastNowKey = "";

	private Scrub? _scrub;

	private double _waveHintAt = -1000000000.0;

	private bool? _goReady;

	private bool _lastJackLost;

	private DispatcherTimer? _panicTimer;

	private double _panicStart;

	private bool _outputBusy;

	private bool _tempOut;

	private int _lastDevEvents;

	private double _lastRetry;

	private int _lastRate;

	private string? _wantDevice;

	private readonly AudioEngine _engine;

	private readonly PlayerController C;

	private readonly LibraryStore _store;

	private readonly MusicLibrary _lib;

	private readonly Settings _set;

	private readonly SourceWatcher _src;

	private readonly DispatcherTimer _timer;

	private readonly TipHost _tips;

	private readonly string[] _args;

	private string _chip = "all";

	private int _sel;

	private List<GSoundPlayer.Model.Track> _view = new List<GSoundPlayer.Model.Track>();

	private bool _needRender = true;

	private double _toastUntil;

	private double _warnUntil;

	private bool _confirmedExit;

	private int _tickCount;

	private string _lastSession = "";

	private string? _startupFile;

	private const uint EsContinuous = 2147483648u;

	private const uint EsSystem = 1u;

	private const uint EsDisplay = 2u;

	private bool _narrow;

	private WindowState _beforeFull;

	private bool _full;

	private Action? _toastClick;

	private bool ModalOpen => Modal.Visibility == Visibility.Visible;

	private bool MenuOpen => MenuLayer.Visibility == Visibility.Visible;

	private void StartPending(PendingDelete p)
	{
		CommitPendingDelete();
		_pendingDel = p;
		p.Timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(10000L)
		};
		p.Timer.Tick += (object? _, EventArgs _) =>
		{
			CommitPendingDelete();
		};
		p.Timer.Start();
	}

	private void CommitPendingDelete()
	{
		PendingDelete pendingDel = _pendingDel;
		if (pendingDel == null)
		{
			return;
		}
		_pendingDel = null;
		pendingDel.Timer?.Stop();
		if (pendingDel.Tracks.Count > 0)
		{
			_lib.Delete(pendingDel.Tracks);
		}
		if (pendingDel.Fx.Count <= 0)
		{
			return;
		}
		foreach (var item in pendingDel.Fx)
		{
			if (item.Sample != 0)
			{
				_engine.FreeSample(item.Sample);
			}
		}
		DeleteUnusedFxFiles(pendingDel.Fx.Select(((int Index, string Name, string Color, string File, double Dur, nint Sample) f) => f.File).OfType<string>());
	}

	private void UndoPendingDelete()
	{
		PendingDelete pendingDel = _pendingDel;
		if (pendingDel == null)
		{
			return;
		}
		_pendingDel = null;
		pendingDel.Timer?.Stop();
		List<GSoundPlayer.Model.Track> list = _lib.Reattach(pendingDel.Tracks);
		foreach (var (val, track) in pendingDel.QueuePos.OrderBy(((int Index, GSoundPlayer.Model.Track T) x) => x.Index))
		{
			if (list.Contains(track) && !C.Queue.Contains(track))
			{
				C.InsertQueue(Math.Min(val, C.Queue.Count), track);
			}
		}
		if (pendingDel.Pick != null && C.Pick == null && list.Contains(pendingDel.Pick))
		{
			C.SetPick(pendingDel.Pick);
		}
		int num = 0;
		foreach (var item6 in pendingDel.Fx)
		{
			FxSlot fxSlot = C.Fx[item6.Index];
			if (!fxSlot.Empty)
			{
				if (item6.Sample != 0)
				{
					_engine.FreeSample(item6.Sample);
				}
				continue;
			}
			FxSlot fxSlot2 = fxSlot;
			FxSlot fxSlot3 = fxSlot;
			FxSlot fxSlot4 = fxSlot;
			FxSlot fxSlot5 = fxSlot;
			string item = item6.Name;
			string item2 = item6.Color;
			string item3 = item6.File;
			double item4 = item6.Dur;
			nint item5 = item6.Sample;
			fxSlot2.Name = item;
			fxSlot3.Color = item2;
			fxSlot4.FilePath = item3;
			fxSlot5.Dur = item4;
			fxSlot.Sample = item5;
			num++;
		}
		if (num > 0)
		{
			SaveFx();
			UpdateFxLabels();
		}
		_needRender = true;
		List<string> list2 = new List<string>();
		if (list.Count > 0)
		{
			list2.Add(list.Count + " bài");
		}
		if (num > 0)
		{
			list2.Add(num + " hiệu ứng");
		}
		ShowToast((list2.Count > 0) ? ("Đã lấy lại " + string.Join(", ", list2)) : "Không lấy lại được: đã có bài, hiệu ứng mới thế chỗ", (list2.Count > 0) ? ToastKind.Ok : ToastKind.Warn);
	}

	private void ConfirmDelete(string title, string body, string okText, Action ok)
	{
		TextBlock textBlock = Ui.T(title, 17.0, "Ink", FontWeights.Bold, trim: false, wrap: true);
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		TextBlock textBlock2 = Ui.T(body, 14.0, "Ink2", null, trim: false, wrap: true);
		Button button = Ui.Btn("Danger", okText, () =>
		{
			CloseModal();
			ok();
		});
		StackPanel stackPanel = Ui.Row(8.0, Ui.Secondary("Hủy", CloseModal), button);
		stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
		stackPanel.Margin = new Thickness(0.0, 18.0, 0.0, 0.0);
		ShowModal(Ui.Col(textBlock, textBlock2, stackPanel), 540.0);
	}

	private void TrackMenu(GSoundPlayer.Model.Track t, FrameworkElement anchor)
	{
		string text = ((t.Name.Length > 40) ? (t.Name.Substring(0, 39) + "…") : t.Name);
		List<Mi> list = new List<Mi>
		{
			new Mi("Sửa bài", () =>
			{
				OpenItemEd(t);
			}, "pencil"),
			new Mi("Xếp vào hàng chờ", () =>
			{
				C.Enqueue(t);
			}, "list-plus"),
			new Mi("Nhóm…", () =>
			{
				TrackGroupMenu(t, anchor);
			}, "folder")
		};
		Group g = FindGroup(_chip);
		if ((object)g != null && t.Tags.Contains(g.Key))
		{
			list.Add(new Mi("Bỏ khỏi nhóm " + g.Name, () =>
			{
				RemoveFromGroup(t, g.Key);
			}, "x"));
		}
		list.Add(new Mi("Xóa khỏi kho nhạc…", () =>
		{
			AskDeleteTrack(t);
		}, "trash", Danger: true));
		ShowMenu(anchor, text.ToUpperInvariant(), list);
	}

	private void OnItemDelete(object s, RoutedEventArgs e)
	{
		if (_editItem != null)
		{
			AskDeleteTrack(_editItem);
		}
	}

	private void AskDeleteTrack(GSoundPlayer.Model.Track t)
	{
		if (C.IsSounding(t))
		{
			ShowToast("“" + t.Name + "” đang phát: tắt bài rồi mới xóa được", ToastKind.Warn);
			return;
		}
		List<string> list = new List<string> { "Bài mất khỏi kho nhạc cùng mọi chỉnh sửa riêng (đoạn phát, fade, âm lượng, ghi chú). Nhầm thì bấm Hoàn tác trong 10 giây." };
		int num = C.Queue.IndexOf(t);
		if (num >= 0)
		{
			list.Add("Bài đang ở hàng chờ, thứ " + (num + 1) + ": sẽ bị bỏ khỏi hàng chờ.");
		}
		list.Add(AppPaths.IsInside(t.FilePath, AppPaths.MusicDir) ? "File nhạc phần mềm đã chép vào thư mục của nó cũng bị xóa." : ("File gốc trên máy không bị xóa: " + t.FilePath));
		ConfirmDelete("Xóa “" + t.Name + "” khỏi kho nhạc?", string.Join("\n\n", list), "Xóa bài", () =>
		{
			DeleteTracks(new GSoundPlayer.Model.Track[1] { t });
		});
	}

	private void AskDeleteAllTracks()
	{
		int count = _lib.Tracks.Count;
		if (count == 0)
		{
			ShowToast("Kho nhạc đang trống", ToastKind.Warn);
			return;
		}
		if (C.Players.Count > 0)
		{
			ShowToast("Đang có nhạc: dừng hết nhạc rồi mới xóa được cả kho", ToastKind.Warn);
			return;
		}
		int num = Enumerable.Count(_lib.Tracks, (GSoundPlayer.Model.Track t) => AppPaths.IsInside(t.FilePath, AppPaths.MusicDir));
		string body = "Kho nhạc và hàng chờ sẽ trống, mọi chỉnh sửa riêng của từng bài mất theo. Nhầm thì bấm Hoàn tác trong 10 giây.\n\n" + ((num > 0) ? (num + " file nhạc phần mềm đã chép vào thư mục của nó cũng bị xóa. ") : "") + "File gốc trên máy không bị xóa. Bảng hiệu ứng giữ nguyên.";
		ConfirmDelete("Xóa tất cả " + count + " bài trong kho nhạc?", body, "Xóa tất cả " + count + " bài", () =>
		{
			DeleteTracks(_lib.Tracks.ToList());
		});
	}

	private void DeleteTracks(IReadOnlyCollection<GSoundPlayer.Model.Track> list)
	{
		HashSet<GSoundPlayer.Model.Track> del = list.Where((GSoundPlayer.Model.Track t) => !C.IsSounding(t)).ToHashSet();
		int num = list.Count - del.Count;
		if (del.Count == 0)
		{
			ShowToast("Bài này vừa được phát nên chưa xóa. Tắt bài rồi xóa lại", ToastKind.Warn);
			return;
		}
		if (_editItem != null && del.Contains(_editItem))
		{
			CloseItemEd();
		}
		if (_plUndo != null)
		{
			_plUndo.Queue.RemoveAll(del.Contains);
			_plUndo.Params.RemoveAll(((GSoundPlayer.Model.Track T, double In, double? Out, double Fi, double Fo, double Vol, int Warn, bool Loud, string Note, bool Edited, bool Auto) tuple) => del.Contains(tuple.T));
		}
		PendingDelete p = new PendingDelete
		{
			Tracks = del.ToList(),
			QueuePos = (from x in C.Queue.Select((GSoundPlayer.Model.Track t, int i) => (i: i, t: t))
				where del.Contains(x.t)
				select x).ToList(),
			Pick = ((C.Pick != null && del.Contains(C.Pick)) ? C.Pick : null)
		};
		C.Forget(del);
		_lib.Detach(del);
		StartPending(p);
		_needRender = true;
		string text = ((del.Count == 1) ? ("Đã xóa “" + del.First().Name + "” khỏi kho nhạc") : ("Đã xóa " + del.Count + " bài khỏi kho nhạc"));
		if (num > 0)
		{
			text = text + " · " + num + " bài đang phát nên giữ lại";
		}
		ShowToast(text, (num > 0) ? ToastKind.Warn : ToastKind.Normal, "Hoàn tác", UndoPendingDelete, 10000);
	}

	private void AskDeleteFx(int idx)
	{
		FxSlot fxSlot = C.Fx[idx];
		if (!fxSlot.Empty)
		{
			ConfirmDelete("Xóa hiệu ứng “" + fxSlot.Name + "” ở phím " + fxSlot.Key + "?", "Phím " + fxSlot.Key + " sẽ trống. Đang kêu thì tắt ngay. File hiệu ứng phần mềm đã chép vào thư mục của nó cũng bị xóa; file gốc đã chọn lúc gán không bị đụng tới. Nhầm thì bấm Hoàn tác trong 10 giây.", "Xóa hiệu ứng", () =>
			{
				DeleteFx(new int[1] { idx });
			});
		}
	}

	private void AskDeleteAllFx()
	{
		List<int> used = (from f in C.Fx
			where !f.Empty
			select f.Index).ToList();
		if (used.Count == 0)
		{
			ShowToast("Bảng hiệu ứng đang trống", ToastKind.Warn);
			return;
		}
		ConfirmDelete("Xóa tất cả " + used.Count + " hiệu ứng?", "Cả 9 phím hiệu ứng sẽ trống. Hiệu ứng đang kêu thì tắt ngay. File hiệu ứng phần mềm đã chép vào thư mục của nó cũng bị xóa; file gốc đã chọn lúc gán không bị đụng tới. Nhầm thì bấm Hoàn tác trong 10 giây.", "Xóa tất cả " + used.Count + " hiệu ứng", () =>
		{
			DeleteFx(used);
		});
	}

	private void DeleteFx(IEnumerable<int> idxs)
	{
		PendingDelete pendingDelete = new PendingDelete();
		string text = null;
		foreach (int idx in idxs)
		{
			FxSlot fxSlot = C.Fx[idx];
			if (!fxSlot.Empty)
			{
				text = fxSlot.Name;
				pendingDelete.Fx.Add((fxSlot.Index, fxSlot.Name, fxSlot.Color, fxSlot.FilePath, fxSlot.Dur, fxSlot.Sample));
				ClearFxSlot(fxSlot, new List<string>(), keepSample: true);
			}
		}
		int count = pendingDelete.Fx.Count;
		if (count != 0)
		{
			StartPending(pendingDelete);
			SaveFx();
			UpdateFxLabels();
			ShowToast((count == 1) ? ("Đã xóa hiệu ứng “" + text + "”") : ("Đã xóa " + count + " hiệu ứng"), ToastKind.Normal, "Hoàn tác", UndoPendingDelete, 10000);
		}
	}

	private void ClearFxSlot(FxSlot e, List<string> oldFiles, bool keepSample = false)
	{
		C.StopFx(e);
		if (e.Sample != 0 && !keepSample)
		{
			_engine.FreeSample(e.Sample);
		}
		e.Sample = 0;
		if (e.FilePath != null)
		{
			oldFiles.Add(e.FilePath);
		}
		e.FilePath = null;
		e.Name = "";
		e.Dur = 0.0;
		e.Color = "green";
	}

	private void DeleteUnusedFxFiles(IEnumerable<string> files)
	{
		IEnumerable<string?> first = C.Fx.Select((FxSlot f) => f.FilePath);
		PlUndo? plUndo = _plUndo;
		object obj;
		if ((object)plUndo == null)
		{
			obj = null;
		}
		else
		{
			List<(string Name, string Color, string? File, double Dur)>? fx = plUndo.Fx;
			obj = ((fx != null) ? fx.Select(((string Name, string Color, string File, double Dur) f) => f.File) : null);
		}
		if (obj == null)
		{
			IEnumerable<string> enumerable = Enumerable.Empty<string>();
			obj = enumerable;
		}
		HashSet<string> keep = first.Concat((IEnumerable<string>)obj).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
		List<string> drop = files.Where((string f) => AppPaths.IsInside(f, AppPaths.FxDir) && !keep.Contains(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (drop.Count > 0)
		{
			Task.Run(() =>
			{
				MusicLibrary.DeleteFiles(drop);
			});
		}
	}

	private void ShowModal(UIElement content, double width, Action? onClose = null)
	{
		if (!_lockShown)
		{
			_ged = null;
			ModalScroll.Content = content;
			ModalBox.Width = Math.Min(width, ActualWidth * 0.94);
			ModalBox.MaxHeight = ActualHeight * 0.9;
			Modal.Visibility = Visibility.Visible;
			_onModalClose = onClose;
			Root.Focus();
		}
	}

	private void CloseModal()
	{
		if (!_lockShown)
		{
			Modal.Visibility = Visibility.Collapsed;
			ModalScroll.Content = null;
			Action? onModalClose = _onModalClose;
			_onModalClose = null;
			onModalClose?.Invoke();
			Root.Focus();
		}
	}

	private void OnModalBackdrop(object s, MouseButtonEventArgs e)
	{
		if (e.OriginalSource == Modal)
		{
			CloseModal();
		}
	}

	private void OnModalBoxDown(object s, MouseButtonEventArgs e)
	{
		e.Handled = false;
	}

	private void ShowMenu(FrameworkElement anchor, string title, IEnumerable<Mi> items)
	{
		MenuBody.Children.Clear();
		TextBlock textBlock = Ui.T(title, 11.5, "Ink2", FontWeights.ExtraBold);
		textBlock.Margin = new Thickness(10.0, 6.0, 10.0, 2.0);
		MenuBody.Children.Add(textBlock);
		List<Mi> list = items.ToList();
		bool flag = list.Any((Mi x) => x.Icon != null);
		bool flag2 = false;
		foreach (Mi item in list)
		{
			if (item.Danger && !flag2 && MenuBody.Children.Count > 1)
			{
				Border border = new Border
				{
					Height = 1.0,
					Margin = new Thickness(6.0, 4.0, 6.0, 4.0)
				};
				border.SetResourceReference(Border.BackgroundProperty, "Line2");
				MenuBody.Children.Add(border);
				flag2 = true;
			}
			string icon = item.Icon;
			FrameworkElement frameworkElement;
			if (icon == null)
			{
				frameworkElement = new FrameworkElement
				{
					Width = (flag ? 16 : 0)
				};
			}
			else
			{
				frameworkElement = ((!icon.StartsWith('#')) ? ((FrameworkElement)Ui.Icon(icon)) : ((FrameworkElement)new Ellipse
				{
					Width = 12.0,
					Height = 12.0,
					Margin = new Thickness(2.0, 0.0, 2.0, 0.0),
					Fill = Ui.Hex(icon)
				}));
			}
			FrameworkElement frameworkElement2 = frameworkElement;
			frameworkElement2.Margin = new Thickness(frameworkElement2.Margin.Left, 0.0, frameworkElement2.Margin.Right + (double)(flag ? 10 : 0), 0.0);
			frameworkElement2.VerticalAlignment = VerticalAlignment.Center;
			StackPanel content = Ui.Row(0.0, frameworkElement2, new TextBlock
			{
				Text = item.Text,
				VerticalAlignment = VerticalAlignment.Center
			});
			Action a = item.Do;
			Button button = Ui.Btn("MenuItemBtn", content, () =>
			{
				CloseMenu();
				a();
			});
			if (item.Danger)
			{
				button.SetResourceReference(Control.ForegroundProperty, "RedText");
			}
			MenuBody.Children.Add(button);
		}
		MenuLayer.Visibility = Visibility.Visible;
		MenuBox.UpdateLayout();
		MenuBox.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		Size desiredSize = MenuBox.DesiredSize;
		Point point = anchor.TranslatePoint(new Point(anchor.ActualWidth, anchor.ActualHeight), Root);
		double length = Math.Max(8.0, Math.Min(Root.ActualWidth - desiredSize.Width - 8.0, point.X - desiredSize.Width));
		double val = ((point.Y + desiredSize.Height + 8.0 > Root.ActualHeight) ? (point.Y - anchor.ActualHeight - desiredSize.Height - 4.0) : (point.Y + 4.0));
		Canvas.SetLeft(MenuBox, length);
		Canvas.SetTop(MenuBox, Math.Max(8.0, val));
	}

	private void CloseMenu()
	{
		MenuLayer.Visibility = Visibility.Collapsed;
	}

	private void OnMenuLayerDown(object s, MouseButtonEventArgs e)
	{
		if (e.OriginalSource == MenuLayer)
		{
			CloseMenu();
		}
	}

	private void AskText(string title, string value, Action<string> done)
	{
		TextBox box = new TextBox
		{
			Text = value,
			Height = 40.0,
			FontSize = 15.0
		};
		box.KeyDown += (object _, KeyEventArgs e) =>
		{
			if (e.Key == Key.Return)
			{
				e.Handled = true;
				Ok();
			}
		};
		StackPanel stackPanel = Ui.Row(8.0, Ui.Secondary("Hủy", CloseModal), Ui.Primary("Lưu", Ok));
		stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
		stackPanel.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		ShowModal(Ui.Col(WithMargin(Ui.T(title, 16.0, "Ink", FontWeights.Bold), 0.0, 0.0, 0.0, 10.0), box, stackPanel), 460.0);
		box.Focus();
		box.SelectAll();
		void Ok()
		{
			string text = box.Text.Trim();
			CloseModal();
			if (text.Length > 0)
			{
				done(text);
			}
		}
	}

	private static FrameworkElement CloseBtn(object content, Action a)
	{
		Button button = Ui.Btn("Btn", content, a);
		button.Height = 30.0;
		button.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
		button.HorizontalAlignment = HorizontalAlignment.Right;
		return button;
	}

	private static FrameworkElement DropTitle(string text)
	{
		StackPanel stackPanel = Ui.Row(0.0, WithMargin(Ui.Icon("download", 20.0), 0.0, 0.0, 8.0, 0.0), Ui.T(text, 17.0, "Ink", FontWeights.Bold));
		stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
		return stackPanel;
	}

	private static Grid HeadRow(string title, FrameworkElement right)
	{
		return new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
			Children = 
			{
				(UIElement)Ui.T(title, 18.0, "Ink", FontWeights.Bold),
				(UIElement)right
			}
		};
	}

	private void BuildSettings()
	{
		TextBlock textBlock = Ui.T("Tùy chỉnh phát", 14.0, "Ink", FontWeights.Bold);
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		SetBody.Children.Add(textBlock);
		_setFade = new SegGroup(("2", "2 s"), ("3", "3 s"), ("5", "5 s"), ("8", "8 s"));
		_setFade.Value = ((int)C.FadeLen).ToString();
		_setFade.Picked += (string v) =>
		{
			C.FadeLen = double.Parse(v);
			_set.FadeLen = C.FadeLen;
			_setFade.Value = v;
			SetFadeLabel(C.FadeLen);
			_set.Save();
		};
		SetBody.Children.Add(SegRow("Tắt dần trong", _setFade));
		_setXf = new SegGroup(("0", "Cắt ngay"), ("2", "2 s"), ("3", "3 s"), ("5", "5 s"), ("8", "8 s"));
		_setXf.Value = ((int)C.Xfade).ToString();
		_setXf.Picked += (string v) =>
		{
			C.Xfade = double.Parse(v);
			_set.Xfade = C.Xfade;
			_setXf.Value = v;
			_set.Save();
		};
		SetBody.Children.Add(SegRow("Chuyển bài", _setXf));
		_setFxFade = new SegGroup(("0", "Tắt ngay"), ("1", "Tắt dần 1 s"));
		_setFxFade.Value = ((C.FxFade > 0.0) ? "1" : "0");
		_setFxFade.Picked += (string v) =>
		{
			C.FxFade = double.Parse(v);
			_set.FxFade = C.FxFade;
			_setFxFade.Value = v;
			_set.Save();
		};
		SetBody.Children.Add(SegRow("Đổi hiệu ứng", _setFxFade));
		_devBox = new ComboBox
		{
			Height = 30.0,
			FontSize = 13.0
		};
		_devBox.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
		{
			OnDevicePicked();
		};
		_devBox.DropDownOpened += (object? _, EventArgs _) =>
		{
			FillDevices();
		};
		SetBody.Children.Add(SegRow("Output", _devBox));
		_setEx = new SegGroup(("1", "Bật"), ("0", "Tắt"));
		_setEx.Value = (_set.Exclusive ? "1" : "0");
		_setEx.Picked += OnExPicked;
		SetBody.Children.Add(SegRow("Chặn app khác", _setEx));
		_exText = Ui.T("", 12.5, "Ink2", null, trim: false, wrap: true);
		Button button = Ui.Btn("HBtn", "Vẫn đổi", () =>
		{
			SetExclusive(_exAsk == true);
		});
		button.Height = 28.0;
		Button button2 = Ui.Btn("HBtn", "Thôi", () =>
		{
			_exAsk = null;
			DrawEx();
		});
		button2.Height = 28.0;
		button2.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
		_exAskBar = Ui.Row(0.0, button, button2);
		_exAskBar.Visibility = Visibility.Collapsed;
		_exAskBar.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
		SetBody.Children.Add(Ui.Col(_exText, _exAskBar));
		Button button3 = Ui.Btn("HBtn", "Xem bản quyền, tác giả…", () =>
		{
			OnLicenseClick(this, new RoutedEventArgs());
		});
		button3.Height = 28.0;
		_licSetText = Ui.T("", 12.5, "Ink2", null, trim: true);
		_licSetText.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
		FrameworkElement frameworkElement = SegRow("Bản quyền", Ui.Row(0.0, button3, _licSetText));
		frameworkElement.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		SetBody.Children.Add(frameworkElement);
		UpdateLicensePill();
		DrawEx();
		static FrameworkElement SegRow(string label, FrameworkElement ctl)
		{
			DockPanel dockPanel = new DockPanel
			{
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
			};
			TextBlock textBlock2 = Ui.T(label, 13.0, "Ink2");
			textBlock2.Width = 108.0;
			DockPanel.SetDock(textBlock2, Dock.Left);
			dockPanel.Children.Add(textBlock2);
			dockPanel.Children.Add(ctl);
			return dockPanel;
		}
	}

	private void BuildMaster()
	{
		MasterSlider.Value = _set.MasterDb;
		UpdateMasterLabel();
		MasterSlider.ValueChanged += (object _, RoutedPropertyChangedEventArgs<double> _) =>
		{
			_set.MasterDb = MasterSlider.Value;
			_engine.SetMasterDb(MasterSlider.Value);
			UpdateMasterLabel();
		};
		MasterSlider.PreviewMouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			_set.Save();
		};
		MasterSlider.PreviewMouseLeftButtonDown += (object _, MouseButtonEventArgs e) =>
		{
			if (e.OriginalSource is DependencyObject d && !IsInside<Thumb>(d))
			{
				e.Handled = true;
				ShowToast("Âm lượng: kéo nút tròn để đổi, bấm vào thanh không đổi", ToastKind.Warn);
			}
		};
	}

	private void UpdateMasterLabel()
	{
		MasterValue.Text = ((MasterSlider.Value == 0.0) ? "0 dB" : ("−" + Math.Abs(MasterSlider.Value) + " dB"));
	}

	private void OnSettingsClick(object s, RoutedEventArgs e)
	{
		_exAsk = null;
		DrawEx();
		FillDevices();
		SetPop.Visibility = ((SetPop.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void FillDevices()
	{
		_devFilling = true;
		_devBox.Items.Clear();
		string outputName = _engine.OutputName;
		foreach (var item2 in _engine.ListDevices())
		{
			string item = item2.Name;
			_devBox.Items.Add(item);
			if (item == outputName)
			{
				_devBox.SelectedItem = item;
			}
		}
		_devFilling = false;
	}

	private async void OnDevicePicked()
	{
		if (_devFilling)
		{
			return;
		}
		object selectedItem = _devBox.SelectedItem;
		string name = selectedItem as string;
		if (name == null || name == _engine.OutputName)
		{
			return;
		}
		if (!C.Silent || _outputBusy)
		{
			_devFilling = true;
			_devBox.SelectedItem = _engine.OutputName;
			_devFilling = false;
			ShowToast("Đang có nhạc: dừng nhạc rồi mới đổi Output", ToastKind.Warn);
			return;
		}
		string previous = _engine.OutputName;
		bool ex = _set.Exclusive;
		_outputBusy = true;
		(int, int) tuple = await Task.Run(() =>
		{
			int num = _engine.OpenOutput(name, ex);
			return (r: num, (num != 0 && previous.Length > 0) ? _engine.OpenOutput(previous, ex) : 0);
		});
		int item = tuple.Item1;
		int item2 = tuple.Item2;
		_outputBusy = false;
		if (item != 0)
		{
			if (item2 != 0)
			{
				ShowOutputFailed();
			}
			else
			{
				UpdateOutPill();
			}
			FillDevices();
			ShowToast("Không mở được “" + name + "”, vẫn dùng “" + previous + "”", ToastKind.Warn);
		}
		else
		{
			_wantDevice = name;
			_set.DeviceName = name;
			_set.Save();
			_tempOut = false;
			AfterOutputOpened();
			if (_engine.Status.ExclusiveFailed != 0)
			{
				ShowToast("Đã chuyển sang “" + name + "”, nhưng không giữ riêng được cổng này nên đang phát chung", ToastKind.Warn);
			}
			else
			{
				ShowToast("Đã chuyển Output sang “" + name + "”", ToastKind.Ok);
			}
		}
	}

	private async Task<bool> ReopenOutputAsync()
	{
		string name = _wantDevice;
		bool ex = _set.Exclusive;
		_outputBusy = true;
		int num = await Task.Run(() => _engine.OpenOutput(name, ex));
		_outputBusy = false;
		if (num != 0)
		{
			ShowOutputFailed();
			return false;
		}
		AfterOutputOpened();
		return true;
	}

	private void AfterOutputOpened()
	{
		UpdateOutPill();
		OutputStatus status = _engine.Status;
		if (status.SampleRate != _lastRate)
		{
			_lastRate = status.SampleRate;
			LoadAllFx();
		}
	}

	private void ShowOutputFailed()
	{
		C.OnOutputLost();
		AlertTitle.Text = "KHÔNG MỞ ĐƯỢC OUTPUT";
		AlertMsg.Text = "Đã tạm dừng mọi bài. Phần mềm đang tự thử mở lại. Nếu vẫn báo, kiểm tra thiết bị trong cài đặt âm thanh của Windows.";
		AlertBar.Visibility = Visibility.Visible;
		PillOutError("Không mở được Output");
	}

	private void OnExPicked(string v)
	{
		bool flag = v == "1";
		if (flag == _set.Exclusive)
		{
			_exAsk = null;
			DrawEx();
		}
		else if (!C.Silent)
		{
			_exAsk = flag;
			DrawEx();
		}
		else
		{
			SetExclusive(flag);
		}
	}

	private async void SetExclusive(bool v)
	{
		if (_outputBusy)
		{
			ShowToast("Output đang được mở, thử lại sau một giây", ToastKind.Warn);
			return;
		}
		_exAsk = null;
		_set.Exclusive = v;
		_set.ExclusiveChosen = true;
		_set.Save();
		DrawEx();
		if (await ReopenOutputAsync())
		{
			if (v && _engine.Status.ExclusiveFailed != 0)
			{
				ShowToast("Không giữ riêng được cổng này (app khác đang giữ, hoặc Windows không cho). Đang phát chung", ToastKind.Warn);
			}
			else
			{
				ShowToast(v ? "Đã bật chặn app khác: tiếng Zalo, Windows không lên mixer" : "Đã tắt chặn app khác: tiếng Zalo, Windows sẽ lên mixer", v ? ToastKind.Ok : ToastKind.Warn);
			}
		}
	}

	private void DrawEx()
	{
		_setEx.Value = (_set.Exclusive ? "1" : "0");
		if (_exAsk.HasValue)
		{
			_exText.Text = "Đang có nhạc: đổi sẽ làm ngắt tiếng một thoáng";
			_exText.SetResourceReference(TextBlock.ForegroundProperty, "AmberText");
			_exAskBar.Visibility = Visibility.Visible;
		}
		else
		{
			_exAskBar.Visibility = Visibility.Collapsed;
			_exText.Text = (_set.Exclusive ? "Tiếng Zalo, Windows không lên mixer. App khác không phát ra được cổng này" : "Phát chung được với app khác (YouTube, PowerPoint). Tiếng Zalo, Windows cũng lên mixer");
			_exText.SetResourceReference(TextBlock.ForegroundProperty, "Ink2");
		}
	}

	private void OnHelpClick(object s, RoutedEventArgs e)
	{
		OpenHelp();
	}

	private void OpenHelp()
	{
		StackPanel element = Ui.Col(Ui.H3("BÀN PHÍM LAPTOP"), K("Space", "GO: phát bài trong ô KẾ TIẾP. Giữ phím không chạy nhiều bài; sau mỗi lần bấm khóa 1 giây"), K("Esc", "Tắt dần bài đang phát. Bấm lần nữa: tắt nhanh trong nửa giây. Muốn hủy: bấm nút HỦY trên đồng hồ"), K("Giữ Esc 1 s", "Dừng tất cả (cũng là F12)"), K("Shift+Esc", "Dừng ngay, không tắt dần"), K("Shift+→", "Phát bài kế tiếp (như Space)"), K("Shift+←", "Về bài trước. Bài đã chạy quá 3 giây thì về đầu bài"), K("⌫ Backspace", "Phát lại bài vừa bị tắt, từ đúng chỗ. Đang có nhạc thì phải giữ nửa giây. Làm lần nữa để về bài trước đó"), K("1 – 9", "Hiệu ứng, luôn phát lại từ đầu"), K("0", "Tắt mọi hiệu ứng"), K("M", "Bật / tắt MC nói (hạ nhạc −12 dB). GO bài mới tự tắt"), K("P", "Tạm dừng / phát tiếp"), K("L", "Lặp bài đang phát (bật / tắt)"), K("↑ ↓", "Chọn bài trong kho nhạc"), K("Bấm 1 bài", "Chọn làm KẾ TIẾP, chưa phát; Space phát. Bấm đúp: phát ngay. ← →: sang cột bên, Enter: chọn"), K("Chuột phải", "Vào bài trong kho: sửa, xếp hàng chờ, nhóm, xóa khỏi kho. Vào chip nhóm: sửa danh sách và thứ tự bài trong nhóm (bấm đúp chip cũng mở), xếp cả nhóm vào hàng chờ, đổi tên, màu, xóa nhóm. Vào ô hiệu ứng: đổi file, chọn cả thư mục, đổi tên, màu, xóa. Xóa nào cũng hỏi lại trước"), K("/ hoặc Ctrl+F", "Tìm bài (hoặc bấm chuột vào ô tìm). Chưa bấm vào ô thì gõ chữ không vào ô"), K("Ctrl+E", "Sửa bài đang chọn"), K("Ctrl+O", "Mở ô nạp nhạc"), K("Ctrl+V", "Dán file đã chép từ thư mục vào kho nhạc"), K("Dạng sóng", "Kéo hoặc bấm đúp để tua. Kéo rồi thả ra ngoài thì hủy"), K("Kéo thả", "Bài từ kho vào hàng chờ; dòng trong hàng chờ lên xuống để đổi chỗ. Bài vào chip nhóm để thêm vào nhóm, vào chip Nhóm (dấu cộng) để tạo nhóm mới. File .gsplayer, .m3u vào cửa sổ: mở ô nhập danh sách phát. Thư mục vào ô hiệu ứng: gán lần lượt vào phím 1–9"));
		StackPanel element2 = Ui.Col(Ui.H3("ĐANG GÕ TRONG Ô TÌM BÀI"), K("Enter", "Đang im thì phát ngay, đang có nhạc thì đưa lên làm bài chờ kế tiếp (Space phát)"), K("Shift+Enter", "Xếp vào hàng chờ"), K("↑ ↓", "Chọn bài trong kết quả"), K("Esc", "Thoát ô tìm. Không tắt nhạc; Esc ngay sau đó cũng không tắt"), K("Alt+M, Alt+P", "Hạ nhạc, tạm dừng khi đang gõ"), K("Alt+1…9", "Hiệu ứng khi đang gõ"), K("Alt+0", "Tắt mọi hiệu ứng khi đang gõ"));
		StackPanel element3 = Ui.Col(Ui.H3("BÀN PHÍM SỐ RỜI (USB)"), K("Enter", "GO"), K(". (dấu chấm)", "Tắt dần / tắt nhanh"), K("1 – 9", "Hiệu ứng"), K("0", "Tắt mọi hiệu ứng"), K("+", "Bật / tắt MC nói"), K("−", "Tạm dừng / phát tiếp"), K("*", "Lặp bài đang phát"), K("/", "Phát lại chỗ vừa tắt (đang có nhạc thì giữ nửa giây)"), Ui.H3("BÚT TRÌNH CHIẾU, BÀN ĐẠP CHÂN"), K("Trang sau", "GO (nút \"next\" của bút bấm slide, bàn đạp USB đặt phím PageDown)"));
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(18.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(18.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		Grid.SetColumn(element2, 2);
		Grid.SetColumn(element3, 4);
		grid.Children.Add(element);
		grid.Children.Add(element2);
		grid.Children.Add(element3);
		ShowModal(Ui.Col(HeadRow("Phím tắt", CloseBtn("Đóng (Esc)", CloseModal)), grid), 1080.0);
		static FrameworkElement K(string key, string what)
		{
			DockPanel dockPanel = new DockPanel
			{
				Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
			};
			TextBlock textBlock = Ui.T(key, 12.5, "Ink", FontWeights.Bold);
			textBlock.Width = 92.0;
			textBlock.VerticalAlignment = VerticalAlignment.Top;
			DockPanel.SetDock(textBlock, Dock.Left);
			dockPanel.Children.Add(textBlock);
			dockPanel.Children.Add(Ui.T(what, 13.5, "Ink2", null, trim: false, wrap: true));
			return dockPanel;
		}
	}

	private void OnImportClick(object s, RoutedEventArgs e)
	{
		OpenImport();
	}

	private void UpdateBadge()
	{
		int zaloCount = _src.ZaloCount;
		ImpBadge.Visibility = ((zaloCount <= 0) ? Visibility.Collapsed : Visibility.Visible);
		ImpBadgeText.Text = zaloCount.ToString();
	}

	private void UpdateImpProgress()
	{
		StatusRight.Text = ((_lib.AnalysisTotal > 0) ? ("Đang phân tích " + _lib.AnalysisDone + "/" + _lib.AnalysisTotal + " bài…") : "");
	}

	private async Task ImportAndReportAsync(IEnumerable<string> paths, MusicLibrary.ImportOptions? opts = null, bool toQueue = false)
	{
		ImportResult importResult = await _lib.ImportAsync(paths, opts);
		if (toQueue)
		{
			foreach (GSoundPlayer.Model.Track item in importResult.Added)
			{
				if (!C.Queue.Contains(item))
				{
					C.Queue.Add(item);
				}
			}
		}
		if (importResult.Added.Count > 0)
		{
			_chip = "new";
			_sel = 0;
		}
		List<string> list = new List<string>();
		if (importResult.Added.Count > 0)
		{
			list.Add("Đã nạp " + importResult.Added.Count + " bài vào kho" + (toQueue ? " và xếp vào hàng chờ" : ""));
		}
		if (importResult.Duplicates > 0)
		{
			list.Add(importResult.Duplicates + " bài đã có, bỏ qua");
		}
		if (importResult.Bad > 0)
		{
			list.Add(importResult.Bad + " file không đọc được");
		}
		if (importResult.NoAudio.Count > 0)
		{
			list.Add(importResult.NoAudio.Count + " video chỉ có hình, không có tiếng (" + string.Join(", ", importResult.NoAudio.Take(2)) + ((importResult.NoAudio.Count > 2) ? "…" : "") + ")");
		}
		ShowToast((list.Count > 0) ? string.Join(" · ", list) : "Không có bài nào để nạp", (importResult.Added.Count > 0 && importResult.NoAudio.Count == 0) ? ToastKind.Ok : ToastKind.Warn);
		UpdateImpProgress();
		_needRender = true;
	}

	private void OpenImport()
	{
		DrawImportMain();
	}

	private void DrawImportMain()
	{
		Button button = Ui.Primary("Chọn file…", () =>
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Chọn file nhạc",
				Multiselect = true,
				Filter = "Nhạc và video|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.mp4;*.m4v;*.mov;*.mkv;*.avi;*.wmv;*.3gp|Tất cả|*.*"
			};
			if (openFileDialog.ShowDialog(this) == true)
			{
				CloseModal();
				ImportAndReportAsync(openFileDialog.FileNames);
			}
		});
		Button button2 = Ui.Primary("Chọn cả thư mục…", () =>
		{
			OpenFolderDialog openFolderDialog = new OpenFolderDialog
			{
				Title = "Chọn thư mục nhạc",
				Multiselect = true
			};
			if (openFolderDialog.ShowDialog(this) == true)
			{
				CloseModal();
				ImportAndReportAsync(openFolderDialog.FolderNames);
			}
		});
		StackPanel stackPanel = Ui.Row(8.0, button, button2);
		stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
		stackPanel.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		FrameworkElement frameworkElement = DropTitle("Kéo thả file hoặc cả thư mục vào đây");
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		TextBlock textBlock = Ui.T("MP3, WAV, FLAC, M4A và video (chỉ giữ phần tiếng). Ở màn hình nào cũng kéo thả hoặc Ctrl+V được, không cần mở ô này. Bài dùng được ngay, tự dọn tên, bỏ bài trùng; bài mới vào “Chưa có nhóm”, kéo bài thả vào chip nhóm để xếp nhóm.", 13.0, "Ink2", null, trim: false, wrap: true);
		textBlock.TextAlignment = TextAlignment.Center;
		textBlock.Margin = new Thickness(0.0, 9.0, 0.0, 0.0);
		Border border = new Border();
		border.BorderThickness = new Thickness(2.0);
		border.CornerRadius = new CornerRadius(10.0);
		border.Padding = new Thickness(16.0);
		border.Child = Ui.Col(frameworkElement, stackPanel, textBlock);
		Border border2 = border;
		border2.SetResourceReference(Border.BorderBrushProperty, "Line2");
		StackPanel stackPanel2 = Ui.Col(HeadRow("Nạp nhạc vào kho", CloseBtn("Đóng (Esc)", CloseModal)), border2);
		IReadOnlyList<Source> readOnlyList = _src.Snapshot();
		if (readOnlyList.Count > 0)
		{
			stackPanel2.Children.Add(Ui.H3("CÓ SẴN ĐỂ NẠP"));
			foreach (Source item in readOnlyList)
			{
				Source so = item;
				stackPanel2.Children.Add(ImpRow(Ui.T(item.Title, 14.0, "Ink", FontWeights.Bold), item.Files.Count + " bài", item.Where, Ui.Primary("Xem và nạp", () =>
				{
					DrawSource(so);
				})));
			}
		}
		stackPanel2.Children.Add(Ui.H3("THƯ MỤC TỰ CẬP NHẬT (CÓ BÀI MỚI LÀ TỰ VÀO KHO)"));
		foreach (string item2 in _set.WatchFolders.ToList())
		{
			string path = item2;
			int num = Enumerable.Count(_lib.Tracks, (GSoundPlayer.Model.Track t) => t.WatchRoot == path);
			stackPanel2.Children.Add(ImpRow(Ui.T(path, 14.0, "Ink", FontWeights.Bold, trim: true), num + " bài", "", Ui.Secondary("Bỏ theo dõi", () =>
			{
				_set.WatchFolders.Remove(path);
				_src.UnwatchFolder(path);
				_set.Save();
				DrawImportMain();
			})));
		}
		Button button3 = Ui.Btn("Secondary", Ui.IconText("plus", "Thêm thư mục"), async () =>
		{
			OpenFolderDialog openFolderDialog = new OpenFolderDialog
			{
				Title = "Chọn thư mục nhạc để theo dõi"
			};
			if (openFolderDialog.ShowDialog(this) == true)
			{
				string folderName = openFolderDialog.FolderName;
				if (!_set.WatchFolders.Contains(folderName, StringComparer.OrdinalIgnoreCase))
				{
					_set.WatchFolders.Add(folderName);
					_set.Save();
					_src.WatchFolder(folderName);
				}
				ImportResult importResult = await _lib.ImportAsync(new string[1] { folderName }, new MusicLibrary.ImportOptions
				{
					WatchRoot = folderName
				});
				UpdateImpProgress();
				DrawImportMain();
				ShowToast("Đã thêm thư mục, nạp " + importResult.Added.Count + " bài. Có bài mới trong đó là tự vào kho", ToastKind.Ok);
			}
		});
		StackPanel stackPanel3 = Ui.Row(10.0, button3, Ui.T("Không chép file, chỉ ghi nhớ đường dẫn. Hợp với kho nhạc lớn trên máy.", 13.5, "Ink2"));
		stackPanel3.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		stackPanel2.Children.Add(stackPanel3);
		ShowModal(stackPanel2, 920.0);
	}

	private static FrameworkElement ImpRow(FrameworkElement title, string count, string where, FrameworkElement btn)
	{
		DockPanel dockPanel = new DockPanel();
		DockPanel.SetDock(btn, Dock.Right);
		dockPanel.Children.Add(btn);
		DockPanel.SetDock(title, Dock.Left);
		dockPanel.Children.Add(title);
		TextBlock textBlock = Ui.T(count, 14.0, "Ink2");
		textBlock.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
		DockPanel.SetDock(textBlock, Dock.Left);
		dockPanel.Children.Add(textBlock);
		dockPanel.Children.Add(WithMargin(Ui.T(where, 13.0, "Ink2", null, trim: true), 10.0, 0.0, 10.0, 0.0));
		Border border = Ui.Card(dockPanel, new Thickness(10.0, 7.0, 10.0, 7.0));
		border.Margin = new Thickness(0.0, 0.0, 0.0, 6.0);
		return border;
	}

	private void DrawSource(Source src)
	{
		List<ImpLine> lines = new List<ImpLine>();
		foreach (SourceFile file in src.Files)
		{
			string item = Text.CleanName(file.FileName).Name;
			lines.Add(new ImpLine
			{
				File = file,
				Name = item
			});
		}
		StackPanel stackPanel = new StackPanel();
		Button go = Ui.Primary("");
		foreach (ImpLine item3 in lines)
		{
			ImpLine line = item3;
			bool item2 = Text.CleanName(item3.File.FileName).Junk;
			List<string> list = new List<string>();
			if (item2)
			{
				list.Add("Tên file khó đọc: gõ tên mới");
			}
			else if (item3.Name != System.IO.Path.GetFileNameWithoutExtension(item3.File.FileName))
			{
				list.Add("Đã dọn tên file");
			}
			if (Catalog.IsVideo(item3.File.FileName))
			{
				list.Add("Video: chỉ giữ phần tiếng");
			}
			CheckBox cb = new CheckBox
			{
				IsChecked = true,
				VerticalAlignment = VerticalAlignment.Center,
				ToolTip = item3.File.FileName
			};
			cb.Click += (object _, RoutedEventArgs _) =>
			{
				line.Checked = cb.IsChecked == true;
				Count();
			};
			TextBox box = new TextBox
			{
				Text = item3.Name,
				Height = 28.0,
				FontSize = 13.5,
				ToolTip = "Tên gốc: " + item3.File.FileName
			};
			box.TextChanged += (object _, TextChangedEventArgs _) =>
			{
				line.Name = box.Text;
			};
			ComboBox tag = new ComboBox
			{
				Height = 28.0,
				FontSize = 13.0
			};
			tag.Items.Add("Chưa có nhóm");
			foreach (Group group in _groups)
			{
				tag.Items.Add(group.Name);
			}
			tag.SelectedIndex = 0;
			tag.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
			{
				line.Tag = ((tag.SelectedIndex > 0) ? _groups[tag.SelectedIndex - 1].Key : "");
			};
			Grid grid = new Grid
			{
				Margin = new Thickness(8.0, 3.0, 8.0, 3.0)
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(26.0)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(130.0)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(220.0)
			});
			grid.Children.Add(cb);
			Grid.SetColumn(box, 1);
			grid.Children.Add(box);
			tag.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
			Grid.SetColumn(tag, 2);
			grid.Children.Add(tag);
			TextBlock textBlock = Ui.T(string.Join(" · ", list), 12.5, item2 ? "AmberText" : "Ink2", item2 ? new FontWeight?(FontWeights.Bold) : ((FontWeight?)null), trim: true);
			textBlock.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
			Grid.SetColumn(textBlock, 3);
			grid.Children.Add(textBlock);
			Border border = new Border
			{
				Child = grid,
				BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0)
			};
			border.SetResourceReference(Border.BorderBrushProperty, "Line");
			stackPanel.Children.Add(border);
		}
		CheckBox copy = new CheckBox
		{
			IsChecked = src.CopyByDefault,
			Content = Ui.T("Chép vào máy (rút USB, xóa tin nhắn vẫn phát được)", 13.5, "Ink2")
		};
		go.Click += (object _, RoutedEventArgs _) =>
		{
			Run(toQ: false);
		};
		Count();
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		StackPanel element = Ui.Row(8.0, Ui.Secondary("Nạp và xếp vào hàng chờ", () =>
		{
			Run(toQ: true);
		}), go);
		DockPanel.SetDock(element, Dock.Right);
		dockPanel.Children.Add(element);
		dockPanel.Children.Add(copy);
		ScrollViewer child = new ScrollViewer
		{
			MaxHeight = ActualHeight * 0.46,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = stackPanel,
			Focusable = false
		};
		ShowModal(Ui.Col(HeadRow(src.Title + " · " + src.Files.Count + " bài", CloseBtn(Ui.IconText("chev-left", "Quay lại", 14.0, 4.0), DrawImportMain)), Ui.Card(child, new Thickness(0.0)), dockPanel), 920.0);
		void Count()
		{
			go.Content = "Nạp " + Enumerable.Count(lines, (ImpLine x) => x.Checked) + " bài vào kho";
		}
		void Run(bool toQ)
		{
			List<ImpLine> list2 = lines.Where((ImpLine x) => x.Checked).ToList();
			if (list2.Count == 0)
			{
				ShowToast("Chưa chọn bài nào", ToastKind.Warn);
			}
			else
			{
				Dictionary<string, string> names = list2.ToDictionary((ImpLine x) => x.File.Path, (ImpLine x) => x.Name.Trim(), StringComparer.OrdinalIgnoreCase);
				Dictionary<string, string> tags = list2.ToDictionary((ImpLine x) => x.File.Path, (ImpLine x) => x.Tag, StringComparer.OrdinalIgnoreCase);
				MusicLibrary.ImportOptions opts = new MusicLibrary.ImportOptions
				{
					CopyToLibrary = (copy.IsChecked == true),
					NameOverride = (string p) => (!names.TryGetValue(p, out var value) || value.Length <= 0) ? Text.CleanName(System.IO.Path.GetFileName(p)).Name : value,
					TagOverride = (string p) => (!tags.TryGetValue(p, out var value) || value.Length <= 0) ? null : new List<string> { value }
				};
				List<string> paths = list2.Select((ImpLine x) => x.File.Path).ToList();
				CloseModal();
				_src.ClearSource(src.Key, paths);
				UpdateBadge();
				ImportAndReportAsync(paths, opts, toQ);
			}
		}
	}

	private void OnListMenu(object s, RoutedEventArgs e)
	{
		List<Mi> list = new List<Mi>
		{
			new Mi("Nhập từ file…", DrawPlaylistPick, "download"),
			new Mi("Xuất ra file…", DrawExport, "upload"),
			new Mi("Sắp xếp kho theo nhóm sự kiện", delegate { GroupEventLibrary(); }, "list-plus"),
			new Mi("Nguồn nhạc & giấy phép", ShowMusicCredits, "alert")
		};
		if (_plUndo != null)
		{
			list.Add(new Mi("Trở lại trước khi nhập", PlaylistUndo, "undo"));
		}
		if (C.Queue.Concat(_lib.Tracks).Any((GSoundPlayer.Model.Track x) => x.Missing))
		{
			list.Add(new Mi("Tìm bài thiếu file", () =>
			{
				DrawMissing(null, () => C.Queue.Concat(_lib.Tracks));
			}, "search"));
		}
		if (_lib.Tracks.Count > 0)
		{
			list.Add(new Mi("Xóa tất cả bài trong kho…", AskDeleteAllTracks, "trash", Danger: true));
		}
		ShowMenu(BtnListMenu, "DANH SÁCH NHẠC", list);
	}

	private void DrawExport()
	{
		DrawExport(useQueue: false);
	}

	private void DrawExport(bool useQueue)
	{
		if (_view.Count == 0 && C.Queue.Count == 0)
		{
			ShowToast("Danh sách đang trống, chưa có gì để xuất", ToastKind.Warn);
			return;
		}
		if (_view.Count == 0)
		{
			useQueue = true;
		}
		if (C.Queue.Count == 0)
		{
			useQueue = false;
		}
		List<GSoundPlayer.Model.Track> tracks = (useQueue ? C.Queue : _view).ToList();
		List<GSoundPlayer.Model.Track> list = tracks.Where((GSoundPlayer.Model.Track t) => t.Missing || !File.Exists(t.FilePath)).ToList();
		List<GSoundPlayer.Model.Track> list2 = tracks.DistinctBy((GSoundPlayer.Model.Track t) => t.FilePath).ToList();
		List<FxSlot> list3 = C.Fx.Where((FxSlot f) => !f.Empty).ToList();
		long bytes = PlaylistIO.EstimateBytes(tracks, C.Fx);
		bool flag = SearchBox.Text.Trim().Length > 0;
		string text = FindGroup(_chip)?.Name ?? Catalog.Chips.FirstOrDefault(((string Key, string Label) c) => c.Key == _chip).Label ?? "";
		string text2;
		if (flag)
		{
			text2 = "Kết quả tìm";
		}
		else
		{
			text2 = ((_chip == "all") ? "Cả kho nhạc" : text);
		}
		string? text3;
		if (useQueue)
		{
			text3 = "Hàng chờ";
		}
		else
		{
			text3 = ((_chip == "all" && !flag) ? "Kho nhạc" : ("Danh sách " + text2));
		}
		string text4 = Text.SafeFileName(text3 + " " + DateTime.Now.ToString("dd-MM"));
		TextBox nameBox = new TextBox
		{
			Text = text4,
			Height = 34.0,
			FontSize = 14.0
		};
		object obj2;
		if (!useQueue && !flag)
		{
			Group obj = FindGroup(_chip);
			if ((object)obj != null)
			{
				obj2 = new PlaylistGroup(obj.Name, obj.Color);
				goto IL_0296;
			}
		}
		obj2 = null;
		goto IL_0296;
		IL_0296:
		PlaylistGroup group = (PlaylistGroup)obj2;
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(6.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		FrameworkElement element = TargetOption("Danh sách nhạc đang hiện", text2 + " · " + _view.Count + " bài", !useQueue, () =>
		{
			DrawExport(useQueue: false);
		});
		FrameworkElement element2 = TargetOption("Hàng chờ", (C.Queue.Count > 0) ? (C.Queue.Count + " bài · " + Text.Fmt(C.Queue.Sum((GSoundPlayer.Model.Track t) => t.Len))) : "Đang trống", useQueue, () =>
		{
			DrawExport(useQueue: true);
		});
		Grid.SetColumn(element2, 2);
		grid.Children.Add(element);
		grid.Children.Add(element2);
		StackPanel stackPanel = Ui.Col(Ui.T(list2.Count + " bài nhạc · " + list3.Count + " hiệu ứng · đủ thông số từng bài" + ((group != null) ? (" · nhóm “" + group.Name + "” đúng thứ tự") : ""), 14.5, "Ink", FontWeights.Bold, trim: false, wrap: true), Ui.T("Gộp trong một file, khoảng " + Math.Max(1L, bytes / 1000000).ToString("N0") + " MB. Chép sang USB, máy dự phòng, mở lên là phát được ngay", 13.5, "Ink2", null, trim: false, wrap: true));
		Button button = Ui.Primary("Xuất file");
		button.IsEnabled = list.Count == 0;
		button.Click += (object _, RoutedEventArgs _) =>
		{
			string text5 = Text.SafeFileName(nameBox.Text);
			if (text5.Length == 0)
			{
				ShowToast("Chưa có tên file", ToastKind.Warn);
			}
			else
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog
				{
					Title = "Lưu file danh sách phát",
					FileName = text5 + ".gsplayer",
					Filter = "Danh sách CTECSoundPlayer|*.gsplayer",
					DefaultExt = ".gsplayer"
				};
				if (saveFileDialog.ShowDialog(this) == true)
				{
					StartExport(saveFileDialog.FileName, text5, bytes, tracks, group);
				}
			}
		};
		StackPanel stackPanel2 = Ui.Row(8.0, Ui.Secondary("Hủy", CloseModal), button);
		stackPanel2.HorizontalAlignment = HorizontalAlignment.Right;
		stackPanel2.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		DockPanel dockPanel = new DockPanel();
		TextBlock textBlock = Ui.T(".gsplayer", 14.0, "Ink2");
		textBlock.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
		DockPanel.SetDock(textBlock, Dock.Right);
		dockPanel.Children.Add(textBlock);
		dockPanel.Children.Add(nameBox);
		StackPanel stackPanel3 = Ui.Col(HeadRow("Xuất danh sách nhạc", CloseBtn("Đóng (Esc)", CloseModal)), Ui.H3("XUẤT DANH SÁCH NÀO"), grid, Ui.H3("TRONG FILE CÓ"), stackPanel);
		if (list.Count > 0)
		{
			stackPanel3.Children.Add(MissBar("Còn " + list.Count + " bài thiếu file", "File xuất phải có đủ nhạc, nên cần tìm lại các bài này trước", Ui.Secondary("Tìm bài thiếu…", () =>
			{
				DrawMissing(() =>
				{
					DrawExport(useQueue);
				}, () => (!useQueue) ? _view : C.Queue);
			})));
		}
		stackPanel3.Children.Add(Ui.H3("TÊN FILE"));
		stackPanel3.Children.Add(dockPanel);
		stackPanel3.Children.Add(stackPanel2);
		ShowModal(stackPanel3, 720.0);
	}

	private static FrameworkElement MissBar(string title, string text, FrameworkElement btn, FrameworkElement? btn2 = null)
	{
		DockPanel dockPanel = new DockPanel();
		if (btn2 != null)
		{
			DockPanel.SetDock(btn2, Dock.Right);
			btn2.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
			dockPanel.Children.Add(btn2);
		}
		DockPanel.SetDock(btn, Dock.Right);
		dockPanel.Children.Add(btn);
		StackPanel stackPanel = Ui.Row(0.0, WithMargin(Ui.Icon("alert"), 0.0, 0.0, 6.0, 0.0), Ui.T(title, 13.5, "Ink", FontWeights.Bold));
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		DockPanel.SetDock(stackPanel, Dock.Left);
		dockPanel.Children.Add(stackPanel);
		dockPanel.Children.Add(WithMargin(Ui.T(text, 12.5, "Ink2", null, trim: false, wrap: true), 10.0, 0.0, 10.0, 0.0));
		Border border = Ui.Card(dockPanel, new Thickness(10.0, 6.0, 10.0, 6.0), "Red", "RedTint");
		border.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
		return border;
	}

	private void StartExport(string path, string name, long bytes, List<GSoundPlayer.Model.Track> tracks, PlaylistGroup? group = null)
	{
		try
		{
			if (new DriveInfo(System.IO.Path.GetPathRoot(path)).AvailableFreeSpace < bytes + 10000000)
			{
				ShowToast("Ổ đĩa này không đủ chỗ: cần khoảng " + (bytes / 1000000).ToString("N0") + " MB", ToastKind.Warn);
				return;
			}
		}
		catch (Exception ex) when ((ex is IOException || ex is ArgumentException) ? true : false)
		{
		}
		CancellationTokenSource cts = new CancellationTokenSource();
		ProgressBar bar = new ProgressBar
		{
			Height = 10.0,
			Minimum = 0.0,
			Maximum = 1.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 8.0)
		};
		bar.SetResourceReference(Control.BackgroundProperty, "Line2");
		bar.SetResourceReference(Control.ForegroundProperty, "Green2");
		TextBlock label = Ui.T("", 14.0, "Ink2");
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		Button element = Ui.Secondary("Hủy", () =>
		{
			cts.Cancel();
		});
		DockPanel.SetDock(element, Dock.Right);
		dockPanel.Children.Add(element);
		dockPanel.Children.Add(Ui.T("Nhạc đang phát không bị ảnh hưởng.", 13.5, "Ink2"));
		ShowModal(Ui.Col(WithMargin(Ui.T("Đang xuất “" + System.IO.Path.GetFileName(path) + "”", 18.0, "Ink", FontWeights.Bold), 0.0, 0.0, 0.0, 6.0), bar, label, dockPanel), 620.0, () =>
		{
			cts.Cancel();
		});
		List<FxSlot> fx = C.Fx.ToList();
		Progress<PlaylistIO.Progress> progress = new Progress<PlaylistIO.Progress>((PlaylistIO.Progress p) =>
		{
			bar.Value = ((p.Total > 0) ? ((double)p.Done / (double)p.Total) : 0.0);
			label.Text = p.Text;
		});
		RunExportAsync();
		async Task RunExportAsync()
		{
			try
			{
				await Task.Run(() =>
				{
					Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
					try
					{
						PlaylistIO.Export(path, name, tracks, fx, progress, cts.Token, group);
					}
					finally
					{
						Thread.CurrentThread.Priority = ThreadPriority.Normal;
					}
				});
				_onModalClose = null;
				CloseModal();
				ShowToast("Đã xuất “" + System.IO.Path.GetFileName(path) + "”: " + tracks.DistinctBy((GSoundPlayer.Model.Track t) => t.FilePath).Count() + " bài, " + Enumerable.Count(fx, (FxSlot f) => !f.Empty) + " hiệu ứng", ToastKind.Ok);
			}
			catch (OperationCanceledException)
			{
				_onModalClose = null;
				if (ModalOpen)
				{
					CloseModal();
				}
				ShowToast("Đã hủy xuất file");
			}
			catch (Exception ex3) when ((ex3 is IOException || ex3 is UnauthorizedAccessException) ? true : false)
			{
				_onModalClose = null;
				if (ModalOpen)
				{
					CloseModal();
				}
				ShowToast("Không ghi được file: " + ex3.Message, ToastKind.Warn);
			}
		}
	}

	private void DrawPlaylistPick()
	{
		Button button = Ui.Primary("Chọn file…", () =>
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Chọn danh sách phát",
				Filter = "Danh sách phát|*.gsplayer;*.m3u;*.m3u8"
			};
			if (openFileDialog.ShowDialog(this) == true)
			{
				OpenPlaylistFile(openFileDialog.FileName);
			}
		});
		button.HorizontalAlignment = HorizontalAlignment.Center;
		button.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		FrameworkElement frameworkElement = DropTitle("Kéo thả file .gsplayer hoặc .m3u vào đây");
		frameworkElement.HorizontalAlignment = HorizontalAlignment.Center;
		TextBlock textBlock = Ui.T("Ở màn hình nào cũng kéo thả được. Chưa có gì thay đổi cho tới khi bấm nút nhập ở bước sau.", 13.0, "Ink2", null, trim: false, wrap: true);
		textBlock.TextAlignment = TextAlignment.Center;
		textBlock.Margin = new Thickness(0.0, 9.0, 0.0, 0.0);
		Border border = new Border();
		border.BorderThickness = new Thickness(2.0);
		border.CornerRadius = new CornerRadius(10.0);
		border.Padding = new Thickness(16.0);
		border.Child = Ui.Col(frameworkElement, button, textBlock);
		Border border2 = border;
		border2.SetResourceReference(Border.BorderBrushProperty, "Line2");
		ShowModal(Ui.Col(HeadRow("Nhập danh sách nhạc", CloseBtn("Đóng (Esc)", CloseModal)), border2), 720.0);
	}

	private async void OpenPlaylistFile(string path)
	{
		try
		{
			Playlist pl = await Task.Run(() => PlaylistIO.Read(path));
			Resolve(pl);
			DrawPreview(pl, "library", useParams: true);
		}
		catch (PlaylistException ex)
		{
			ShowToast(ex.Message, ToastKind.Warn);
		}
		catch (Exception ex2) when ((ex2 is IOException || ex2 is UnauthorizedAccessException) ? true : false)
		{
			ShowToast("Không đọc được file này", ToastKind.Warn);
		}
	}

	private GSoundPlayer.Model.Track? FindInLib(PlaylistItem x)
	{
		if (x.Hash.Length > 0)
		{
			GSoundPlayer.Model.Track track = _lib.FindByHash(x.Hash);
			if (track != null)
			{
				return track;
			}
		}
		string key = Text.Vn(System.IO.Path.GetFileNameWithoutExtension(x.File));
		return _lib.Tracks.FirstOrDefault((GSoundPlayer.Model.Track t) => Text.Vn(System.IO.Path.GetFileNameWithoutExtension(t.FileName)) == key || t.SearchKey == key);
	}

	private void Resolve(Playlist pl)
	{
		foreach (PlaylistItem item in pl.Items)
		{
			if (item.ResolvedPath == null)
			{
				GSoundPlayer.Model.Track track = FindInLib(item);
				if (track != null && File.Exists(track.FilePath))
				{
					item.ResolvedPath = track.FilePath;
				}
			}
		}
	}

	private static bool HasAudio(PlaylistItem x)
	{
		if (x.Entry == null)
		{
			return x.ResolvedPath != null;
		}
		return true;
	}

	private void DrawPreview(Playlist pl, string target, bool useParams)
	{
		int count = C.Queue.Count;
		List<PlaylistItem> miss = pl.Items.Where((PlaylistItem x) => !HasAudio(x)).ToList();
		List<PlaylistItem> list = pl.Items.Where((PlaylistItem x) =>
		{
			if (!x.Plain)
			{
				GSoundPlayer.Model.Track track = FindInLib(x);
				if (track != null)
				{
					return !PlaylistIO.SameParams(track, x);
				}
			}
			return false;
		}).ToList();
		int num = (from x in pl.Items
			where FindInLib(x) == null && x.Entry != null
			select x.Entry).Distinct().Count();
		List<string> list2 = new List<string>
		{
			pl.IsPackage ? "Danh sách phát" : "Danh sách M3U",
			pl.Items.Select((PlaylistItem x) => x.File).Distinct().Count() + " bài"
		};
		DateTime? exported = pl.Exported;
		if (exported.HasValue)
		{
			list2.Add("xuất lúc " + exported.GetValueOrDefault().ToString("HH:mm dd/MM/yyyy"));
		}
		if (pl.IsPackage)
		{
			list2.Add("kèm đủ nhạc" + ((num > 0) ? (", " + num + " bài mới sẽ chép vào kho") : ""));
		}
		if (pl.Effects != null)
		{
			list2.Add(pl.Effects.Count + " hiệu ứng");
		}
		if (pl.Group != null)
		{
			list2.Add("nhóm “" + pl.Group.Name + "”");
		}
		if (!pl.IsPackage)
		{
			list2.Add("không có đoạn phát, fade");
		}
		StackPanel stackPanel = new StackPanel();
		for (int num2 = 0; num2 < pl.Items.Count; num2++)
		{
			PlaylistItem playlistItem = pl.Items[num2];
			bool flag = !HasAudio(playlistItem);
			bool flag2 = FindInLib(playlistItem) != null;
			string text;
			if (flag)
			{
				text = "THIẾU FILE";
			}
			else
			{
				text = (flag2 ? "Có sẵn" : "Bài mới");
			}
			Grid grid = new Grid
			{
				Margin = new Thickness(8.0, 3.0, 8.0, 3.0),
				MinHeight = 22.0
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(26.0)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(50.0)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(84.0)
			});
			grid.Children.Add(Ui.T((num2 + 1).ToString("00"), 13.5, "Ink2"));
			TextBlock textBlock = Ui.T(playlistItem.Name, 13.5, flag ? "RedText" : "Ink", flag ? new FontWeight?(FontWeights.Bold) : ((FontWeight?)null), trim: true);
			textBlock.ToolTip = playlistItem.File;
			Grid.SetColumn(textBlock, 1);
			grid.Children.Add(textBlock);
			TextBlock element = Ui.T((playlistItem.Dur > 0.0) ? Text.Fmt((playlistItem.OutP ?? playlistItem.Dur) - playlistItem.InP) : "", 13.5, "Ink2");
			Grid.SetColumn(element, 2);
			grid.Children.Add(element);
			TextBlock textBlock2 = Ui.T(text, 12.5, flag ? "RedText" : "GreenText", FontWeights.SemiBold);
			textBlock2.HorizontalAlignment = HorizontalAlignment.Right;
			Grid.SetColumn(textBlock2, 3);
			grid.Children.Add(textBlock2);
			Border border = new Border
			{
				Child = grid,
				BorderThickness = new Thickness(0.0, 0.0, 0.0, 1.0)
			};
			border.SetResourceReference(Border.BorderBrushProperty, "Line");
			stackPanel.Children.Add(border);
		}
		ScrollViewer child = new ScrollViewer
		{
			MaxHeight = ActualHeight * 0.3,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = stackPanel,
			Focusable = false
		};
		StackPanel stackPanel2 = Ui.Col(HeadRow("Nhập: " + pl.Name, CloseBtn("Đóng (Esc)", CloseModal)), WithMargin(Ui.T(string.Join(" · ", list2), 14.0, "Ink2", null, trim: false, wrap: true), 0.0, 0.0, 0.0, 8.0), Ui.Card(child, new Thickness(0.0)));
		if (miss.Count > 0)
		{
			stackPanel2.Children.Add(MissBar("Máy này chưa có " + miss.Count + " bài", "Vẫn nhập được: bài thiếu hiện đỏ, chưa phát được tới khi có file", Ui.Secondary("Tìm trong thư mục…", () =>
			{
				FindMissing(miss.Select((PlaylistItem x) => x.File), folder: true, () =>
				{
					Resolve(pl);
					DrawPreview(pl, target, useParams);
				});
			}), Ui.Secondary("Chọn file…", () =>
			{
				FindMissing(miss.Select((PlaylistItem x) => x.File), folder: false, () =>
				{
					Resolve(pl);
					DrawPreview(pl, target, useParams);
				});
			})));
		}
		stackPanel2.Children.Add(Ui.H3("ĐƯA VÀO ĐÂU"));
		Grid grid2 = new Grid();
		grid2.ColumnDefinitions.Add(new ColumnDefinition());
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(6.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition());
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(6.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition());
		FrameworkElement element2 = TargetOption("Chỉ thêm vào kho nhạc", "Hàng chờ không đổi", target == "library", () =>
		{
			DrawPreview(pl, "library", useParams);
		});
		FrameworkElement element3 = TargetOption("Thêm vào cuối hàng chờ", pl.Items.Count + " bài, xếp sau " + count + " bài đang chờ", target == "queueAdd", () =>
		{
			DrawPreview(pl, "queueAdd", useParams);
		});
		FrameworkElement element4 = TargetOption("Thay hàng chờ", (count > 0) ? ("Bỏ " + count + " bài đang chờ") : "Hàng chờ đang trống", target == "queueNew", () =>
		{
			DrawPreview(pl, "queueNew", useParams);
		});
		Grid.SetColumn(element3, 2);
		Grid.SetColumn(element4, 4);
		grid2.Children.Add(element2);
		grid2.Children.Add(element3);
		grid2.Children.Add(element4);
		stackPanel2.Children.Add(grid2);
		CheckBox cbParams = null;
		CheckBox cbFx = null;
		if (list.Count > 0)
		{
			cbParams = new CheckBox
			{
				IsChecked = useParams,
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
				Content = Ui.T("Dùng thông số trong file cho " + list.Count + " bài đang có ở kho (đoạn phát, fade, âm lượng khác máy này)", 13.5, "Ink2", null, trim: false, wrap: true)
			};
			stackPanel2.Children.Add(cbParams);
		}
		if (pl.Effects != null && pl.IsPackage)
		{
			cbFx = new CheckBox
			{
				IsChecked = (target == "queueNew"),
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
				Content = Ui.T("Thay bảng hiệu ứng bằng bảng trong file (" + string.Join(", ", from e in pl.Effects.Take(3)
					select e.Name) + "…). Phím 1–9 sẽ đổi theo", 13.5, "Ink2", null, trim: false, wrap: true)
			};
			stackPanel2.Children.Add(cbFx);
		}
		CheckBox cbGroup = null;
		if (pl.Group != null)
		{
			Group obj = _groups.FirstOrDefault((Group x) => string.Equals(x.Name, pl.Group.Name, StringComparison.OrdinalIgnoreCase));
			bool flag3 = obj == null && _groups.Count >= 12;
			string text2;
			if (flag3)
			{
				text2 = "Đã đủ " + 12 + " nhóm nên không tạo được nhóm “" + pl.Group.Name + "”. Muốn có nhóm thì xóa bớt nhóm không dùng rồi nhập lại";
			}
			else
			{
				text2 = ((obj != null) ? ("Cập nhật nhóm “" + obj.Name + "” đã có: các bài trong file đứng đầu nhóm theo đúng thứ tự trong file, bài khác của nhóm xếp sau") : ("Tạo nhóm “" + pl.Group.Name + "” với các bài trong file, đúng thứ tự trong file"));
			}
			cbGroup = new CheckBox
			{
				IsChecked = !flag3,
				IsEnabled = !flag3,
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
				Content = Ui.T(text2, 13.5, flag3 ? "AmberText" : "Ink2", null, trim: false, wrap: true)
			};
			stackPanel2.Children.Add(cbGroup);
		}
		string text3;
		if (target == "queueNew")
		{
			text3 = "Thay hàng chờ";
		}
		else
		{
			text3 = ((target == "queueAdd") ? "Thêm vào hàng chờ" : "Thêm vào kho nhạc");
		}
		Button button = Ui.Primary(text3, () =>
		{
			ApplyPlaylistAsync(pl, target, cbParams?.IsChecked.GetValueOrDefault() ?? false, cbFx?.IsChecked.GetValueOrDefault() ?? false, cbGroup?.IsChecked.GetValueOrDefault() ?? false);
		});
		StackPanel stackPanel3 = Ui.Row(8.0, Ui.Btn("Secondary", Ui.IconText("chev-left", "Chọn file khác"), DrawPlaylistPick), button);
		stackPanel3.HorizontalAlignment = HorizontalAlignment.Right;
		stackPanel3.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		stackPanel2.Children.Add(stackPanel3);
		ShowModal(stackPanel2, 920.0);
	}

	private static FrameworkElement TargetOption(string title, string sub, bool on, Action pick)
	{
		StackPanel child = Ui.Col(Ui.T(title, 14.0, "Ink", FontWeights.Bold), Ui.T(sub, 12.5, "Ink2"));
		Border border = new Border
		{
			Child = child,
			Padding = new Thickness(10.0, 7.0, 10.0, 7.0),
			CornerRadius = new CornerRadius(8.0),
			BorderThickness = new Thickness(1.0),
			Cursor = Cursors.Hand
		};
		border.SetResourceReference(Border.BorderBrushProperty, on ? "Green" : "Line2");
		if (on)
		{
			border.SetResourceReference(Border.BackgroundProperty, "GreenTint");
		}
		else
		{
			border.Background = Brushes.Transparent;
		}
		border.MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			pick();
		};
		return border;
	}

	private async Task ApplyPlaylistAsync(Playlist pl, string target, bool overwrite, bool useFx, bool useGroup = false)
	{
		PlUndo undo = new PlUndo(C.Queue.ToList(), new List<(GSoundPlayer.Model.Track, double, double?, double, double, double, int, bool, string, bool, bool)>(), useFx ? C.Fx.Select((FxSlot f) => (Name: f.Name, Color: f.Color, FilePath: f.FilePath, Dur: f.Dur)).ToList() : null);
		Dictionary<string, string> entryToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (pl.IsPackage)
		{
			List<(string Entry, string Dest)> extract = new List<(string, string)>();
			foreach (PlaylistItem item in pl.Items.Where((PlaylistItem x) => x.Entry != null && FindInLib(x) == null).DistinctBy((PlaylistItem x) => x.Entry))
			{
				string extension = System.IO.Path.GetExtension(item.Entry);
				string text = MusicLibrary.UniquePath(AppPaths.MusicDir, Text.SafeFileName(System.IO.Path.GetFileNameWithoutExtension(item.File)) + extension);
				entryToPath[item.Entry] = text;
				extract.Add((item.Entry, text));
			}
			if (extract.Count > 0)
			{
				ShowModal(WithMargin(Ui.T("Đang chép " + extract.Count + " bài vào kho…", 16.0, "Ink", FontWeights.Bold), 0.0, 6.0, 0.0, 6.0), 460.0);
				try
				{
					await Task.Run(() =>
					{
						PlaylistIO.Extract(pl.SourcePath, extract, null, CancellationToken.None);
		});
		GroupEventLibrary(false);
	}
				catch (Exception ex) when ((ex is IOException || ex is InvalidDataException || ex is PlaylistException) ? true : false)
				{
					CloseModal();
					ShowToast("File danh sách bị hỏng, không chép được nhạc", ToastKind.Warn);
					return;
				}
			}
		}
		List<string> onDisk = (from x in pl.Items
			where x.ResolvedPath != null && FindInLib(x) == null
			select x.ResolvedPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (onDisk.Count > 0)
		{
			await _lib.ImportAsync(onDisk);
		}
		if (entryToPath.Count > 0)
		{
			Dictionary<string, string> titles = pl.Items.Where((PlaylistItem x) => x.Entry != null && entryToPath.ContainsKey(x.Entry)).GroupBy((PlaylistItem x) => entryToPath[x.Entry], StringComparer.OrdinalIgnoreCase).ToDictionary((IGrouping<string, PlaylistItem> g) => g.Key, (IGrouping<string, PlaylistItem> g) => g.First().Name, StringComparer.OrdinalIgnoreCase);
			await _lib.ImportAsync(entryToPath.Values, new MusicLibrary.ImportOptions
			{
				NameOverride = (string text5) => titles.GetValueOrDefault(text5, System.IO.Path.GetFileNameWithoutExtension(text5))
			});
		}
		if (target == "queueNew")
		{
			C.ReplaceQueue(Array.Empty<GSoundPlayer.Model.Track>());
			C.LockGo(1500.0);
		}
		bool toQueue = target != "library";
		int added = 0;
		int lost = 0;
		List<GSoundPlayer.Model.Track> inFile = new List<GSoundPlayer.Model.Track>();
		foreach (PlaylistItem item2 in pl.Items)
		{
			GSoundPlayer.Model.Track track = FindInLib(item2);
			if (track == null && item2.Entry != null && entryToPath.TryGetValue(item2.Entry, out string p))
			{
				track = _lib.Tracks.FirstOrDefault((GSoundPlayer.Model.Track q) => q.FilePath.Equals(p, StringComparison.OrdinalIgnoreCase));
			}
			if (track != null && !inFile.Contains(track))
			{
				inFile.Add(track);
			}
			if (track == null)
			{
				lost++;
				if (!toQueue)
				{
					continue;
				}
				track = new GSoundPlayer.Model.Track
				{
					Name = item2.Name,
					FilePath = item2.File,
					FileName = item2.File,
					Dur = item2.Dur,
					Missing = true
				};
			}
			else if (!item2.Plain)
			{
				bool flag = entryToPath.Count > 0 && item2.Entry != null && entryToPath.ContainsKey(item2.Entry);
				if (flag || (overwrite && !PlaylistIO.SameParams(track, item2)))
				{
					if (!flag)
					{
						undo.Params.Add((track, track.InP, track.OutP, track.FadeIn, track.FadeOut, track.Vol, track.Warn, track.Loud, track.Note, track.Edited, track.AutoTrim));
					}
					PlaylistIO.ApplyParams(track, item2);
					_lib.Save(track);
					C.SyncItem(track);
				}
			}
			if (toQueue && !C.Queue.Contains(track))
			{
				C.Queue.Add(track);
				added++;
			}
		}
		bool fxChanged = false;
		if (useFx && pl.Effects != null)
		{
			List<(string Entry, string Dest)> extract2 = new List<(string, string)>();
			for (int num = 0; num < 9; num++)
			{
				FxSlot fxSlot = C.Fx[num];
				if (fxSlot.Sample != 0)
				{
					_engine.FreeSample(fxSlot.Sample);
				}
				fxSlot.Sample = 0;
				fxSlot.FilePath = null;
				fxSlot.Name = "";
				fxSlot.Dur = 0.0;
			}
			for (int num2 = 0; num2 < pl.Effects.Count; num2++)
			{
				PlaylistFx playlistFx = pl.Effects[num2];
				if (!string.IsNullOrEmpty(playlistFx.Entry))
				{
					string text2 = System.IO.Path.Combine(AppPaths.FxDir, "nhap_" + Guid.NewGuid().ToString("N").Substring(0, 8) + System.IO.Path.GetExtension(playlistFx.Entry));
					extract2.Add((playlistFx.Entry, text2));
					C.Fx[num2].Name = playlistFx.Name;
					C.Fx[num2].Color = playlistFx.Color;
					C.Fx[num2].Dur = playlistFx.Dur;
					C.Fx[num2].FilePath = text2;
				}
			}
			try
			{
				await Task.Run(() =>
				{
					PlaylistIO.Extract(pl.SourcePath, extract2, null, CancellationToken.None);
				});
				LoadAllFx();
				SaveFx();
				UpdateFxLabels();
				fxChanged = true;
			}
			catch (Exception ex2) when ((ex2 is IOException || ex2 is InvalidDataException || ex2 is PlaylistException) ? true : false)
			{
				ShowToast("Không lấy được bảng hiệu ứng trong file", ToastKind.Warn);
			}
		}
		string text3 = "";
		if (useGroup && pl.Group != null && inFile.Count > 0)
		{
			(string, string, Group, List<GSoundPlayer.Model.Track>) tuple = ApplyPlaylistGroup(pl.Group, inFile);
			text3 = tuple.Item1;
			undo = undo with
			{
				GroupKey = tuple.Item2,
				GroupBefore = tuple.Item3,
				GroupAdded = tuple.Item4
			};
		}
		_plUndo = undo;
		if (ModalOpen)
		{
			CloseModal();
		}
		_needRender = true;
		FlashNext();
		int num3 = entryToPath.Count + onDisk.Count;
		string text4;
		if (toQueue)
		{
			text4 = "Đã xếp " + added + " bài vào hàng chờ" + ((num3 > 0) ? (" · " + num3 + " bài mới vào kho") : "");
		}
		else
		{
			text4 = ((num3 > 0) ? ("Đã thêm " + num3 + " bài vào kho nhạc") : "Các bài trong file đã có sẵn trong kho");
		}
		ShowToast(text4 + text3 + (fxChanged ? " · đã đổi bảng hiệu ứng" : "") + ((lost > 0) ? (" · " + lost + " bài thiếu file") : "") + ". Nhầm thì mở nút ⋯ cạnh nút Nạp để trở lại", (lost <= 0) ? ToastKind.Ok : ToastKind.Warn);
	}

	private void PlaylistUndo()
	{
		PlUndo plUndo = _plUndo;
		if (plUndo == null)
		{
			return;
		}
		C.ReplaceQueue(plUndo.Queue);
		foreach (var item in plUndo.Params)
		{
			item.T.InP = item.In;
			item.T.OutP = item.Out;
			item.T.FadeIn = item.Fi;
			item.T.FadeOut = item.Fo;
			item.T.Vol = item.Vol;
			item.T.Warn = item.Warn;
			item.T.Loud = item.Loud;
			item.T.Note = item.Note;
			item.T.Edited = item.Edited;
			item.T.AutoTrim = item.Auto;
			_lib.Save(item.T);
			C.SyncItem(item.T);
		}
		string gk = plUndo.GroupKey;
		if (gk != null)
		{
			foreach (GSoundPlayer.Model.Track item2 in plUndo.GroupAdded ?? new List<GSoundPlayer.Model.Track>())
			{
				item2.Tags = item2.Tags.Where((string k) => k != gk).ToList();
				_lib.Save(item2);
			}
			int num = _groups.FindIndex((Group x) => x.Key == gk);
			if (num >= 0 && plUndo.GroupBefore == null)
			{
				foreach (GSoundPlayer.Model.Track item3 in _lib.Tracks.Where((GSoundPlayer.Model.Track t) => t.Tags.Contains(gk)))
				{
					item3.Tags = item3.Tags.Where((string k) => k != gk).ToList();
					_lib.Save(item3);
				}
				_groups.RemoveAt(num);
				if (_chip == gk)
				{
					_chip = "all";
				}
			}
			else if (num >= 0)
			{
				_groups[num] = plUndo.GroupBefore;
			}
			SaveGroups();
			BuildChips();
			RenderLib(resetPage: true);
		}
		if (plUndo.Fx != null)
		{
			for (int num2 = 0; num2 < 9 && num2 < plUndo.Fx.Count; num2++)
			{
				FxSlot fxSlot = C.Fx[num2];
				if (fxSlot.Sample != 0)
				{
					_engine.FreeSample(fxSlot.Sample);
				}
				fxSlot.Sample = 0;
				FxSlot fxSlot2 = fxSlot;
				FxSlot fxSlot3 = fxSlot;
				FxSlot fxSlot4 = fxSlot;
				(string, string, string, double) tuple = plUndo.Fx[num2];
				fxSlot.Name = tuple.Item1;
				fxSlot2.Color = tuple.Item2;
				fxSlot3.FilePath = tuple.Item3;
				fxSlot4.Dur = tuple.Item4;
			}
			LoadAllFx();
			SaveFx();
			UpdateFxLabels();
		}
		_plUndo = null;
		C.LockGo(1500.0);
		FlashNext();
		ShowToast("Đã trở lại danh sách trước khi nhập", ToastKind.Ok);
	}

	private void FindMissing(IEnumerable<string> names, bool folder, Action after)
	{
		HashSet<string> want = new HashSet<string>(names.Select((string n) => Text.Vn(System.IO.Path.GetFileNameWithoutExtension(n))));
		string[] picked;
		if (folder)
		{
			OpenFolderDialog openFolderDialog = new OpenFolderDialog
			{
				Title = "Chọn thư mục có các bài thiếu"
			};
			if (openFolderDialog.ShowDialog(this) != true)
			{
				return;
			}
			picked = new string[1] { openFolderDialog.FolderName };
		}
		else
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Title = "Chọn các bài thiếu",
				Multiselect = true,
				Filter = "Nhạc và video|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.mp4;*.m4v;*.mov;*.mkv;*.avi;*.wmv;*.3gp|Tất cả|*.*"
			};
			if (openFileDialog.ShowDialog(this) != true)
			{
				return;
			}
			picked = openFileDialog.FileNames;
		}
		RunFindAsync();
		async Task RunFindAsync()
		{
			List<string> hits = (await Task.Run(() => MusicLibrary.Expand(picked).Audio)).Where((string f) => want.Contains(Text.Vn(System.IO.Path.GetFileNameWithoutExtension(f)))).ToList();
			if (hits.Count > 0)
			{
				await _lib.ImportAsync(hits);
				RelinkMissing();
			}
			after();
			ShowToast((hits.Count > 0) ? ("Đã tìm thấy " + hits.Count + " bài") : "Không thấy bài nào trùng tên file", (hits.Count > 0) ? ToastKind.Ok : ToastKind.Warn);
		}
	}

	private void RelinkMissing()
	{
		for (int i = 0; i < C.Queue.Count; i++)
		{
			GSoundPlayer.Model.Track track = C.Queue[i];
			if (track.Missing)
			{
				string key = Text.Vn(System.IO.Path.GetFileNameWithoutExtension(track.FileName));
				GSoundPlayer.Model.Track track2 = _lib.Tracks.FirstOrDefault((GSoundPlayer.Model.Track t) => !t.Missing && Text.Vn(System.IO.Path.GetFileNameWithoutExtension(t.FileName)) == key);
				if (track2 != null)
				{
					C.Queue[i] = track2;
				}
			}
		}
		_needRender = true;
	}

	private void DrawMissing(Action? then, Func<IEnumerable<GSoundPlayer.Model.Track>> scope)
	{
		List<string> names = (from t in scope()
			where t.Missing || !File.Exists(t.FilePath)
			select t.FileName).Distinct().ToList();
		if (names.Count == 0)
		{
			if (then != null)
			{
				then();
				return;
			}
			if (ModalOpen)
			{
				CloseModal();
			}
			ShowToast("Đủ file nhạc, không mục nào bị thiếu", ToastKind.Ok);
			return;
		}
		StackPanel stackPanel = new StackPanel();
		for (int num = 0; num < names.Count; num++)
		{
			DockPanel dockPanel = new DockPanel
			{
				Margin = new Thickness(8.0, 3.0, 8.0, 3.0)
			};
			TextBlock element = Ui.T("THIẾU FILE", 12.5, "RedText", FontWeights.Bold);
			DockPanel.SetDock(element, Dock.Right);
			dockPanel.Children.Add(element);
			dockPanel.Children.Add(Ui.T((num + 1).ToString("00") + "  " + names[num], 13.5, "RedText", null, trim: true));
			stackPanel.Children.Add(dockPanel);
		}
		DockPanel dockPanel2 = new DockPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		StackPanel element2 = Ui.Row(8.0, Ui.Secondary("Tìm trong thư mục…", () =>
		{
			FindMissing(names, folder: true, After);
		}), Ui.Primary("Chọn file…", () =>
		{
			FindMissing(names, folder: false, After);
		}));
		DockPanel.SetDock(element2, Dock.Right);
		dockPanel2.Children.Add(element2);
		dockPanel2.Children.Add(Ui.T("Tìm thấy file nào, mục đó tự phát được.", 13.5, "Ink2"));
		ScrollViewer child = new ScrollViewer
		{
			MaxHeight = ActualHeight * 0.4,
			Content = stackPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Focusable = false
		};
		ShowModal(Ui.Col(HeadRow("Còn " + names.Count + " bài thiếu file", CloseBtn("Đóng (Esc)", CloseModal)), Ui.Card(child, new Thickness(0.0)), dockPanel2), 720.0);
		void After()
		{
			DrawMissing(then, scope);
		}
	}

	[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
	private static extern int StrCmpLogicalW(string a, string b);

	private void OnFxFolder(object s, RoutedEventArgs e)
	{
		PickFxFolder();
	}

	private void PickFxFolder()
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = "Chọn thư mục hiệu ứng: các file được gán lần lượt vào phím 1–9"
		};
		if (openFolderDialog.ShowDialog(this) == true)
		{
			AskFxFiles(new string[1] { openFolderDialog.FolderName });
		}
	}

	private void AskFxFiles(IReadOnlyList<string> paths)
	{
		if (_fxLoading)
		{
			ShowToast("Đang nạp hiệu ứng, chờ xong đã", ToastKind.Warn);
			return;
		}
		List<string> files = MusicLibrary.Expand(paths).Audio.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		files.Sort((string a, string b) => StrCmpLogicalW(a, b));
		if (files.Count == 0)
		{
			ShowToast("Không thấy file nhạc nào ở đây (mp3, wav, m4a…)", ToastKind.Warn);
			return;
		}
		List<int> free = (from f in C.Fx
			where f.Empty
			select f.Index).ToList();
		List<int> all = Enumerable.Range(0, 9).ToList();
		if (free.Count == 9)
		{
			FillFxAsync(files, all, replaceAll: false);
			return;
		}
		string text = ((paths.Count == 1 && Directory.Exists(paths[0])) ? System.IO.Path.GetFileName(paths[0].TrimEnd(new char[2] { '\\', '/' })) : null);
		IEnumerable<string> values = from f in files.Take(9)
			select Text.FxName(System.IO.Path.GetFileName(f));
		string text2 = files.Count + " file, xếp theo tên: " + string.Join(", ", values) + ((files.Count > 9) ? "… Chỉ lấy 9 file đầu." : ".") + "\n\nBảng đang có " + (9 - free.Count) + " phím đã gán" + ((free.Count > 0) ? (", còn " + free.Count + " phím trống.") : ", không còn phím trống.");
		TextBlock textBlock = Ui.T((text != null) ? ("Gán hiệu ứng từ thư mục “" + text + "”") : ("Gán " + files.Count + " hiệu ứng"), 17.0, "Ink", FontWeights.Bold, trim: false, wrap: true);
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		List<UIElement> list = new List<UIElement> { Ui.Secondary("Hủy", CloseModal) };
		if (free.Count > 0)
		{
			list.Add(Ui.Primary("Điền vào " + free.Count + " phím trống", () =>
			{
				CloseModal();
				FillFxAsync(files, free, replaceAll: false);
			}));
		}
		list.Add(Ui.Btn("Danger", "Thay cả bảng hiệu ứng", () =>
		{
			CloseModal();
			FillFxAsync(files, all, replaceAll: true);
		}));
		StackPanel stackPanel = Ui.Row(8.0, list.ToArray());
		stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
		stackPanel.Margin = new Thickness(0.0, 18.0, 0.0, 0.0);
		ShowModal(Ui.Col(textBlock, Ui.T(text2, 14.0, "Ink2", null, trim: false, wrap: true), stackPanel), 560.0);
	}

	private async Task FillFxAsync(List<string> files, List<int> slots, bool replaceAll)
	{
		_fxLoading = true;
		try
		{
			int n = Math.Min(files.Count, slots.Count);
			ShowToast("Đang nạp " + n + " hiệu ứng…");
			List<string> old = new List<string>();
			List<int> done = new List<int>();
			List<string> bad = new List<string>();
			for (int i = 0; i < n; i++)
			{
				int slot = slots[i];
				string prev = C.Fx[slot].FilePath;
				if (await LoadFxInto(slot, files[i]) != null)
				{
					bad.Add(System.IO.Path.GetFileName(files[i]));
					continue;
				}
				if (prev != null)
				{
					old.Add(prev);
				}
				done.Add(slot);
			}
			if (replaceAll)
			{
				foreach (int item in slots.Skip(n))
				{
					if (!C.Fx[item].Empty)
					{
						ClearFxSlot(C.Fx[item], old);
					}
				}
			}
			SaveFx();
			UpdateFxLabels();
			DeleteUnusedFxFiles(old);
			string text = ((done.Count > 0) ? ("Đã gán " + done.Count + " hiệu ứng vào phím " + KeyList(done)) : "Chưa gán được hiệu ứng nào");
			if (bad.Count > 0)
			{
				text = text + " · " + bad.Count + " file không phát được: " + string.Join(", ", bad.Take(3)) + ((bad.Count > 3) ? "…" : "");
			}
			ShowToast(text, (bad.Count <= 0 && done.Count != 0) ? ToastKind.Ok : ToastKind.Warn);
		}
		finally
		{
			_fxLoading = false;
		}
	}

	private static string KeyList(List<int> slots)
	{
		List<int> list = (from s in slots
			select s + 1 into k
			orderby k
			select k).ToList();
		if (list.Count > 2)
		{
			if (list[list.Count - 1] - list[0] == list.Count - 1)
			{
				return list[0] + "–" + list[list.Count - 1];
			}
		}
		return string.Join(", ", list);
	}

	private void OpenGroupEditor(Group g)
	{
		Group grp = FindGroup(g.Key);
		if ((object)grp == null)
		{
			return;
		}
		double height = Math.Max(240.0, ActualHeight - 380.0);
		TextBlock textBlock = Ui.T("", 18.0, "Ink", FontWeights.Bold, trim: true);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		Button element = Ui.Btn("Primary", Ui.IconText("check", "Xong", 15.0), CloseModal);
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
		};
		DockPanel.SetDock(element, Dock.Right);
		dockPanel.Children.Add(element);
		dockPanel.Children.Add(textBlock);
		TextBlock textBlock2 = Ui.T("Kéo bài từ trái sang phải để thêm vào nhóm, thả vào đúng chỗ trong chương trình; kéo về trái để bỏ. Giữ Ctrl hoặc Shift khi bấm để chọn nhiều bài; bấm đúp một bài để chuyển nhanh sang bên kia. Thay đổi lưu ngay.", 13.0, "Ink2", null, trim: false, wrap: true);
		textBlock2.Margin = new Thickness(0.0, 0.0, 0.0, 12.0);
		List<(string, string)> list = Catalog.Chips.ToList();
		list.AddRange(from x in _groups
			where x.Key != grp.Key
			select (Key: x.Key, "Nhóm " + x.Name));
		ComboBox comboBox = new ComboBox
		{
			Height = 30.0,
			FontSize = 13.0,
			Width = 180.0,
			Margin = new Thickness(0.0, 0.0, 6.0, 0.0)
		};
		foreach (var item6 in list)
		{
			string item = item6.Item2;
			comboBox.Items.Add(item);
		}
		comboBox.SelectedIndex = 0;
		(FrameworkElement Box, TextBox Text) tuple = FindBox("Tìm trong kho…");
		FrameworkElement item2 = tuple.Box;
		TextBox item3 = tuple.Text;
		CheckBox checkBox = new CheckBox
		{
			Content = "Ẩn bài đã có trong nhóm",
			FontSize = 12.5,
			Margin = new Thickness(0.0, 6.0, 0.0, 6.0)
		};
		ListBox left = PickList(height);
		TextBlock textBlock3 = Ui.T("", 13.0, "AmberText", FontWeights.Bold);
		DockPanel dockPanel2 = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
			Height = 26.0
		};
		Button element2 = Ui.Btn("HBtn", "Chọn hết", () =>
		{
			left.SelectAll();
		});
		DockPanel.SetDock(element2, Dock.Right);
		dockPanel2.Children.Add(element2);
		textBlock3.VerticalAlignment = VerticalAlignment.Center;
		dockPanel2.Children.Add(textBlock3);
		DockPanel dockPanel3 = new DockPanel();
		DockPanel.SetDock(comboBox, Dock.Left);
		dockPanel3.Children.Add(comboBox);
		dockPanel3.Children.Add(item2);
		StackPanel element3 = Ui.Col(dockPanel2, dockPanel3, checkBox, left);
		(FrameworkElement Box, TextBox Text) tuple2 = FindBox("Tìm trong nhóm…");
		FrameworkElement item4 = tuple2.Box;
		TextBox item5 = tuple2.Text;
		ListBox right = PickList(height);
		TextBlock textBlock4 = Ui.T("", 13.0, "GreenText", FontWeights.Bold);
		textBlock4.VerticalAlignment = VerticalAlignment.Center;
		Button button = Ui.IconBtn("HBtn", "chev-up", () =>
		{
			GroupEdShift(-1);
		}, "Đưa bài đang chọn lên (Ctrl+↑)", 15.0);
		Button button2 = Ui.IconBtn("HBtn", "chev-down", () =>
		{
			GroupEdShift(1);
		}, "Đưa bài đang chọn xuống (Ctrl+↓)", 15.0);
		Button button3 = Ui.Btn("HBtn", "Chọn hết", () =>
		{
			right.SelectAll();
		});
		double width = (button2.Width = 30.0);
		button.Width = width;
		StackPanel element4 = Ui.Row(4.0, button, button2, button3);
		DockPanel dockPanel4 = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0),
			Height = 26.0
		};
		DockPanel.SetDock(element4, Dock.Right);
		dockPanel4.Children.Add(element4);
		dockPanel4.Children.Add(textBlock4);
		TextBlock textBlock5 = Ui.T("Kéo lên, xuống để đổi thứ tự. Delete: bỏ bài đang chọn khỏi nhóm", 12.5, "Ink2", null, trim: true);
		textBlock5.Margin = new Thickness(0.0, 7.0, 0.0, 7.0);
		Border border = new Border
		{
			Height = 3.0,
			CornerRadius = new CornerRadius(1.5),
			VerticalAlignment = VerticalAlignment.Top,
			IsHitTestVisible = false,
			Visibility = Visibility.Collapsed
		};
		border.SetResourceReference(Border.BackgroundProperty, "Green2");
		Grid grid = new Grid();
		grid.Children.Add(right);
		grid.Children.Add(border);
		StackPanel element5 = Ui.Col(dockPanel4, item4, textBlock5, grid);
		Button button4 = Ui.Btn("Primary", Ui.IconText("chev-right", "Thêm vào nhóm", 15.0, 4.0));
		Button button5 = Ui.Btn("Secondary", Ui.IconText("chev-left", "Bỏ khỏi nhóm", 15.0, 4.0));
		button4.Margin = new Thickness(0.0, 0.0, 0.0, 10.0);
		HorizontalAlignment horizontalAlignment = (button5.HorizontalAlignment = HorizontalAlignment.Stretch);
		button4.HorizontalAlignment = horizontalAlignment;
		StackPanel stackPanel = Ui.Col(button4, button5);
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Margin = new Thickness(14.0, 60.0, 14.0, 0.0);
		Grid grid2 = new Grid();
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Grid.SetColumn(stackPanel, 1);
		Grid.SetColumn(element5, 2);
		grid2.Children.Add(element3);
		grid2.Children.Add(stackPanel);
		grid2.Children.Add(element5);
		TextBlock textBlock6 = Ui.T("", 13.0, "Ink2", null, trim: true);
		textBlock6.VerticalAlignment = VerticalAlignment.Center;
		Button button6 = Ui.Btn("Secondary", Ui.IconText("list-plus", "Xếp cả nhóm vào hàng chờ", 15.0), () =>
		{
			GroupEdQueue(replace: false);
		}, "Thêm các bài của nhóm vào cuối hàng chờ, đúng thứ tự trong nhóm. Bài đã có trong hàng chờ bỏ qua");
		Button button7 = Ui.Btn("Secondary", "Thay hàng chờ bằng nhóm này…", () =>
		{
			GroupEdQueue(replace: true);
		}, "Bỏ hàng chờ hiện tại, xếp các bài của nhóm vào theo đúng thứ tự. Hỏi lại trước khi thay");
		StackPanel element6 = Ui.Row(8.0, button6, button7);
		DockPanel dockPanel5 = new DockPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		DockPanel.SetDock(element6, Dock.Right);
		dockPanel5.Children.Add(element6);
		dockPanel5.Children.Add(textBlock6);
		comboBox.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
		{
			RefreshGroupEditor();
		};
		item3.TextChanged += (object _, TextChangedEventArgs _) =>
		{
			RefreshGroupEditor();
		};
		item5.TextChanged += (object _, TextChangedEventArgs _) =>
		{
			RefreshGroupEditor();
		};
		checkBox.Click += (object _, RoutedEventArgs _) =>
		{
			RefreshGroupEditor();
		};
		left.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
		{
			UpdateGroupEdButtons();
		};
		right.SelectionChanged += (object _, SelectionChangedEventArgs _) =>
		{
			UpdateGroupEdButtons();
		};
		button4.Click += (object _, RoutedEventArgs _) =>
		{
			GroupEdAdd(Picked(left));
		};
		button5.Click += (object _, RoutedEventArgs _) =>
		{
			GroupEdRemove(Picked(right));
		};
		left.MouseDoubleClick += (object _, MouseButtonEventArgs e) =>
		{
			GSoundPlayer.Model.Track track = ItemUnder(left, e);
			if (track != null)
			{
				GroupEdAdd(new GSoundPlayer.Model.Track[1] { track });
			}
		};
		right.MouseDoubleClick += (object _, MouseButtonEventArgs e) =>
		{
			GSoundPlayer.Model.Track track = ItemUnder(right, e);
			if (track != null)
			{
				GroupEdRemove(new GSoundPlayer.Model.Track[1] { track });
			}
		};
		WirePickDrag(left, "GSoundPlayer.PickAdd");
		WirePickDrag(right, "GSoundPlayer.PickRemove", "GSoundPlayer.PickMove");
		WireGroupDrop(right, border);
		WirePickDrop(left, "GSoundPlayer.PickRemove", GroupEdRemove);
		ShowModal(Ui.Col(dockPanel, textBlock2, grid2, dockPanel5), 1180.0, () =>
		{
			_ged = null;
			RenderLib();
		});
		_ged = new GroupEd
		{
			G = grp,
			Left = left,
			Right = right,
			LeftFind = item3,
			RightFind = item5,
			Source = comboBox,
			HideIn = checkBox,
			Title = textBlock,
			LeftHead = textBlock3,
			RightHead = textBlock4,
			Status = textBlock6,
			AddBtn = button4,
			RemoveBtn = button5,
			UpBtn = button,
			DownBtn = button2,
			QueueBtn = button6,
			ReplaceBtn = button7,
			Sources = list
		};
		RefreshGroupEditor();
	}

	private static ListBox PickList(double height)
	{
		ListBox listBox = new ListBox();
		listBox.Height = height;
		listBox.SetResourceReference(FrameworkElement.StyleProperty, "PickList");
		return listBox;
	}

	private static (FrameworkElement Box, TextBox Text) FindBox(string placeholder)
	{
		TextBox box = new TextBox
		{
			Height = 30.0,
			FontSize = 13.5
		};
		StackPanel ph = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			IsHitTestVisible = false,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		IconView iconView = Ui.Icon("search", 14.0);
		iconView.SetResourceReference(IconView.ForegroundProperty, "Muted");
		iconView.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		ph.Children.Add(iconView);
		ph.Children.Add(Ui.T(placeholder, 13.5, "Muted"));
		box.TextChanged += (object _, TextChangedEventArgs _) =>
		{
			ph.Visibility = ((box.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
		};
		return (Box: new Grid
		{
			Children = 
			{
				(UIElement)box,
				(UIElement)ph
			}
		}, Text: box);
	}

	private static List<GSoundPlayer.Model.Track> Picked(ListBox lb)
	{
		HashSet<PickItem> hashSet = lb.SelectedItems.OfType<PickItem>().ToHashSet();
		return (from x in lb.Items.OfType<PickItem>().Where(hashSet.Contains)
			select x.T).ToList();
	}

	private static GSoundPlayer.Model.Track? ItemUnder(ListBox lb, MouseButtonEventArgs e)
	{
		if (!(e.OriginalSource is DependencyObject element) || !(ItemsControl.ContainerFromElement(lb, element) is ListBoxItem { Content: PickItem content }))
		{
			return null;
		}
		return content.T;
	}

	private void RefreshGroupEditor(IReadOnlyCollection<GSoundPlayer.Model.Track>? rightSelect = null)
	{
		GroupEd ed = _ged;
		if (ed == null)
		{
			return;
		}
		string key = ed.G.Key;
		(string, string) tuple = ed.Sources[Math.Max(0, ed.Source.SelectedIndex)];
		string srcKey = tuple.Item1;
		string item = tuple.Item2;
		Group obj = FindGroup(srcKey);
		IEnumerable<GSoundPlayer.Model.Track> source;
		if (!(obj != null))
		{
			IEnumerable<GSoundPlayer.Model.Track> enumerable = _lib.Tracks.Where((GSoundPlayer.Model.Track t) => MusicLibrary.InChip(t, srcKey)).OrderBy((GSoundPlayer.Model.Track t) => t.SearchKey, StringComparer.Ordinal);
			source = enumerable;
		}
		else
		{
			IEnumerable<GSoundPlayer.Model.Track> enumerable = GroupTracks(obj);
			source = enumerable;
		}
		List<GSoundPlayer.Model.Track> list = source.Where((GSoundPlayer.Model.Track t) => Match(t, ed.LeftFind.Text) && (ed.HideIn.IsChecked != true || !t.Tags.Contains(key))).ToList();
		List<GSoundPlayer.Model.Track> list2 = GroupTracks(ed.G);
		Dictionary<GSoundPlayer.Model.Track, int> no = list2.Select((GSoundPlayer.Model.Track t, int i) => (t: t, i: i)).ToDictionary(((GSoundPlayer.Model.Track t, int i) x) => x.t, ((GSoundPlayer.Model.Track t, int i) x) => x.i + 1);
		List<GSoundPlayer.Model.Track> source2 = list2.Where((GSoundPlayer.Model.Track t) => Match(t, ed.RightFind.Text)).ToList();
		SetPickItems(ed.Left, list.Select((GSoundPlayer.Model.Track t) => new PickItem
		{
			T = t,
			In = t.Tags.Contains(key)
		}));
		SetPickItems(ed.Right, source2.Select((GSoundPlayer.Model.Track t) => new PickItem
		{
			T = t,
			No = no[t]
		}), rightSelect);
		ed.Title.Text = "Sửa danh sách nhóm “" + ed.G.Name + "”";
		ed.LeftHead.Text = ((srcKey == "all") ? "TẤT CẢ BÀI" : item.ToUpperInvariant()) + " · " + list.Count + " bài";
		ed.RightHead.Text = "TRONG NHÓM · " + list2.Count + " bài · " + Text.Fmt(list2.Sum((GSoundPlayer.Model.Track t) => t.Len));
		ed.ReplaceBtn.IsEnabled = list2.Count > 0;
		ed.QueueBtn.IsEnabled = list2.Any((GSoundPlayer.Model.Track t) => !C.Queue.Contains(t) && !C.IsSounding(t));
		ed.QueueBtn.ToolTip = ((list2.Count > 0 && !ed.QueueBtn.IsEnabled) ? "Mọi bài của nhóm đã ở trong hàng chờ (hoặc đang phát)" : "Thêm các bài của nhóm vào cuối hàng chờ, đúng thứ tự trong nhóm. Bài đã có trong hàng chờ bỏ qua");
		UpdateGroupEdButtons();
		static bool Match(GSoundPlayer.Model.Track t, string q)
		{
			return Text.Vn(q.Trim()).Split(' ', StringSplitOptions.RemoveEmptyEntries).All((string w) => t.SearchKey.Contains(w, StringComparison.Ordinal));
		}
	}

	private static void SetPickItems(ListBox lb, IEnumerable<PickItem> items, IReadOnlyCollection<GSoundPlayer.Model.Track>? select = null)
	{
		ScrollViewer sv = FindScroller(lb);
		double off = sv?.VerticalOffset ?? 0.0;
		HashSet<GSoundPlayer.Model.Track> keep = (select ?? Picked(lb)).ToHashSet();
		List<PickItem> list = items.ToList();
		lb.ItemsSource = list;
		List<PickItem> chosen = list.Where((PickItem x) => keep.Contains(x.T)).ToList();
		if (chosen.Count == list.Count && chosen.Count > 0)
		{
			lb.SelectAll();
		}
		else
		{
			foreach (PickItem item in chosen)
			{
				lb.SelectedItems.Add(item);
			}
		}
		if (sv != null && off > 0.0)
		{
			lb.Dispatcher.BeginInvoke((Action)(() =>
			{
				sv.ScrollToVerticalOffset(off);
			}), DispatcherPriority.Loaded);
		}
		if (select != null && chosen.Count > 0)
		{
			lb.Dispatcher.BeginInvoke((Action)(() =>
			{
				lb.ScrollIntoView(chosen[0]);
			}), DispatcherPriority.Loaded);
		}
	}

	private static ScrollViewer? FindScroller(DependencyObject d)
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(d, i);
			if (child is ScrollViewer result)
			{
				return result;
			}
			ScrollViewer scrollViewer = FindScroller(child);
			if (scrollViewer != null)
			{
				return scrollViewer;
			}
		}
		return null;
	}

	private void UpdateGroupEdButtons()
	{
		GroupEd ged = _ged;
		if (ged != null)
		{
			ged.AddBtn.IsEnabled = ged.Left.SelectedItems.OfType<PickItem>().Any((PickItem x) => !x.In);
			ged.RemoveBtn.IsEnabled = ged.Right.SelectedItems.Count > 0;
			List<GSoundPlayer.Model.Track> list = Picked(ged.Right);
			List<GSoundPlayer.Model.Track> list2 = ((list.Count > 0) ? GroupTracks(ged.G) : new List<GSoundPlayer.Model.Track>());
			ged.UpBtn.IsEnabled = list.Count > 0 && !GroupOrder.Shift(list2, list, -1).SequenceEqual(list2);
			ged.DownBtn.IsEnabled = list.Count > 0 && !GroupOrder.Shift(list2, list, 1).SequenceEqual(list2);
		}
	}

	private void GroupEdAdd(IEnumerable<GSoundPlayer.Model.Track> tracks, int? at = null)
	{
		GroupEd ed = _ged;
		if (ed != null)
		{
			List<GSoundPlayer.Model.Track> list = (from t in tracks.Distinct()
				where !t.Tags.Contains(ed.G.Key)
				select t).ToList();
			int num = PutInGroup(ed.G, list, at);
			string text = "";
			if (num > 0 && at.HasValue)
			{
				int num2 = GroupTracks(ed.G).IndexOf(list[0]) + 1;
				text = ((num == 1) ? (", thành bài thứ " + num2) : (", từ bài thứ " + num2));
			}
			ed.Status.Text = ((num > 0) ? ("Đã thêm " + num + " bài vào nhóm" + text) : "Các bài này đã có trong nhóm");
			RefreshGroupEditor((num > 0) ? list : null);
		}
	}

	private void GroupEdRemove(IEnumerable<GSoundPlayer.Model.Track> tracks)
	{
		GroupEd ged = _ged;
		if (ged != null)
		{
			int num = TakeFromGroup(ged.G, tracks);
			ged.Status.Text = ((num > 0) ? ("Đã bỏ " + num + " bài khỏi nhóm (bài vẫn còn trong kho)") : "");
			RefreshGroupEditor();
		}
	}

	private void GroupEdMove(IEnumerable<GSoundPlayer.Model.Track> tracks, int at)
	{
		GroupEd ed = _ged;
		if (ed == null)
		{
			return;
		}
		List<GSoundPlayer.Model.Track> list = GroupTracks(ed.G);
		List<GSoundPlayer.Model.Track> list2 = tracks.Where((GSoundPlayer.Model.Track t) => t.Tags.Contains(ed.G.Key)).ToList();
		if (list2.Count != 0)
		{
			List<GSoundPlayer.Model.Track> list3 = GroupOrder.Place(list, list2, at);
			if (!list3.SequenceEqual(list))
			{
				SetGroupOrder(ed.G, list3);
				ed.Status.Text = ((list2.Count == 1) ? ("Đã dời “" + list2[0].Name + "” thành bài thứ " + (list3.IndexOf(list2[0]) + 1)) : ("Đã dời " + list2.Count + " bài, từ bài thứ " + (list3.IndexOf(list2[0]) + 1)));
				RefreshGroupEditor(list2);
			}
		}
	}

	private void GroupEdShift(int dir)
	{
		GroupEd ged = _ged;
		if (ged == null)
		{
			return;
		}
		List<GSoundPlayer.Model.Track> list = Picked(ged.Right);
		if (list.Count != 0)
		{
			List<GSoundPlayer.Model.Track> list2 = GroupTracks(ged.G);
			List<GSoundPlayer.Model.Track> list3 = GroupOrder.Shift(list2, list, dir);
			if (!list3.SequenceEqual(list2))
			{
				SetGroupOrder(ged.G, list3);
				ged.Status.Text = ((list.Count == 1) ? ("“" + list[0].Name + "” giờ là bài thứ " + (list3.IndexOf(list[0]) + 1)) : ("Đã dời " + list.Count + " bài"));
				RefreshGroupEditor(list);
			}
		}
	}

	private void GroupEdQueue(bool replace)
	{
		GroupEd ged = _ged;
		if (ged != null)
		{
			Group g = ged.G;
			if (replace)
			{
				CloseModal();
				AskReplaceQueueWithGroup(g);
			}
			else
			{
				ged.Status.Text = QueueGroup(g, replace: false);
			}
		}
	}

	private bool GroupEdKey(Key key, bool ctrl)
	{
		GroupEd ged = _ged;
		if (ged == null)
		{
			return false;
		}
		if (key == Key.Delete && ged.Right.SelectedItems.Count > 0)
		{
			GroupEdRemove(Picked(ged.Right));
			return true;
		}
		if (ctrl && key == Key.A)
		{
			(ged.Right.IsKeyboardFocusWithin ? ged.Right : ged.Left).SelectAll();
			return true;
		}
		bool flag = ctrl;
		if (flag)
		{
			bool flag2 = ((key == Key.Up || key == Key.Down) ? true : false);
			flag = flag2;
		}
		if (flag && ged.Right.SelectedItems.Count > 0)
		{
			GroupEdShift((key != Key.Up) ? 1 : (-1));
			return true;
		}
		return false;
	}

	private static void WirePickDrag(ListBox list, params string[] formats)
	{
		Point? start = null;
		ListBoxItem keep = null;
		list.PreviewMouseLeftButtonDown += (object _, MouseButtonEventArgs e) =>
		{
			start = null;
			keep = null;
			if (e.OriginalSource is DependencyObject dependencyObject && !IsInside<ScrollBar>(dependencyObject) && ItemsControl.ContainerFromElement(list, dependencyObject) is ListBoxItem listBoxItem && e.ClickCount < 2)
			{
				start = e.GetPosition(list);
				if (listBoxItem.IsSelected && Keyboard.Modifiers == ModifierKeys.None && list.SelectedItems.Count > 1)
				{
					keep = listBoxItem;
					e.Handled = true;
				}
			}
		};
		list.PreviewMouseMove += (object _, MouseEventArgs e) =>
		{
			if (start.HasValue)
			{
				Point valueOrDefault = start.GetValueOrDefault();
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					Point position = e.GetPosition(list);
					if (!(Math.Abs(position.X - valueOrDefault.X) < 6.0) || !(Math.Abs(position.Y - valueOrDefault.Y) < 6.0))
					{
						start = null;
						keep = null;
						List<GSoundPlayer.Model.Track> list2 = Picked(list);
						if (list2.Count != 0)
						{
							DataObject dataObject = new DataObject();
							string[] array = formats;
							foreach (string format in array)
							{
								dataObject.SetData(format, list2);
							}
							DragDrop.DoDragDrop(list, dataObject, DragDropEffects.Move);
						}
					}
				}
			}
		};
		list.PreviewMouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			if (keep != null)
			{
				list.SelectedItems.Clear();
				keep.IsSelected = true;
			}
			start = null;
			keep = null;
		};
	}

	private static void WirePickDrop(ListBox list, string format, Action<List<GSoundPlayer.Model.Track>> drop)
	{
		list.DragOver += (object _, DragEventArgs e) =>
		{
			e.Handled = true;
			if (!e.Data.GetDataPresent(format))
			{
				e.Effects = DragDropEffects.None;
			}
			else
			{
				e.Effects = DragDropEffects.Move;
				list.SetResourceReference(Control.BorderBrushProperty, "Green2");
			}
		};
		list.DragLeave += (object _, DragEventArgs _) =>
		{
			list.SetResourceReference(Control.BorderBrushProperty, "Line2");
		};
		list.Drop += (object _, DragEventArgs e) =>
		{
			list.SetResourceReference(Control.BorderBrushProperty, "Line2");
			if (e.Data.GetData(format) is List<GSoundPlayer.Model.Track> obj)
			{
				e.Handled = true;
				drop(obj);
			}
		};
	}

	private void WireGroupDrop(ListBox right, Border line)
	{
		long lastScroll = 0L;
		right.DragOver += (object _, DragEventArgs e) =>
		{
			e.Handled = true;
			if (!e.Data.GetDataPresent("GSoundPlayer.PickAdd") && !e.Data.GetDataPresent("GSoundPlayer.PickMove"))
			{
				e.Effects = DragDropEffects.None;
				Hide();
			}
			else
			{
				e.Effects = DragDropEffects.Move;
				right.SetResourceReference(Control.BorderBrushProperty, "Green2");
				Point position = e.GetPosition(right);
				line.Margin = new Thickness(8.0, Math.Max(0.0, PickSlotAt(right, position).Y - 1.5), 8.0, 0.0);
				line.Visibility = Visibility.Visible;
				long tickCount = Environment.TickCount64;
				if (tickCount - lastScroll > 70)
				{
					ScrollViewer scrollViewer = FindScroller(right);
					if (scrollViewer != null)
					{
						if (position.Y < 30.0)
						{
							scrollViewer.LineUp();
						}
						else if (position.Y > right.ActualHeight - 30.0)
						{
							scrollViewer.LineDown();
						}
						lastScroll = tickCount;
					}
				}
			}
		};
		right.DragLeave += (object _, DragEventArgs _) =>
		{
			Hide();
		};
		right.Drop += (object _, DragEventArgs e) =>
		{
			Hide();
			e.Handled = true;
			int num = GroupSlot(PickSlotAt(right, e.GetPosition(right)).Slot);
			if (e.Data.GetData("GSoundPlayer.PickAdd") is List<GSoundPlayer.Model.Track> tracks)
			{
				GroupEdAdd(tracks, num);
			}
			else if (e.Data.GetData("GSoundPlayer.PickMove") is List<GSoundPlayer.Model.Track> tracks2)
			{
				GroupEdMove(tracks2, num);
			}
		};
		void Hide()
		{
			line.Visibility = Visibility.Collapsed;
			right.SetResourceReference(Control.BorderBrushProperty, "Line2");
		}
	}

	private static (int Slot, double Y) PickSlotAt(ListBox lb, Point p)
	{
		int num = -1;
		double item = 4.0;
		for (int i = 0; i < lb.Items.Count; i++)
		{
			if (!(lb.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem { IsVisible: not false } listBoxItem))
			{
				continue;
			}
			double y = listBoxItem.TranslatePoint(new Point(0.0, 0.0), lb).Y;
			if (!(y + listBoxItem.ActualHeight <= 0.0))
			{
				if (y >= lb.ActualHeight)
				{
					break;
				}
				if (p.Y < y + listBoxItem.ActualHeight / 2.0)
				{
					return (Slot: i, Y: y);
				}
				num = i;
				item = y + listBoxItem.ActualHeight;
			}
		}
		return (Slot: num + 1, Y: item);
	}

	private int GroupSlot(int slot)
	{
		GroupEd ged = _ged;
		if (ged == null)
		{
			return 0;
		}
		List<GSoundPlayer.Model.Track> list = GroupTracks(ged.G);
		List<PickItem> list2 = ged.Right.Items.OfType<PickItem>().ToList();
		if (slot < list2.Count)
		{
			return list.IndexOf(list2[slot].T);
		}
		if (list2.Count <= 0)
		{
			return list.Count;
		}
		return list.IndexOf(list2[list2.Count - 1].T) + 1;
	}

	private Group? FindGroup(string key)
	{
		return _groups.FirstOrDefault((Group g) => g.Key == key);
	}

	private void LoadGroups()
	{
		string kv = _lib.GetKv("nhom");
		if (kv == null)
		{
			foreach (GSoundPlayer.Model.Track item in _lib.Tracks.Where((GSoundPlayer.Model.Track t) => t.Tags.Count > 0))
			{
				item.Tags = new List<string>();
				_lib.Save(item);
			}
			_groups = new List<Group>();
			SaveGroups();
			return;
		}
		try
		{
			_groups = JsonSerializer.Deserialize<List<Group>>(kv) ?? new List<Group>();
		}
		catch (JsonException)
		{
			_groups = new List<Group>();
		}
		foreach (Group g in _groups)
		{
			List<long> collection = GroupOrder.Prune(g.Order, (from t in _lib.Tracks
				where t.Tags.Contains(g.Key)
				select t.Id).ToHashSet());
			g.Order.Clear();
			g.Order.AddRange(collection);
		}
	}

	private void SaveGroups()
	{
		_lib.SetKv("nhom", JsonSerializer.Serialize(_groups));
	}

	private List<GSoundPlayer.Model.Track> GroupTracks(Group g)
	{
		return GroupOrder.Sort(_lib.Tracks.Where((GSoundPlayer.Model.Track t) => t.Tags.Contains(g.Key)), g.Order);
	}

	private void SetGroupOrder(Group g, IReadOnlyList<GSoundPlayer.Model.Track> members)
	{
		List<long> collection = GroupOrder.Ids(members, g.Order, _lib.Tracks.Select((GSoundPlayer.Model.Track t) => t.Id).ToHashSet());
		g.Order.Clear();
		g.Order.AddRange(collection);
		SaveGroups();
	}

	private int PutInGroup(Group g, IEnumerable<GSoundPlayer.Model.Track> tracks, int? at = null)
	{
		List<GSoundPlayer.Model.Track> members = GroupTracks(g);
		List<GSoundPlayer.Model.Track> list = (from t in tracks.Distinct()
			where !t.Tags.Contains(g.Key)
			select t).ToList();
		if (list.Count == 0)
		{
			return 0;
		}
		foreach (GSoundPlayer.Model.Track item in list)
		{
			item.Tags = item.Tags.Append(g.Key).ToList();
			_lib.Save(item);
		}
		SetGroupOrder(g, GroupOrder.Place(members, list, at));
		return list.Count;
	}

	private int TakeFromGroup(Group g, IEnumerable<GSoundPlayer.Model.Track> tracks)
	{
		int num = 0;
		foreach (GSoundPlayer.Model.Track item in (from t in tracks.Distinct()
			where t.Tags.Contains(g.Key)
			select t).ToList())
		{
			item.Tags = item.Tags.Where((string k) => k != g.Key).ToList();
			_lib.Save(item);
			num++;
		}
		return num;
	}

	private string GroupText(GSoundPlayer.Model.Track t)
	{
		List<string> list = (from g in t.Tags.Select(FindGroup).OfType<Group>()
			select g.Name).ToList();
		if (list.Count <= 0)
		{
			return "Chưa có nhóm";
		}
		return string.Join(", ", list);
	}

	private void BuildChips()
	{
		Chips.Children.Clear();
		_chipButtons.Clear();
		(string, string)[] chips = Catalog.Chips;
		for (int i = 0; i < chips.Length; i++)
		{
			var (text, text2) = chips[i];
			AddChip(text, (text == "fav") ? ((object)Ui.IconText("star", text2, 13.0, 4.0)) : ((object)text2), null);
		}
		foreach (Group group in _groups)
		{
			AddChip(group.Key, GroupChipContent(group), group);
		}
		Button button = Ui.Btn("Chip", Ui.IconText("plus", "Nhóm", 12.0, 3.0), () =>
		{
			AskNewGroup(null);
		}, "Tạo nhóm mới. Kéo bài thả vào đây cũng được: tạo nhóm và thêm bài vào luôn");
		button.SetResourceReference(Control.ForegroundProperty, "AmberText");
		WireChipDrop(button, (GSoundPlayer.Model.Track t) =>
		{
			AskNewGroup(t);
		});
		if (_groups.Count >= 12)
		{
			button.IsEnabled = false;
			button.ToolTip = "Đã đủ " + 12 + " nhóm. Xóa nhóm không dùng nữa (chuột phải vào chip nhóm) để tạo nhóm mới";
		}
		Chips.Children.Add(button);
		if (!_chipButtons.ContainsKey(_chip))
		{
			_chip = "all";
		}
	}

	private static FrameworkElement GroupChipContent(Group g)
	{
		Ellipse ellipse = new Ellipse
		{
			Width = 8.0,
			Height = 8.0,
			Fill = Ui.Hex(g.Color),
			Margin = new Thickness(0.0, 0.0, 5.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		return Ui.Row(0.0, ellipse, new TextBlock
		{
			Text = g.Name,
			MaxWidth = 150.0,
			TextTrimming = TextTrimming.CharacterEllipsis,
			VerticalAlignment = VerticalAlignment.Center
		});
	}

	private void AddChip(string key, object content, Group? g)
	{
		Button b = Ui.Btn("Chip", content, () =>
		{
			_chip = key;
			_sel = 0;
			RenderLib(resetPage: true);
		});
		_chipButtons[key] = b;
		if (g != null)
		{
			b.ToolTip = "Nhóm " + g.Name + ". Kéo bài thả vào đây để thêm vào nhóm. Bấm đúp: sửa danh sách nhóm. Chuột phải: đổi tên, màu, xóa";
			b.MouseDoubleClick += (object _, MouseButtonEventArgs _) =>
			{
				OpenGroupEditor(g);
			};
			WireChipDrop(b, (GSoundPlayer.Model.Track t) =>
			{
				AddToGroup(t, g.Key);
			});
			b.MouseRightButtonUp += (object _, MouseButtonEventArgs e) =>
			{
				e.Handled = true;
				GroupMenu(b, g);
			};
		}
		else if (key == "none")
		{
			b.ToolTip = "Bài chưa ở nhóm nào. Kéo bài thả vào đây để bỏ bài khỏi mọi nhóm";
			WireChipDrop(b, ClearGroups);
		}
		else if (key == "fav")
		{
			b.ToolTip = "Bài hay dùng (có sao). Kéo bài thả vào đây để đánh sao";
			WireChipDrop(b, (GSoundPlayer.Model.Track t) =>
			{
				if (!t.Fav)
				{
					t.Fav = true;
					_lib.Save(t);
					RenderLib();
				}
				ShowToast("“" + t.Name + "” đã ở Hay dùng", ToastKind.Ok);
			});
		}
		Chips.Children.Add(b);
	}

	private void WireChipDrop(Button b, Action<GSoundPlayer.Model.Track> drop)
	{
		b.AllowDrop = true;
		string before = null;
		b.DragEnter += (object _, DragEventArgs e) =>
		{
			if (e.Data.GetDataPresent("GSoundPlayer.Track"))
			{
				if (before == null)
				{
					before = Ui.GetState(b);
				}
				Ui.SetState(b, "drop");
			}
		};
		b.DragOver += (object _, DragEventArgs e) =>
		{
			if (e.Data.GetDataPresent("GSoundPlayer.Track"))
			{
				e.Effects = DragDropEffects.Copy;
				e.Handled = true;
			}
		};
		b.DragLeave += (object _, DragEventArgs _) =>
		{
			Restore();
		};
		b.Drop += (object _, DragEventArgs e) =>
		{
			Restore();
			if (e.Data.GetData("GSoundPlayer.Track") is GSoundPlayer.Model.Track obj)
			{
				e.Handled = true;
				drop(obj);
			}
		};
		void Restore()
		{
			if (before != null)
			{
				Ui.SetState(b, before);
				before = null;
			}
		}
	}

	private void AddToGroup(GSoundPlayer.Model.Track t, string key)
	{
		Group obj = FindGroup(key);
		if ((object)obj != null)
		{
			if (PutInGroup(obj, new GSoundPlayer.Model.Track[1] { t }) == 0)
			{
				ShowToast("“" + t.Name + "” đã ở nhóm " + obj.Name);
				return;
			}
			RenderLib();
			ShowToast("Đã thêm “" + t.Name + "” vào cuối nhóm " + obj.Name, ToastKind.Ok);
		}
	}

	private void RemoveFromGroup(GSoundPlayer.Model.Track t, string key)
	{
		Group obj = FindGroup(key);
		if ((object)obj != null && TakeFromGroup(obj, new GSoundPlayer.Model.Track[1] { t }) != 0)
		{
			RenderLib();
			ShowToast("Đã bỏ “" + t.Name + "” khỏi nhóm " + obj.Name);
		}
	}

	private void ClearGroups(GSoundPlayer.Model.Track t)
	{
		if (t.Tags.Count == 0)
		{
			ShowToast("“" + t.Name + "” chưa ở nhóm nào");
			return;
		}
		t.Tags = new List<string>();
		_lib.Save(t);
		RenderLib();
		ShowToast("Đã bỏ “" + t.Name + "” khỏi mọi nhóm");
	}

	private void TrackGroupMenu(GSoundPlayer.Model.Track t, FrameworkElement anchor)
	{
		List<Mi> list = new List<Mi>();
		foreach (Group group in _groups)
		{
			Group gg = group;
			list.Add(t.Tags.Contains(group.Key) ? new Mi("Bỏ khỏi nhóm " + group.Name, () =>
			{
				RemoveFromGroup(t, gg.Key);
			}, "x") : new Mi("Thêm vào nhóm " + group.Name, () =>
			{
				AddToGroup(t, gg.Key);
			}, group.Color));
		}
		list.Add(new Mi("Nhóm mới…", () =>
		{
			AskNewGroup(t);
		}, "plus"));
		if (t.Tags.Count > 0)
		{
			list.Add(new Mi("Bỏ khỏi mọi nhóm", () =>
			{
				ClearGroups(t);
			}, "x"));
		}
		string text = ((t.Name.Length > 34) ? (t.Name.Substring(0, 33) + "…") : t.Name);
		ShowMenu(anchor, "NHÓM CỦA " + text.ToUpperInvariant(), list);
	}

	private bool GroupsFull()
	{
		if (_groups.Count < 12)
		{
			return false;
		}
		ShowToast("Đã đủ " + 12 + " nhóm, không tạo thêm được. Xóa nhóm không dùng nữa (chuột phải vào chip nhóm) rồi tạo nhóm mới", ToastKind.Warn);
		return true;
	}

	private void AskNewGroup(GSoundPlayer.Model.Track? first)
	{
		if (GroupsFull())
		{
			return;
		}
		AskText((first != null) ? ("Tên nhóm mới cho “" + first.Name + "”") : "Tên nhóm mới", "", (string name) =>
		{
			Group obj = CreateGroup(name);
			if (!(obj == null))
			{
				if (first != null)
				{
					AddToGroup(first, obj.Key);
				}
				else
				{
					OpenGroupEditor(obj);
				}
			}
		});
	}

	private bool NameTaken(string name, Group? except = null)
	{
		if (!_groups.Any((Group g) => g != except && string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase)))
		{
			return Catalog.Chips.Any(((string Key, string Label) c) => string.Equals(c.Label, name, StringComparison.OrdinalIgnoreCase));
		}
		return true;
	}

	private Group? CreateGroup(string name)
	{
		name = name.Trim();
		if (name.Length == 0 || GroupsFull())
		{
			return null;
		}
		if (NameTaken(name))
		{
			ShowToast("Đã có nhóm tên “" + name + "”", ToastKind.Warn);
			return null;
		}
		HashSet<string> used = _groups.Select((Group x) => x.Color).ToHashSet(StringComparer.OrdinalIgnoreCase);
		string color = Catalog.GroupColors.FirstOrDefault((string c) => !used.Contains(c)) ?? Catalog.GroupColors[_groups.Count % Catalog.GroupColors.Length];
		Group obj = new Group("g" + Guid.NewGuid().ToString("N").Substring(0, 8), name, color);
		_groups.Add(obj);
		SaveGroups();
		BuildChips();
		RenderLib();
		ShowToast("Đã tạo nhóm " + name + ". Kéo bài thả vào chip nhóm để thêm bài", ToastKind.Ok);
		return obj;
	}

	private void ReplaceGroup(Group old, Group now)
	{
		int num = _groups.IndexOf(old);
		if (num >= 0)
		{
			_groups[num] = now;
			SaveGroups();
			BuildChips();
			RenderLib();
		}
	}

	private void GroupMenu(FrameworkElement anchor, Group g)
	{
		List<Mi> list = new List<Mi>
		{
			new Mi("Sửa danh sách nhóm…", () =>
			{
				OpenGroupEditor(g);
			}, "music"),
			new Mi("Xếp cả nhóm vào hàng chờ", () =>
			{
				QueueGroup(g, replace: false);
			}, "list-plus"),
			new Mi("Thay hàng chờ bằng nhóm này…", () =>
			{
				AskReplaceQueueWithGroup(g);
			}),
			new Mi("Đổi tên…", () =>
			{
				AskText("Tên nhóm", g.Name, (string v) =>
				{
					v = v.Trim();
					if (v.Length != 0 && !(v == g.Name))
					{
						if (NameTaken(v, g))
						{
							ShowToast("Đã có nhóm tên “" + v + "”", ToastKind.Warn);
						}
						else
						{
							ReplaceGroup(g, g with
							{
								Name = v
							});
						}
					}
				});
			}, "pencil")
		};
		foreach (var (c, text3) in Catalog.FxColors)
		{
			string h = text3;
			list.Add(new Mi("Màu " + ColorName(c), () =>
			{
				ReplaceGroup(g, g with
				{
					Color = h
				});
			}, h));
		}
		list.Add(new Mi("Xóa nhóm…", () =>
		{
			AskDeleteGroup(g);
		}, "trash", Danger: true));
		ShowMenu(anchor, "NHÓM " + g.Name.ToUpperInvariant(), list);
	}

	private void AskDeleteGroup(Group g)
	{
		int num = Enumerable.Count(_lib.Tracks, (GSoundPlayer.Model.Track t) => t.Tags.Contains(g.Key));
		ConfirmDelete("Xóa nhóm “" + g.Name + "”?", ((num > 0) ? (num + " bài trong nhóm không bị xóa khỏi kho, chỉ ra khỏi nhóm này; nhóm khác của các bài giữ nguyên. ") : "Nhóm đang trống. ") + "Nhầm thì bấm Hoàn tác trong 10 giây.", "Xóa nhóm", () =>
		{
			DeleteGroup(g);
		});
	}

	private void DeleteGroup(Group g)
	{
		int at = _groups.IndexOf(g);
		if (at < 0)
		{
			return;
		}
		List<GSoundPlayer.Model.Track> members = _lib.Tracks.Where((GSoundPlayer.Model.Track t) => t.Tags.Contains(g.Key)).ToList();
		foreach (GSoundPlayer.Model.Track item in members)
		{
			item.Tags = item.Tags.Where((string k) => k != g.Key).ToList();
			_lib.Save(item);
		}
		_groups.RemoveAt(at);
		SaveGroups();
		if (_chip == g.Key)
		{
			_chip = "all";
		}
		BuildChips();
		RenderLib(resetPage: true);
		ShowToast("Đã xóa nhóm " + g.Name, ToastKind.Normal, "Hoàn tác", () =>
		{
			if (!(FindGroup(g.Key) != null))
			{
				_groups.Insert(Math.Min(at, _groups.Count), g);
				foreach (GSoundPlayer.Model.Track item2 in members.Where((GSoundPlayer.Model.Track t) => _lib.Tracks.Contains(t) && !t.Tags.Contains(g.Key)))
				{
					item2.Tags = item2.Tags.Append(g.Key).ToList();
					_lib.Save(item2);
				}
				SaveGroups();
				BuildChips();
				RenderLib();
				ShowToast("Đã lấy lại nhóm " + g.Name, ToastKind.Ok);
			}
		}, 10000);
	}

	private (string Note, string? Key, Group? Before, List<GSoundPlayer.Model.Track> Added) ApplyPlaylistGroup(PlaylistGroup pg, List<GSoundPlayer.Model.Track> tracks)
	{
		Group g = _groups.FirstOrDefault((Group x) => string.Equals(x.Name, pg.Name, StringComparison.OrdinalIgnoreCase));
		Group obj = ((g == null) ? null : g with
		{
			Order = new List<long>(g.Order)
		});
		if (g == null)
		{
			if (_groups.Count >= 12)
			{
				return (Note: " · chưa tạo nhóm vì đã đủ " + 12 + " nhóm", Key: null, Before: null, Added: new List<GSoundPlayer.Model.Track>());
			}
			string name = (NameTaken(pg.Name) ? (pg.Name + " (nhập)") : pg.Name);
			g = new Group("g" + Guid.NewGuid().ToString("N").Substring(0, 8), name, pg.Color);
			_groups.Add(g);
		}
		List<GSoundPlayer.Model.Track> list = tracks.Where((GSoundPlayer.Model.Track t) => !t.Tags.Contains(g.Key)).ToList();
		foreach (GSoundPlayer.Model.Track item in list)
		{
			item.Tags = item.Tags.Append(g.Key).ToList();
			_lib.Save(item);
		}
		IEnumerable<GSoundPlayer.Model.Track> second = from t in GroupTracks(g)
			where !tracks.Contains(t)
			select t;
		SetGroupOrder(g, tracks.Concat(second).ToList());
		BuildChips();
		return (Note: (obj == null) ? (" · đã tạo nhóm " + g.Name) : (" · đã cập nhật nhóm " + g.Name), Key: g.Key, Before: obj, Added: list);
	}

	private string QueueGroup(Group g, bool replace)
	{
		Group obj = FindGroup(g.Key);
		if ((object)obj == null)
		{
			return "";
		}
		List<GSoundPlayer.Model.Track> list = GroupTracks(obj);
		string text;
		if (list.Count == 0)
		{
			text = "Nhóm " + obj.Name + " chưa có bài nào";
			ShowToast(text, ToastKind.Warn);
			return text;
		}
		List<GSoundPlayer.Model.Track> list2 = list.Where((GSoundPlayer.Model.Track t) => !C.IsSounding(t)).ToList();
		string text2 = ((list2.Count < list.Count) ? " (bỏ qua bài đang phát)" : "");
		if (replace)
		{
			GSoundPlayer.Model.Track pick = C.Pick;
			if (pick != null)
			{
				C.ClearPickIf(pick);
			}
			C.ReplaceQueue(list2);
			C.LockGo(1500.0);
			FlashNext();
			text = "Hàng chờ giờ là nhóm " + obj.Name + ", " + list2.Count + " bài" + text2;
			ShowToast(text, ToastKind.Ok);
			return text;
		}
		List<GSoundPlayer.Model.Track> list3 = list2.Where((GSoundPlayer.Model.Track t) => !C.Queue.Contains(t)).ToList();
		if (list3.Count == 0)
		{
			text = "Cả nhóm " + obj.Name + " đã ở trong hàng chờ" + text2;
			ShowToast(text);
			return text;
		}
		C.ReplaceQueue(C.Queue.Concat(list3).ToList());
		int num = list2.Count - list3.Count;
		text = "Đã xếp " + list3.Count + " bài nhóm " + obj.Name + " vào cuối hàng chờ" + ((num > 0) ? (", " + num + " bài đã có sẵn") : "") + text2;
		ShowToast(text, ToastKind.Ok);
		return text;
	}

	private void AskReplaceQueueWithGroup(Group g)
	{
		if (C.Queue.Count == 0)
		{
			QueueGroup(g, replace: true);
			return;
		}
		ConfirmDelete("Thay hàng chờ bằng nhóm “" + g.Name + "”?", C.Queue.Count + " bài đang trong hàng chờ sẽ ra khỏi hàng chờ (vẫn còn trong kho), thay bằng các bài của nhóm theo đúng thứ tự. Bài đang phát vẫn phát tiếp.", "Thay hàng chờ", () =>
		{
			QueueGroup(g, replace: true);
		});
	}

	private void WireInput()
	{
		PreviewKeyDown += OnPreviewKey;
		PreviewKeyUp += OnPreviewKeyUp;
		PreviewTextInput += OnPreviewText;
		PreviewMouseDown += OnWindowMouseDown;
		Deactivated += (object? _, EventArgs _) =>
		{
			EndKeyHold(tap: false);
			StopEscHold();
		};
		DragEnter += OnDragEnter;
		DragOver += OnDragOver;
		DragLeave += OnDragLeave;
		Drop += OnDropFiles;
		ComponentDispatcher.ThreadFilterMessage += OnThreadFilter;
	}

	private void OnThreadFilter(ref MSG m, ref bool handled)
	{
		if (m.message == 256 && (int)m.wParam == 13)
		{
			_enterExtended = ((ulong)m.lParam & 0x1000000uL) != 0;
		}
	}

	private void FocusSearch()
	{
		SearchBox.Focus();
		SearchBox.SelectAll();
	}

	private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.OriginalSource is DependencyObject d && !IsInside<TextBoxBase>(d))
		{
			Root.Focus();
		}
		if (SetPop.Visibility == Visibility.Visible && !_devBox.IsDropDownOpen && e.OriginalSource is DependencyObject d2 && !IsDescendant(d2, SetPop) && !IsDescendant(d2, BtnSet))
		{
			SetPop.Visibility = Visibility.Collapsed;
		}
	}

	private static bool IsInside<T>(DependencyObject? d) where T : class
	{
		while (d != null)
		{
			if (d is T)
			{
				return true;
			}
			bool flag = ((d is Visual || d is Visual3D) ? true : false);
			d = (flag ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d));
		}
		return false;
	}

	private static bool IsDescendant(DependencyObject? d, DependencyObject root)
	{
		while (d != null)
		{
			if (d == root)
			{
				return true;
			}
			bool flag = ((d is Visual || d is Visual3D) ? true : false);
			d = (flag ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d));
		}
		return false;
	}

	private void StartKeyHold(Key k, int ms, Action fn, Action? onTap)
	{
		EndKeyHold(tap: false);
		_keyHoldKey = k;
		_keyHoldFired = false;
		_keyHoldTap = onTap;
		_keyHold = new DispatcherTimer(DispatcherPriority.Normal)
		{
			Interval = TimeSpan.FromMilliseconds(ms)
		};
		_keyHold.Tick += (object? _, EventArgs _) =>
		{
			_keyHold?.Stop();
			_keyHoldFired = true;
			fn();
		};
		_keyHold.Start();
	}

	private void EndKeyHold(bool tap)
	{
		_keyHold?.Stop();
		_keyHold = null;
		if (tap && _keyHoldKey != Key.None && !_keyHoldFired)
		{
			_keyHoldTap?.Invoke();
		}
		_keyHoldKey = Key.None;
	}

	private void UndoKey(Key k)
	{
		if (C.Silent)
		{
			C.ResumeCut();
			return;
		}
		StartKeyHold(k, 500, C.ResumeCut, () =>
		{
			ShowToast("Đang có nhạc: giữ ⌫ nửa giây mới phát lại bài vừa tắt", ToastKind.Warn);
		});
	}

	private void StopEscHold()
	{
		_escHold?.Stop();
		_escHold = null;
	}

	private void EscPress(bool shift)
	{
		double nowMs = C.NowMs;
		StopEscHold();
		_escHold = new DispatcherTimer(DispatcherPriority.Normal)
		{
			Interval = TimeSpan.FromMilliseconds(1000L)
		};
		_escHold.Tick += (object? _, EventArgs _) =>
		{
			StopEscHold();
			C.Panic();
		};
		_escHold.Start();
		if (nowMs < _escGuard)
		{
			_escGuard = 0.0;
			ShowToast("Vừa thoát ô chữ nên Esc này không tắt nhạc. Muốn tắt dần: bấm Esc lần nữa", ToastKind.Warn);
		}
		else if (shift)
		{
			C.StopNow();
		}
		else
		{
			C.EscAction();
		}
	}

	private void OnPreviewKeyUp(object sender, KeyEventArgs e)
	{
		Key key = ((e.Key == Key.System) ? e.SystemKey : e.Key);
		if (key == Key.Escape)
		{
			StopEscHold();
		}
		if (_keyHoldKey == key)
		{
			EndKeyHold(tap: true);
		}
	}

	private void OnPreviewText(object sender, TextCompositionEventArgs e)
	{
		if (!(Keyboard.FocusedElement is TextBox) && !ModalOpen && e.Text.Length != 0 && char.IsLetter(e.Text[0]))
		{
			bool flag;
			switch (e.Text.ToLowerInvariant())
			{
			case "m":
			case "p":
			case "l":
			case "n":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				_typeGuard = C.NowMs + 1500.0;
			}
		}
	}

	private void OnPreviewKey(object sender, KeyEventArgs e)
	{
		Key key = ((e.Key == Key.System) ? e.SystemKey : e.Key);
		ModifierKeys modifiers = Keyboard.Modifiers;
		bool flag = modifiers.HasFlag(ModifierKeys.Control);
		bool flag2 = modifiers.HasFlag(ModifierKeys.Alt);
		bool flag3 = modifiers.HasFlag(ModifierKeys.Shift);
		TextBox textBox = Keyboard.FocusedElement as TextBox;
		double nowMs = C.NowMs;
		if (ModalOpen)
		{
			if (key == Key.Escape)
			{
				e.Handled = true;
				CloseModal();
				_escGuard = nowMs + 2000.0;
			}
			else if (textBox == null)
			{
				GroupEdKey(key, flag);
				e.Handled = true;
			}
			return;
		}
		if (MenuOpen)
		{
			if (key == Key.Escape)
			{
				CloseMenu();
			}
			e.Handled = true;
			return;
		}
		if (SetPop.Visibility == Visibility.Visible)
		{
			bool flag4;
			switch (key)
			{
			case Key.Escape:
				e.Handled = true;
				SetPop.Visibility = Visibility.Collapsed;
				_escGuard = nowMs + 2000.0;
				return;
			case Key.Return:
			case Key.Space:
				flag4 = true;
				break;
			default:
				flag4 = false;
				break;
			}
			if (flag4)
			{
				e.Handled = true;
				return;
			}
		}
		if (flag && key == Key.O)
		{
			e.Handled = true;
			OpenImport();
			return;
		}
		if (_editItem != null && key == Key.Escape && textBox == null)
		{
			e.Handled = true;
			CloseItemEd();
			_escGuard = nowMs + 2000.0;
			return;
		}
		if (flag && key == Key.F)
		{
			e.Handled = true;
			FocusSearch();
			return;
		}
		if (flag && key == Key.E)
		{
			e.Handled = true;
			GSoundPlayer.Model.Track track = C.Pick ?? ((_sel < _view.Count) ? _view[_sel] : null);
			if (track != null)
			{
				OpenItemEd(track);
			}
			return;
		}
		if (flag && key == Key.V && textBox == null)
		{
			e.Handled = true;
			PasteFiles();
			return;
		}
		if (flag2 && !flag)
		{
			switch (key)
			{
			case Key.M:
				e.Handled = true;
				C.ToggleDuck();
				return;
			case Key.P:
				e.Handled = true;
				C.TogglePause();
				return;
			case Key.L:
				e.Handled = true;
				C.ToggleLoop();
				return;
			case Key.D1:
			case Key.D2:
			case Key.D3:
			case Key.D4:
			case Key.D5:
			case Key.D6:
			case Key.D7:
			case Key.D8:
			case Key.D9:
				e.Handled = true;
				C.PlayFx((int)(key - 35));
				return;
			}
			if (key == Key.D0)
			{
				e.Handled = true;
				C.StopAllFx();
				return;
			}
		}
		if (textBox != null)
		{
			if (textBox == SearchBox)
			{
				if ((key == Key.Up || key == Key.Down) ? true : false)
				{
					e.Handled = true;
					MoveSel((key == Key.Down) ? 1 : (-1));
					return;
				}
				switch (key)
				{
				case Key.Return:
					e.Handled = true;
					if (SearchBox.Text.Trim().Length == 0)
					{
						ShowToast("Gõ tên bài trước đã", ToastKind.Warn);
					}
					else if (!((_sel >= _view.Count) | flag))
					{
						GSoundPlayer.Model.Track t = _view[_sel];
						if (flag3)
						{
							C.Enqueue(t);
							break;
						}
						if (C.Silent)
						{
							PlayFromLib(t);
							break;
						}
						C.MoveToFront(t);
						ClearSearch();
						FlashNext();
					}
					break;
				case Key.Escape:
					e.Handled = true;
					ClearSearch();
					break;
				}
			}
			else if ((key == Key.Return || key == Key.Escape) ? true : false)
			{
				e.Handled = true;
				Root.Focus();
				_escGuard = nowMs + 2000.0;
			}
			return;
		}
		if (key != Key.Return || !_enterExtended)
		{
			switch (key)
			{
			case Key.Next:
				break;
			case Key.NumPad0:
				e.Handled = true;
				if (!e.IsRepeat)
				{
					C.StopAllFx();
				}
				return;
			case Key.Decimal:
				e.Handled = true;
				if (!e.IsRepeat)
				{
					C.EscAction();
				}
				return;
			case Key.NumPad1:
			case Key.NumPad2:
			case Key.NumPad3:
			case Key.NumPad4:
			case Key.NumPad5:
			case Key.NumPad6:
			case Key.NumPad7:
			case Key.NumPad8:
			case Key.NumPad9:
				e.Handled = true;
				if (!e.IsRepeat)
				{
					C.PlayFx((int)(key - 75));
				}
				return;
			default:
			{
				if (key == Key.Add)
				{
					e.Handled = true;
					if (!e.IsRepeat)
					{
						C.ToggleDuck();
					}
					return;
				}
				if (key == Key.Subtract)
				{
					e.Handled = true;
					if (!e.IsRepeat)
					{
						C.TogglePause();
					}
					return;
				}
				if (key == Key.Multiply)
				{
					e.Handled = true;
					if (!e.IsRepeat)
					{
						C.ToggleLoop();
					}
					return;
				}
				if (key == Key.Divide)
				{
					e.Handled = true;
					if (!e.IsRepeat)
					{
						UndoKey(key);
					}
					return;
				}
				bool flag5 = key >= Key.A && key <= Key.Z;
				bool flag6 = key >= Key.D0 && key <= Key.D9;
				bool flag4;
				if (!flag && !flag2 && !e.IsRepeat)
				{
					flag4 = (((uint)(key - 55) <= 2u || key == Key.P) ? true : false);
					bool flag7 = flag4;
					if (nowMs < _typeGuard && ((flag5 || key == Key.Space || key == Key.Back) | flag6))
					{
						e.Handled = true;
						_typeGuard = nowMs + 1500.0;
						return;
					}
					if (flag5 && !flag7)
					{
						_typeGuard = nowMs + 1500.0;
						if (nowMs - _hintAt > 4000.0)
						{
							_hintAt = nowMs;
							ShowToast("Gõ ngoài ô tìm nên phím lệnh khóa 1,5 giây. Muốn tìm bài: bấm vào ô tìm (hoặc phím /)", ToastKind.Warn);
						}
						e.Handled = true;
						return;
					}
				}
				switch (key)
				{
				case Key.Space:
					e.Handled = true;
					if (!e.IsRepeat)
					{
						C.Go();
					}
					return;
				case Key.Escape:
					e.Handled = true;
					if (!e.IsRepeat)
					{
						EscPress(flag3);
					}
					return;
				case Key.Back:
					e.Handled = true;
					if (!e.IsRepeat)
					{
						UndoKey(key);
					}
					return;
				case Key.F12:
					e.Handled = true;
					if (!e.IsRepeat)
					{
						C.Panic();
					}
					return;
				case Key.Up:
				case Key.Down:
					e.Handled = true;
					MoveSel((key == Key.Down) ? 1 : (-1));
					return;
				}
				flag4 = flag3 && !flag && !flag2;
				if (flag4)
				{
					bool flag8 = ((key == Key.Left || key == Key.Right) ? true : false);
					flag4 = flag8;
				}
				if (flag4)
				{
					e.Handled = true;
					if (!e.IsRepeat)
					{
						if (key == Key.Right)
						{
							C.Go();
						}
						else
						{
							C.PrevTrack();
						}
					}
					return;
				}
				if (!flag && !flag2)
				{
					if ((key == Key.Left || key == Key.Right) ? true : false)
					{
						e.Handled = true;
						MoveSel(((key == Key.Right) ? 1 : (-1)) * Math.Max(1, LibGrid.Rows));
						return;
					}
					if (key == Key.Return && !e.IsRepeat)
					{
						e.Handled = true;
						if (_sel < _view.Count)
						{
							C.SetPick(_view[_sel]);
							if (C.Pick != null)
							{
								FlashNext();
							}
						}
						return;
					}
				}
				if (e.IsRepeat | flag | flag2)
				{
					return;
				}
				if (key >= Key.D1 && key <= Key.D9)
				{
					e.Handled = true;
					C.PlayFx((int)(key - 35));
					return;
				}
				switch (key)
				{
				case Key.D0:
					e.Handled = true;
					C.StopAllFx();
					break;
				case Key.Oem2:
					e.Handled = true;
					if (flag3)
					{
						OpenHelp();
					}
					else
					{
						FocusSearch();
					}
					break;
				case Key.M:
					e.Handled = true;
					C.ToggleDuck();
					break;
				case Key.P:
					e.Handled = true;
					C.TogglePause();
					break;
				case Key.L:
					e.Handled = true;
					C.ToggleLoop();
					break;
				}
				return;
			}
			}
		}
		e.Handled = true;
		if (!e.IsRepeat)
		{
			C.Go();
		}
	}

	private static bool HasFiles(DragEventArgs e)
	{
		return e.Data.GetDataPresent(DataFormats.FileDrop);
	}

	private void OnDragEnter(object sender, DragEventArgs e)
	{
		if (HasFiles(e))
		{
			DropOverlay.Visibility = Visibility.Visible;
		}
	}

	private void OnDragOver(object sender, DragEventArgs e)
	{
		e.Effects = (HasFiles(e) ? DragDropEffects.Copy : DragDropEffects.None);
		e.Handled = true;
	}

	private void OnDragLeave(object sender, DragEventArgs e)
	{
		Point position = e.GetPosition(this);
		if (position.X <= 0.0 || position.Y <= 0.0 || position.X >= ActualWidth || position.Y >= ActualHeight)
		{
			DropOverlay.Visibility = Visibility.Collapsed;
		}
	}

	private void OnDropFiles(object sender, DragEventArgs e)
	{
		DropOverlay.Visibility = Visibility.Collapsed;
		if (e.Data.GetData(DataFormats.FileDrop) is string[] array && array.Length != 0)
		{
			HandleIncoming(array);
		}
	}

	private void PasteFiles()
	{
		if (Clipboard.ContainsFileDropList())
		{
			StringCollection fileDropList = Clipboard.GetFileDropList();
			HandleIncoming(fileDropList.Cast<string>().ToArray());
		}
	}

	private void HandleIncoming(string[] files)
	{
		if (_lockShown)
		{
			return;
		}
		string text = files.FirstOrDefault(Catalog.IsPackage);
		if (text != null)
		{
			OpenPlaylistFile(text);
			return;
		}
		if (files.Length == 1 && Catalog.IsPlaylist(files[0]))
		{
			OpenPlaylistFile(files[0]);
			return;
		}
		if (ModalOpen)
		{
			CloseModal();
		}
		ImportAndReportAsync(files);
	}

	public void OpenExternalFile(string path)
	{
		Dispatcher.BeginInvoke((Action)(() =>
		{
			if (WindowState == WindowState.Minimized)
			{
				WindowState = WindowState.Normal;
			}
			Activate();
			if (!_lockShown)
			{
				if (Catalog.IsPlaylist(path))
				{
					OpenPlaylistFile(path);
				}
				else
				{
					ImportAndReportAsync(new string[1] { path });
				}
			}
		}));
	}

	private void WireLibrary()
	{
		LibGrid.IsPlaying = (GSoundPlayer.Model.Track t) => C.IsPlaying(t);
		LibGrid.QueuePos = (GSoundPlayer.Model.Track t) => C.Queue.IndexOf(t);
		LibGrid.RowClicked += (GSoundPlayer.Model.Track t, int i) =>
		{
			_sel = i;
			C.SetPick(t);
			if (C.Pick != null)
			{
				FlashNext();
			}
			RefreshLib();
		};
		LibGrid.RowDoubleClicked += (GSoundPlayer.Model.Track t) =>
		{
			PlayFromLib(t);
		};
		LibGrid.RowActionInvoked += RowAct;
		LibGrid.RowMenu += TrackMenu;
		LibGrid.RowDrag += (GSoundPlayer.Model.Track t, FrameworkElement from) =>
		{
			DragDrop.DoDragDrop(from, new DataObject("GSoundPlayer.Track", t), DragDropEffects.Copy | DragDropEffects.Move);
		};
		WireQueueDrop();
		LibGrid.Paged += UpdatePager;
		LibGrid.GroupOf = FindGroup;
		LoadGroups();
		BuildChips();
	}

	private void RenderAll()
	{
		RenderNext();
		RenderQueue();
		RenderLib();
	}

	private void RenderLib(bool resetPage = false)
	{
		_view = _lib.View(SearchBox.Text, _chip, FindGroup(_chip)?.Order);
		if (_sel >= _view.Count)
		{
			_sel = Math.Max(0, _view.Count - 1);
		}
		foreach (var (text2, button2) in _chipButtons)
		{
			if (text2 == "new")
			{
				bool flag = _lib.Count("new") == 0;
				button2.IsEnabled = !flag || _chip == "new";
				button2.ToolTip = (flag ? "Chưa có bài nạp trong 24 giờ qua" : "Bài nạp trong 24 giờ qua");
			}
			Ui.SetState(button2, (text2 == _chip) ? "on" : "");
		}
		LibGrid.Items = _view;
		LibGrid.Selected = _sel;
		LibGrid.Pick = C.Pick;
		LibGrid.CanPlay = !C.JackLost;
		LibraryGrid libGrid = LibGrid;
		int tileMode;
		if (_chip == "fav")
		{
			int count = _view.Count;
			if (count > 0 && count <= 18)
			{
				tileMode = (string.IsNullOrWhiteSpace(SearchBox.Text) ? 1 : 0);
				goto IL_01b5;
			}
		}
		tileMode = 0;
		goto IL_01b5;
		IL_01b5:
		libGrid.TileMode = (byte)tileMode != 0;
		LibGrid.EmptyText = ((_lib.Tracks.Count == 0) ? "Kho nhạc đang trống. Kéo thả file hoặc cả thư mục nhạc vào cửa sổ này, hoặc bấm nút Nạp." : "Không thấy bài nào. Thử gõ ít chữ hơn, hoặc chọn nhóm “Tất cả”.");
		LibGrid.Refresh(resetPage);
		Dispatcher.BeginInvoke(new Action(UpdatePager), DispatcherPriority.Loaded);
	}

	private void RefreshLib()
	{
		LibGrid.Selected = _sel;
		LibGrid.Pick = C.Pick;
		LibGrid.CanPlay = !C.JackLost;
		LibGrid.Refresh();
	}

	private void UpdatePager()
	{
		int pageCount = LibGrid.PageCount;
		Pager.Visibility = ((pageCount <= 1) ? Visibility.Collapsed : Visibility.Visible);
		PagerText.Text = LibGrid.Page + 1 + "/" + pageCount;
		BtnPagePrev.IsEnabled = LibGrid.Page > 0;
		BtnPageNext.IsEnabled = LibGrid.Page < pageCount - 1;
	}

	private void OnPagePrev(object s, RoutedEventArgs e)
	{
		LibGrid.GoPage(-1);
	}

	private void OnPageNext(object s, RoutedEventArgs e)
	{
		LibGrid.GoPage(1);
	}

	private void OnSearchChanged(object s, TextChangedEventArgs e)
	{
		SearchHint.Visibility = ((SearchBox.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
		_sel = 0;
		RenderLib(resetPage: true);
	}

	private void MoveSel(int d)
	{
		if (_view.Count != 0)
		{
			_sel = Math.Clamp(_sel + d, 0, _view.Count - 1);
			LibGrid.EnsureVisible(_sel);
			RefreshLib();
		}
	}

	private void ClearSearch()
	{
		SearchBox.Text = "";
		_sel = 0;
		Root.Focus();
		_escGuard = C.NowMs + 2000.0;
		RenderLib(resetPage: true);
	}

	private void PlayFromLib(GSoundPlayer.Model.Track t)
	{
		C.PlayItem(t);
		if (SearchBox.Text.Length > 0 || SearchBox.IsKeyboardFocused)
		{
			ClearSearch();
		}
	}

	private void StopFromRow(GSoundPlayer.Model.Track t)
	{
		if (!C.StopTrack(t))
		{
			ShowToast("Bài vừa bắt đầu phát nên chưa dừng. Muốn dừng thì bấm nút dừng lần nữa", ToastKind.Warn);
		}
	}

	private void RowAct(GSoundPlayer.Model.Track t, RowAction a)
	{
		switch (a)
		{
		case RowAction.Play:
			PlayFromLib(t);
			break;
		case RowAction.Stop:
			StopFromRow(t);
			break;
		case RowAction.Queue:
			C.Enqueue(t);
			break;
		case RowAction.Edit:
			OpenItemEd(t);
			break;
		case RowAction.Fav:
			t.Fav = !t.Fav;
			_lib.Save(t);
			RenderLib();
			break;
		case RowAction.Rename:
			AskText("Tên hiển thị của bài", t.Name, (string v) =>
			{
				t.Name = v;
				t.Junk = false;
				_lib.Save(t);
				_needRender = true;
			});
			break;
		}
	}

	private void RenderNext()
	{
		GSoundPlayer.Model.Track nextUp = C.NextUp;
		int num = ((nextUp != null && C.Pick == null) ? C.Queue.IndexOf(nextUp) : (-1));
		TextBlock nextLabel = NextLabel;
		string text;
		if (C.Pick != null)
		{
			text = "KẾ TIẾP · BÀI VỪA CHỌN";
		}
		else if (num < 0)
		{
			text = "KẾ TIẾP";
		}
		else if (C.NextWraps)
		{
			text = ((num == 0) ? "KẾ TIẾP · LẶP LẠI TỪ ĐẦU HÀNG CHỜ" : ("KẾ TIẾP · LẶP LẠI, BÀI CHỜ THỨ " + (num + 1)));
		}
		else
		{
			text = ((num == 0) ? "KẾ TIẾP · BÀI CHỜ ĐẦU TIÊN" : ("KẾ TIẾP · BÀI CHỜ THỨ " + (num + 1)));
		}
		nextLabel.Text = text;
		NextTitle.Text = nextUp?.Name ?? (C.QueueDone ? "Đã phát hết hàng chờ" : "Chưa chọn bài");
		TextBlock nextNote = NextNote;
		string text2;
		if (nextUp == null)
		{
			text2 = (C.QueueDone ? "Bấm một bài để chọn, hoặc bấm ▶ ở dòng bài trong hàng chờ. Muốn phát vòng tròn thì bật lặp lại ở nút tùy chọn hàng chờ" : "Bấm một bài trong kho để chọn, rồi bấm Space để phát");
		}
		else
		{
			text2 = ((nextUp.Note.Length > 0) ? nextUp.Note : GroupText(nextUp));
		}
		nextNote.Text = text2;
		CutInfo lastCut = C.LastCut;
		UndoText.Text = ((lastCut != null) ? (lastCut.Item.Name + " " + Text.Fmt(lastCut.Pos - lastCut.Item.InP)) : "Phát lại chỗ vừa tắt");
	}

	private void RenderQueue()
	{
		double t = C.Queue.Sum((GSoundPlayer.Model.Track track3) => track3.Len);
		QCount.Text = ((C.Queue.Count > 0) ? (C.Queue.Count + " bài · " + Text.Fmt(t)) : "trống");
		BtnQClear.Visibility = ((C.Queue.Count <= 0) ? Visibility.Collapsed : Visibility.Visible);
		GSoundPlayer.Model.Track firstUp = ((C.Pick == null) ? C.NextQ() : null);
		int at = C.QueueAtIndex;
		string text = string.Join("|", C.Queue.Select((GSoundPlayer.Model.Track track3, int i) => track3.Id + ":" + track3.Name + ":" + track3.Len.ToString("F1") + ":" + track3.Missing + ":" + (track3 == firstUp) + ":" + C.IsPlaying(track3) + ":" + (i <= at)));
		if (text == _queueKey && QList.Children.Count > 0)
		{
			return;
		}
		_queueKey = text;
		QList.Children.Clear();
		if (C.Queue.Count == 0)
		{
			TextBlock textBlock = Ui.T("Chưa có bài chờ. Kéo bài từ kho thả vào đây, tìm bài rồi Shift+Enter, hoặc bấm nút xếp hàng chờ ở dòng bài.", 13.5, "Ink2", null, trim: false, wrap: true);
			textBlock.TextAlignment = TextAlignment.Center;
			textBlock.Margin = new Thickness(14.0);
			QList.Children.Add(textBlock);
			return;
		}
		GSoundPlayer.Model.Track track = ((C.Pick == null) ? C.NextQ() : null);
		for (int num = 0; num < C.Queue.Count; num++)
		{
			GSoundPlayer.Model.Track track2 = C.Queue[num];
			bool played = num <= at && track2 != track && !C.IsPlaying(track2);
			QList.Children.Add(MakeQueueRow(num, track2, track2 == track, played));
		}
	}

	private FrameworkElement MakeQueueRow(int k, GSoundPlayer.Model.Track it, bool isHead, bool played = false)
	{
		Grid grid = new Grid
		{
			MinHeight = 34.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(20.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		TextBlock textBlock = Ui.T((k + 1).ToString(), 13.0, "Ink2", FontWeights.Bold);
		textBlock.TextAlignment = TextAlignment.Right;
		grid.Children.Add(textBlock);
		TextBlock textBlock2 = Ui.T(it.Name, 13.5, it.Missing ? "RedText" : "Ink", FontWeights.SemiBold, trim: true);
		textBlock2.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
		textBlock2.ToolTip = (it.Missing ? ("THIẾU FILE: " + it.FileName) : GroupText(it));
		Grid.SetColumn(textBlock2, 1);
		grid.Children.Add(textBlock2);
		TabularText tabularText = Ui.Num(Text.Fmt(it.Len), 13.0, "Ink2");
		tabularText.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
		Grid.SetColumn(tabularText, 2);
		grid.Children.Add(tabularText);
		if (played)
		{
			FrameworkElement[] array = new FrameworkElement[3] { textBlock, textBlock2, tabularText };
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Opacity = 0.5;
			}
			if (!it.Missing)
			{
				textBlock2.ToolTip = "Đã phát trong vòng này. " + GroupText(it);
			}
		}
		Button button;
		if (C.IsPlaying(it))
		{
			button = QBtn("stop", "Dừng bài này ngay. Nhầm thì bấm ⌫ để phát lại từ chỗ vừa dừng", () =>
			{
				StopFromRow(it);
			});
			button.SetResourceReference(Control.BackgroundProperty, "RedTint");
			button.SetResourceReference(Control.BorderBrushProperty, "Red");
			button.SetResourceReference(Control.ForegroundProperty, "RedText");
		}
		else
		{
			button = QBtn("play", "Phát ngay", () =>
			{
				C.PlayItem(it);
			});
			button.SetResourceReference(Control.BackgroundProperty, "Green");
			button.BorderBrush = Ui.Hex("#2fdc7a");
			button.Foreground = Brushes.White;
		}
		if (!C.IsPlaying(it))
		{
			button.IsEnabled = !it.Missing && !C.JackLost;
			if (it.Missing)
			{
				button.ToolTip = "Thiếu file nhạc, chưa phát được";
			}
		}
		Button button2 = QBtn("chev-up", "Lên", () =>
		{
			C.SwapQueue(k, k - 1);
		});
		Button button3 = QBtn("chev-down", "Xuống", () =>
		{
			C.SwapQueue(k, k + 1);
		});
		button2.IsEnabled = k > 0;
		button3.IsEnabled = k < C.Queue.Count - 1;
		Button button4 = QBtn("x", "Bỏ khỏi hàng chờ", () =>
		{
			C.RemoveQueueAt(k);
			ShowToast("Đã bỏ “" + it.Name + "” khỏi hàng chờ");
		});
		Button[] array2 = new Button[3] { button2, button3, button4 };
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Width = 26.0;
		}
		button4.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		StackPanel extra = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Visibility = Visibility.Collapsed
		};
		extra.Children.Add(button2);
		extra.Children.Add(button3);
		extra.Children.Add(button4);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(6.0, 1.0, 0.0, 1.0)
		};
		stackPanel.Children.Add(button);
		stackPanel.Children.Add(extra);
		Grid.SetColumn(stackPanel, 3);
		grid.Children.Add(stackPanel);
		Border row = new Border
		{
			Child = grid,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(7.0),
			Padding = new Thickness(4.0, 0.0, 4.0, 0.0),
			Margin = new Thickness(0.0, 1.0, 0.0, 1.0),
			Background = Brushes.Transparent,
			BorderBrush = Brushes.Transparent
		};
		if (isHead)
		{
			row.SetResourceReference(Border.BackgroundProperty, "AmberTint");
			row.SetResourceReference(Border.BorderBrushProperty, "Amber");
		}
		extra.Visibility = Visibility.Visible;
		extra.Opacity = 0.4;
		row.MouseEnter += (object _, MouseEventArgs _) =>
		{
			extra.Opacity = 1.0;
		};
		row.MouseLeave += (object _, MouseEventArgs _) =>
		{
			extra.Opacity = 0.4;
		};
		Point? dragFrom = null;
		row.PreviewMouseLeftButtonDown += (object _, MouseButtonEventArgs e) =>
		{
			dragFrom = ((e.OriginalSource is DependencyObject d && IsInside<ButtonBase>(d)) ? ((Point?)null) : new Point?(e.GetPosition(row)));
		};
		row.PreviewMouseMove += (object _, MouseEventArgs e) =>
		{
			if (dragFrom.HasValue)
			{
				Point valueOrDefault = dragFrom.GetValueOrDefault();
				if (e.LeftButton == MouseButtonState.Pressed)
				{
					Point position = e.GetPosition(row);
					if (!(Math.Abs(position.X - valueOrDefault.X) < 8.0) || !(Math.Abs(position.Y - valueOrDefault.Y) < 8.0))
					{
						dragFrom = null;
						DragDrop.DoDragDrop(row, new DataObject("GSoundPlayer.Track", it), DragDropEffects.Move);
					}
				}
			}
		};
		row.PreviewMouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
		{
			dragFrom = null;
		};
		row.Cursor = Cursors.SizeNS;
		return row;
	}

	private void WireQueueDrop()
	{
		QScroll.DragOver += (object _, DragEventArgs e) =>
		{
			if (e.Data.GetDataPresent("GSoundPlayer.Track"))
			{
				e.Effects = DragDropEffects.Move;
				e.Handled = true;
				ShowQueueDropLine(QueueSlotAt(e.GetPosition(QHost)));
			}
		};
		QScroll.DragLeave += (object _, DragEventArgs _) =>
		{
			QDropLine.Visibility = Visibility.Collapsed;
		};
		QScroll.Drop += (object _, DragEventArgs e) =>
		{
			QDropLine.Visibility = Visibility.Collapsed;
			if (e.Data.GetData("GSoundPlayer.Track") is GSoundPlayer.Model.Track track)
			{
				e.Handled = true;
				bool flag = C.Queue.Contains(track);
				C.PlaceInQueue(track, QueueSlotAt(e.GetPosition(QHost)));
				if (!flag)
				{
					ShowToast("Đã xếp “" + track.Name + "” vào hàng chờ, thứ " + (C.Queue.IndexOf(track) + 1), ToastKind.Ok);
				}
			}
		};
	}

	private List<FrameworkElement> QueueRows()
	{
		return QList.Children.OfType<Border>().Take(C.Queue.Count).Cast<FrameworkElement>()
			.ToList();
	}

	private int QueueSlotAt(Point p)
	{
		List<FrameworkElement> list = QueueRows();
		for (int i = 0; i < list.Count; i++)
		{
			double y = list[i].TranslatePoint(new Point(0.0, 0.0), QHost).Y;
			if (p.Y < y + list[i].ActualHeight / 2.0)
			{
				return i;
			}
		}
		return list.Count;
	}

	private void ShowQueueDropLine(int slot)
	{
		List<FrameworkElement> list = QueueRows();
		double num = 2.0;
		if (slot < list.Count)
		{
			num = list[slot].TranslatePoint(new Point(0.0, 0.0), QHost).Y;
		}
		else if (list.Count > 0)
		{
			num = list[list.Count - 1].TranslatePoint(new Point(0.0, list[list.Count - 1].ActualHeight), QHost).Y;
		}
		QDropLine.Margin = new Thickness(8.0, Math.Max(0.0, num - 1.5), 8.0, 0.0);
		QDropLine.Visibility = Visibility.Visible;
	}

	private void OnQueueClear(object s, RoutedEventArgs e)
	{
		int count = C.Queue.Count;
		if (count != 0)
		{
			ConfirmDelete("Bỏ hết " + count + " bài khỏi hàng chờ?", "Bài vẫn còn trong kho nhạc, chỉ ra khỏi hàng chờ.", "Bỏ hết " + count + " bài", ClearQueueNow);
		}
	}

	private void ClearQueueNow()
	{
		int count = C.Queue.Count;
		if (count != 0)
		{
			C.ReplaceQueue(Array.Empty<GSoundPlayer.Model.Track>());
			ShowToast("Đã bỏ hết " + count + " bài khỏi hàng chờ");
		}
	}

	private static Button QBtn(string icon, string tip, Action a)
	{
		bool flag = ((icon == "play" || icon == "stop") ? true : false);
		Button button = Ui.IconBtn("Btn", icon, a, tip, flag ? 14 : 16);
		button.Width = 30.0;
		button.Height = 30.0;
		button.Margin = new Thickness(2.0, 0.0, 0.0, 0.0);
		Ui.SetRadius(button, new CornerRadius(5.0));
		return button;
	}

	private void OnAutoQueue(object s, RoutedEventArgs e)
	{
		C.AutoQueue = AutoQueueBox.IsChecked == true;
		_set.AutoQueue = C.AutoQueue;
		_set.Save();
	}

	private void BuildFxGrid()
	{
		for (int i = 0; i < 9; i++)
		{
			int idx = i;
			Grid grid = new Grid
			{
				ClipToBounds = true
			};
			Border border = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Width = 0.0,
				Opacity = 0.28
			};
			grid.Children.Add(border);
			Border border2 = new Border
			{
				Width = 25.0,
				Height = 25.0,
				CornerRadius = new CornerRadius(5.0),
				BorderThickness = new Thickness(1.0),
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center
			};
			border2.SetResourceReference(Border.BackgroundProperty, "FxKeyBg");
			TextBlock textBlock = Ui.T((i + 1).ToString(), 14.0, "FxKeyInk", FontWeights.ExtraBold);
			textBlock.HorizontalAlignment = HorizontalAlignment.Center;
			border2.Child = textBlock;
			grid.Children.Add(border2);
			TextBlock textBlock2 = Ui.T("", 12.5, "Ink", FontWeights.SemiBold, trim: true, wrap: true);
			textBlock2.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
			textBlock2.LineHeight = 15.0;
			textBlock2.MaxHeight = 32.0;
			textBlock2.Margin = new Thickness(36.0, 0.0, 3.0, 0.0);
			grid.Children.Add(textBlock2);
			Button b = Ui.Btn("Btn", grid, () =>
			{
				OnFxClick(idx);
			});
			b.Height = 44.0;
			b.Margin = new Thickness(2.0);
			b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			b.VerticalContentAlignment = VerticalAlignment.Stretch;
			Ui.SetRadius(b, new CornerRadius(8.0));
			b.MouseRightButtonUp += (object _, MouseButtonEventArgs e) =>
			{
				FxMenu(b, idx);
				e.Handled = true;
			};
			b.AllowDrop = true;
			b.Drop += (object _, DragEventArgs e) =>
			{
				OnFxDrop(idx, e);
			};
			_fxViews[i] = new FxView(b, textBlock2, Ui.T(""), border, border2);
			FxGrid.Children.Add(b);
		}
	}

	private void OnFxClick(int idx)
	{
		if (C.Fx[idx].Empty)
		{
			PickFxFile(idx);
		}
		else
		{
			C.PlayFx(idx);
		}
	}

	private void OnFxDrop(int idx, DragEventArgs e)
	{
		if (e.Data.GetData(DataFormats.FileDrop) is string[] array && array.Length > 0)
		{
			if (array.Length == 1 && File.Exists(array[0]) && Catalog.IsAudio(array[0]))
			{
				e.Handled = true;
				DropOverlay.Visibility = Visibility.Collapsed;
				AssignFxAsync(idx, array[0]);
			}
			else if (array.Any(Directory.Exists) || array.Count(Catalog.IsAudio) > 1)
			{
				e.Handled = true;
				DropOverlay.Visibility = Visibility.Collapsed;
				AskFxFiles(array);
			}
		}
	}

	private void PickFxFile(int idx)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Chọn file hiệu ứng cho phím " + (idx + 1),
			Filter = "Nhạc và video|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.mp4;*.m4v;*.mov;*.mkv;*.avi;*.wmv;*.3gp|Tất cả|*.*"
		};
		if (openFileDialog.ShowDialog(this) == true)
		{
			AssignFxAsync(idx, openFileDialog.FileName);
		}
	}

	private void FxMenu(Button anchor, int idx)
	{
		FxSlot e = C.Fx[idx];
		List<Mi> list = new List<Mi>
		{
			new Mi(e.Empty ? "Chọn file…" : "Đổi file…", () =>
			{
				PickFxFile(idx);
			}, "music"),
			new Mi("Chọn cả thư mục hiệu ứng…", PickFxFolder, "folder")
		};
		if (!e.Empty)
		{
			list.Add(new Mi("Đổi tên…", () =>
			{
				AskText("Tên hiệu ứng", e.Name, (string v) =>
				{
					e.Name = v;
					SaveFx();
					UpdateFxLabels();
				});
			}, "pencil"));
			foreach (string key in Catalog.FxColors.Keys)
			{
				string c = key;
				list.Add(new Mi("Màu " + ColorName(c), () =>
				{
					e.Color = c;
					SaveFx();
					UpdateFxLabels();
				}, Catalog.FxColors[c]));
			}
			list.Add(new Mi("Xóa hiệu ứng này…", () =>
			{
				AskDeleteFx(idx);
			}, "trash", Danger: true));
		}
		if (C.Fx.Any((FxSlot f) => !f.Empty))
		{
			list.Add(new Mi("Xóa tất cả hiệu ứng…", AskDeleteAllFx, "trash", Danger: true));
		}
		ShowMenu(anchor, "HIỆU ỨNG PHÍM " + (idx + 1), list);
	}

	private static string ColorName(string c)
	{
		return c switch
		{
			"green" => "xanh lá", 
			"gold" => "vàng", 
			"pink" => "hồng", 
			"blue" => "xanh dương", 
			"violet" => "tím", 
			"teal" => "ngọc", 
			"red" => "đỏ", 
			_ => "xám", 
		};
	}

	private async Task AssignFxAsync(int idx, string path)
	{
		FxSlot e = C.Fx[idx];
		string prev = e.FilePath;
		string text = await LoadFxInto(idx, path);
		if (text != null)
		{
			ShowToast(text, ToastKind.Warn);
			return;
		}
		SaveFx();
		UpdateFxLabels();
		if (prev != null)
		{
			DeleteUnusedFxFiles(new string[1] { prev });
		}
		ShowToast("Đã gán “" + e.Name + "” vào phím " + e.Key, ToastKind.Ok);
	}

	private async Task<string?> LoadFxInto(int idx, string path)
	{
		FxSlot e = C.Fx[idx];
		string dest;
		try
		{
			bool isVideo = Catalog.IsVideo(path);
			string text = (isVideo ? ".m4a" : System.IO.Path.GetExtension(path).ToLowerInvariant());
			dest = System.IO.Path.Combine(AppPaths.FxDir, "fx" + (idx + 1) + "_" + Text.StableHash(path + DateTime.Now.Ticks) + text);
			string result;
			switch (await Task.Run(() =>
			{
				if (isVideo)
				{
					return AudioEngine.ExtractAudio(path, dest);
				}
				File.Copy(path, dest, overwrite: true);
				return 0;
			}))
			{
			default:
				result = "Không lấy được phần tiếng của video này";
				break;
			case -9:
				result = "Video này chỉ có hình, không có tiếng";
				break;
			case 0:
				goto end_IL_0064;
			}
			return result;
			end_IL_0064:;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
			return "Không đọc được file này";
		}
		var (num, dur) = await Task.Run(() => (s: _engine.LoadSample(dest, 600.0, out var duration), d: duration));
		if (num == 0)
		{
			Task.Run(() =>
			{
				MusicLibrary.DeleteFiles(new List<string> { dest });
			});
			return "File này không phát được, chọn file khác";
		}
		if (e.Sample != 0)
		{
			_engine.FreeSample(e.Sample);
		}
		e.Sample = num;
		e.Dur = dur;
		e.FilePath = dest;
		e.Name = Text.FxName(System.IO.Path.GetFileName(path));
		return null;
	}

	private void SaveFx()
	{
		FxSaved[] value = (from f in C.Fx
			where !f.Empty
			select new FxSaved(f.Index, f.Name, f.Color, f.FilePath, f.Dur)).ToArray();
		_lib.SetKv("hieu_ung", JsonSerializer.Serialize(value));
	}

	private void RestoreFx()
	{
		string kv = _lib.GetKv("hieu_ung");
		if (kv != null)
		{
			try
			{
				FxSaved[] array = JsonSerializer.Deserialize<FxSaved[]>(kv) ?? Array.Empty<FxSaved>();
				foreach (FxSaved fxSaved in array)
				{
					int index = fxSaved.Index;
					if ((index >= 0 && index <= 8) || 1 == 0)
					{
						FxSlot obj = C.Fx[fxSaved.Index];
						obj.Name = fxSaved.Name;
						obj.Color = (Catalog.FxColors.ContainsKey(fxSaved.Color) ? fxSaved.Color : "green");
						obj.FilePath = fxSaved.File;
						obj.Dur = fxSaved.Dur;
					}
				}
			}
			catch (JsonException)
			{
			}
		}
		UpdateFxLabels();
	}

	private void LoadAllFx()
	{
		FxSlot[] fx = C.Fx;
		foreach (FxSlot fxSlot in fx)
		{
			if (fxSlot.Empty)
			{
				continue;
			}
			FxSlot slot = fxSlot;
			string path = fxSlot.FilePath;
			Task.Run(() =>
			{
				nint s = (File.Exists(path) ? _engine.LoadSample(path, 600.0, out var _) : 0);
				Dispatcher.BeginInvoke((Action)(() =>
				{
					if (slot.Sample != 0)
					{
						_engine.FreeSample(slot.Sample);
					}
					slot.Sample = s;
				}));
			});
		}
	}

	private void UpdateFxLabels()
	{
		for (int i = 0; i < 9; i++)
		{
			FxSlot fxSlot = C.Fx[i];
			FxView obj = _fxViews[i];
			Brush brush = Ui.Hex(Catalog.FxColors[fxSlot.Color]);
			obj.Name.Text = (fxSlot.Empty ? "(chưa gán)" : fxSlot.Name);
			obj.Name.SetResourceReference(TextBlock.ForegroundProperty, fxSlot.Empty ? "Muted" : "Ink");
			obj.KeyBox.BorderBrush = (fxSlot.Empty ? Ui.Res("Line2") : brush);
			obj.Fill.Background = brush;
			obj.B.ToolTip = (fxSlot.Empty ? "Bấm để chọn file. Kéo thả file (hoặc cả thư mục) vào đây cũng được" : (fxSlot.Name + " · " + Text.Fmt(fxSlot.Dur) + " · bấm chuột phải để đổi"));
		}
	}

	private void UpdateFx()
	{
		List<string> list = new List<string>();
		for (int i = 0; i < 9; i++)
		{
			FxSlot fxSlot = C.Fx[i];
			FxView fxView = _fxViews[i];
			double value = ((fxSlot.Playing && fxSlot.Dur > 0.0) ? (fxSlot.Pos / fxSlot.Dur) : 0.0);
			fxView.Fill.Width = fxView.B.ActualWidth * Math.Clamp(value, 0.0, 1.0);
			if (fxSlot.Playing)
			{
				list.Add(fxSlot.Name + " " + Text.Fmt(fxSlot.Dur - fxSlot.Pos));
				fxView.B.BorderBrush = Ui.Hex(Catalog.FxColors[fxSlot.Color]);
			}
			else
			{
				fxView.B.SetResourceReference(Control.BorderBrushProperty, "BtnLine");
			}
		}
		FxActive.Text = string.Join(" · ", list);
		bool flag = list.Count > 0;
		FxActiveIcon.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		BtnFxStop.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		FxHint.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
	}

	private void OnFxStopAll(object s, RoutedEventArgs e)
	{
		C.StopAllFx();
		UpdateFx();
	}

	private void BuildItemEditor()
	{
		_iWarn = new SegGroup(("10", "10 s"), ("15", "15 s"), ("20", "20 s"), ("30", "30 s"));
		_iWarn.Picked += (string v) =>
		{
			if (_editItem != null)
			{
				_editItem.Warn = int.Parse(v);
				_iWarn.Value = v;
				ItemChanged();
			}
		};
		_iFi = new TextBox
		{
			Height = 32.0,
			FontSize = 14.0
		};
		_iFo = new TextBox
		{
			Height = 32.0,
			FontSize = 14.0
		};
		_iFi.TextChanged += (object _, TextChangedEventArgs _) =>
		{
			OnFadeBox(_iFi, (GSoundPlayer.Model.Track t, double v) =>
			{
				t.FadeIn = v;
			});
		};
		_iFo.TextChanged += (object _, TextChangedEventArgs _) =>
		{
			OnFadeBox(_iFo, (GSoundPlayer.Model.Track t, double v) =>
			{
				t.FadeOut = v;
			});
		};
		Grid grid = new Grid
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 11.0)
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(10.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		StackPanel element = Ui.Col(WithMargin(Ui.T("Lên dần đầu bài (giây)", 12.5, "Ink2"), 0.0, 0.0, 0.0, 4.0), _iFi);
		StackPanel element2 = Ui.Col(WithMargin(Ui.T("Tắt dần cuối bài (giây)", 12.5, "Ink2"), 0.0, 0.0, 0.0, 4.0), _iFo);
		Grid.SetColumn(element2, 2);
		grid.Children.Add(element);
		grid.Children.Add(element2);
		_iVol = new Slider
		{
			Minimum = -20.0,
			Maximum = 6.0,
			SmallChange = 0.5,
			LargeChange = 1.0,
			TickFrequency = 0.5,
			IsSnapToTickEnabled = true,
			VerticalAlignment = VerticalAlignment.Center
		};
		_iVol.ValueChanged += (object _, RoutedPropertyChangedEventArgs<double> _) =>
		{
			if (!_iFilling && _editItem != null)
			{
				_editItem.Vol = _iVol.Value;
				_iVolV.Text = Text.Db(_iVol.Value);
				ItemChanged();
			}
		};
		_iVolV = Ui.T("", 13.0, "Ink", FontWeights.Bold);
		_iVolV.Width = 60.0;
		_iVolV.TextAlignment = TextAlignment.Right;
		DockPanel dockPanel = new DockPanel();
		DockPanel.SetDock(_iVolV, Dock.Right);
		dockPanel.Children.Add(_iVolV);
		dockPanel.Children.Add(_iVol);
		_iLoudT = Ui.T("", 13.0, "Ink2", null, trim: false, wrap: true);
		_iLoud = new CheckBox
		{
			Content = _iLoudT
		};
		_iLoud.Click += (object _, RoutedEventArgs _) =>
		{
			if (_editItem != null)
			{
				_editItem.Loud = _iLoud.IsChecked == true;
				ItemChanged();
			}
		};
		ItemRight.Children.Add(grid);
		ItemRight.Children.Add(Row("Báo sắp hết trước", _iWarn));
		ItemRight.Children.Add(Row("Âm lượng", dockPanel));
		ItemRight.Children.Add(_iLoud);
		static FrameworkElement Row(string label, FrameworkElement ctl)
		{
			DockPanel dockPanel2 = new DockPanel
			{
				Margin = new Thickness(0.0, 0.0, 0.0, 11.0)
			};
			TextBlock textBlock = Ui.T(label, 13.0, "Ink2");
			textBlock.Width = 118.0;
			DockPanel.SetDock(textBlock, Dock.Left);
			dockPanel2.Children.Add(textBlock);
			dockPanel2.Children.Add(ctl);
			return dockPanel2;
		}
	}

	private void OnFadeBox(TextBox b, Action<GSoundPlayer.Model.Track, double> set)
	{
		if (!_iFilling && _editItem != null && double.TryParse(b.Text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			set(_editItem, Math.Clamp(result, 0.0, 15.0));
			ItemChanged();
		}
	}

	private void OpenItemEd(GSoundPlayer.Model.Track it)
	{
		_editItem = it;
		FillItemEd();
		ItemEd.Visibility = Visibility.Visible;
	}

	private void FillItemEd()
	{
		GSoundPlayer.Model.Track editItem = _editItem;
		if (editItem != null)
		{
			_iFilling = true;
			IName.Text = editItem.Name;
			INote.Text = editItem.Note;
			IFile.Text = editItem.FileName + " · dài " + Text.Fmt(editItem.Dur);
			_iWarn.Value = editItem.Warn.ToString();
			_iFi.Text = Text.Dec(editItem.FadeIn).Replace(",0", "");
			_iFo.Text = Text.Dec(editItem.FadeOut).Replace(",0", "");
			_iVol.Value = editItem.Vol;
			_iVolV.Text = Text.Db(editItem.Vol);
			_iLoud.IsChecked = editItem.Loud;
			double? lufs = editItem.Lufs;
			if (lufs.HasValue)
			{
				double valueOrDefault = lufs.GetValueOrDefault();
				double num = Math.Clamp(-16.0 - valueOrDefault, -20.0, 6.0);
				_iLoudT.Text = "Tự cân độ to: bài đo được " + Text.Dec(valueOrDefault) + " LUFS → " + ((num >= 0.0) ? "nâng " : "hạ ") + Text.Dec(Math.Abs(num)) + " dB cho bằng các bài khác";
			}
			else
			{
				_iLoudT.Text = "Tự cân độ to: đang đo…";
			}
			_iFilling = false;
			DrawItemWave();
		}
	}

	private void CloseItemEd()
	{
		if (_editItem != null)
		{
			_editItem = null;
			ItemEd.Visibility = Visibility.Collapsed;
			Root.Focus();
			_needRender = true;
		}
	}

	private void OnItemClose(object s, RoutedEventArgs e)
	{
		CloseItemEd();
	}

	private void OnItemReplace(object s, RoutedEventArgs e)
	{
		GSoundPlayer.Model.Track editItem = _editItem;
		if (editItem == null)
		{
			return;
		}
		if (C.IsPlaying(editItem))
		{
			ShowToast("Bài này đang phát: dừng bài rồi mới thay được", ToastKind.Warn);
			return;
		}
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Chọn bài thay cho “" + editItem.Name + "”",
			Filter = "Nhạc và video|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.mp4;*.m4v;*.mov;*.mkv;*.avi;*.wmv;*.3gp|Tất cả|*.*"
		};
		if (openFileDialog.ShowDialog(this) == true)
		{
			ReplaceTrackAsync(editItem, openFileDialog.FileName);
		}
	}

	private async Task ReplaceTrackAsync(GSoundPlayer.Model.Track it, string path)
	{
		ShowToast("Đang đọc bài mới…");
		string text = await _lib.ReplaceFileAsync(it, path);
		if (text != null)
		{
			ShowToast(text, ToastKind.Warn);
			return;
		}
		C.ForgetCut(it);
		if (_editItem == it)
		{
			FillItemEd();
		}
		_needRender = true;
		ShowToast("Đã thay bằng “" + it.Name + "”. Đoạn phát, fade, âm lượng riêng đã đặt lại theo bài mới", ToastKind.Ok);
	}

	private void ItemChanged()
	{
		if (_editItem != null)
		{
			_editItem.Edited = true;
			C.SyncItem(_editItem);
			_lib.Save(_editItem);
			_needRender = true;
		}
	}

	private void OnItemName(object s, TextChangedEventArgs e)
	{
		if (!_iFilling && _editItem != null)
		{
			string text = IName.Text.Trim();
			_editItem.Name = ((text.Length > 0) ? text : "Chưa đặt tên");
			_editItem.Junk = false;
			ItemChanged();
		}
	}

	private void OnItemNote(object s, TextChangedEventArgs e)
	{
		if (!_iFilling && _editItem != null)
		{
			_editItem.Note = INote.Text;
			ItemChanged();
		}
	}

	private void DrawItemWave()
	{
		GSoundPlayer.Model.Track it = _editItem;
		if (it != null)
		{
			Player player = C.Players.FirstOrDefault((Player q) => q.Item == it);
			IWave.Wave = it.Wave;
			IWave.Dur = ((it.Dur > 0.0) ? it.Dur : 1.0);
			IWave.InP = it.InP;
			IWave.OutP = it.Out;
			IWave.FadeIn = it.FadeIn;
			IWave.FadeOut = it.FadeOut;
			IWave.Pos = player?.Pos;
			IWave.Refresh();
			IIn.Text = Text.Fmt(it.InP, tenth: true);
			IOut.Text = Text.Fmt(it.Out, tenth: true);
			ILen.Text = Text.Fmt(it.Len, tenth: true);
			BtnItemDelete.IsEnabled = !C.IsSounding(it);
			BtnItemReplace.IsEnabled = !C.IsPlaying(it);
			BtnItemReset.IsEnabled = it.InP != 0.0 || it.OutP.HasValue || it.FadeIn != 0.0 || it.FadeOut != 0.0 || it.Vol != 0.0;
		}
	}

	private double IWaveT(MouseEventArgs e)
	{
		GSoundPlayer.Model.Track editItem = _editItem;
		return Math.Clamp(e.GetPosition(IWave).X / Math.Max(1.0, IWave.ActualWidth), 0.0, 1.0) * editItem.Dur;
	}

	private void OnIWaveDown(object s, MouseButtonEventArgs e)
	{
		if (_editItem != null)
		{
			double num = IWaveT(e);
			_iDrag = ((Math.Abs(num - _editItem.InP) <= Math.Abs(num - _editItem.Out)) ? "in" : "out");
			IWave.CaptureMouse();
			OnIWaveMove(s, e);
		}
	}

	private void OnIWaveMove(object s, MouseEventArgs e)
	{
		if (_iDrag != null && _editItem != null)
		{
			GSoundPlayer.Model.Track editItem = _editItem;
			double val = Math.Round(IWaveT(e) * 10.0) / 10.0;
			if (_iDrag == "in")
			{
				editItem.InP = Math.Max(0.0, Math.Min(val, editItem.Out - 1.0));
				editItem.AutoTrim = false;
			}
			else
			{
				double num = Math.Min(editItem.Dur, Math.Max(val, editItem.InP + 1.0));
				editItem.OutP = ((num >= editItem.Dur - 0.05) ? ((double?)null) : new double?(num));
			}
			ItemChanged();
		}
	}

	private void OnIWaveUp(object s, MouseButtonEventArgs e)
	{
		_iDrag = null;
		IWave.ReleaseMouseCapture();
	}

	private void OnItemSilence(object s, RoutedEventArgs e)
	{
		GSoundPlayer.Model.Track editItem = _editItem;
		if (editItem != null)
		{
			BoSilenceAsync(editItem);
		}
	}

	private async Task BoSilenceAsync(GSoundPlayer.Model.Track it)
	{
		string path = it.FilePath;
		var (num, analysisResult) = await Task.Run(() => (c: AudioEngine.Analyze(path, out var r, null), res: r));
		if (num != 0)
		{
			ShowToast("Không đọc được file này", ToastKind.Warn);
			return;
		}
		if (analysisResult.SilenceHeadS < 0.05 || it.InP >= analysisResult.SilenceHeadS)
		{
			ShowToast("Đầu đoạn đã không còn khoảng im");
			return;
		}
		it.InP = Math.Round(analysisResult.SilenceHeadS, 2);
		it.AutoTrim = true;
		ItemChanged();
		ShowToast("Đã bỏ " + Text.Dec(analysisResult.SilenceHeadS) + " giây im đầu bài", ToastKind.Ok);
	}

	private void OnItemReset(object s, RoutedEventArgs e)
	{
		GSoundPlayer.Model.Track editItem = _editItem;
		if (editItem != null)
		{
			editItem.InP = 0.0;
			editItem.OutP = null;
			editItem.FadeIn = 0.0;
			editItem.FadeOut = 0.0;
			editItem.Vol = 0.0;
			editItem.AutoTrim = false;
			ItemChanged();
			editItem.Edited = false;
			_lib.Save(editItem);
			FillItemEd();
			ShowToast("Đã trả bài về nguyên bản", ToastKind.Ok);
		}
	}

	private void InitLicense()
	{
		_licDev = false;
		_lic = GSoundPlayer.Licensing.LicenseManager.ForThisPc(() => _store.GetKv("bq"), (string v) =>
		{
			_store.SetKv("bq", v);
		});
		_lic.Refresh(DateTimeOffset.Now);
	}

	private void UpdateLicensePill()
	{
		LicenseStatus status = _lic.Status;
		var (text, flag) = status.Mode switch
		{
			LicenseMode.Licensed => ("", false), 
			LicenseMode.Trial => ((status.DaysLeft <= 1) ? "Dùng thử · hôm nay là ngày cuối" : $"Dùng thử · còn {status.DaysLeft} ngày", status.DaysLeft <= 2), 
			LicenseMode.Emergency => ($"Mở khẩn cấp · tới {status.EmergencyUntil:HH:mm dd/MM}", true), 
			_ => ("Hết hạn dùng thử", true), 
		};
		BtnTrial.Content = text;
		BtnTrial.Visibility = ((text.Length <= 0) ? Visibility.Collapsed : Visibility.Visible);
		Ui.SetState(BtnTrial, flag ? "warn" : "");
		Tip.SetText(BtnTrial, "Mua key 100.000đ, dùng vĩnh viễn. Xem mã máy, nhập key");
		if (_licSetText != null)
		{
			_licSetText.Text = ((status.Mode == LicenseMode.Licensed) ? "Đã kích hoạt · vĩnh viễn" : text);
		}
	}

	private void OnLicenseClick(object s, RoutedEventArgs e)
	{
		SetPop.Visibility = Visibility.Collapsed;
		ShowLicense();
	}

	private void ShowLicense()
	{
		_lockShown = false;
		ShowModal(BuildLicensePanel(), 620.0);
		_lockShown = _lic.Status.Locked;
	}

	private void RedrawLicense()
	{
		if (ModalOpen)
		{
			ModalScroll.Content = BuildLicensePanel();
		}
	}

	private async void Unlock(string msg)
	{
		bool lockShown = _lockShown;
		_lockShown = false;
		UpdateLicensePill();
		if (!lockShown)
		{
			RedrawLicense();
			ShowToast(msg, ToastKind.Ok);
			return;
		}
		CloseModal();
		ShowToast(msg, ToastKind.Ok);
		if (!_licDev)
		{
			await StartAudioAsync();
		}
	}

	private FrameworkElement BuildLicensePanel()
	{
		LicenseStatus status = _lic.Status;
		bool locked = status.Locked;
		StackPanel stackPanel = new StackPanel();
		Version version = typeof(MainWindow).Assembly.GetName().Version;
		TextBlock element = Ui.T((version == null) ? "" : $"Phiên bản {version.Major}.{version.Minor}.{version.Build}", 12.5, "Muted");
		DockPanel dockPanel = new DockPanel
		{
			Margin = new Thickness(0.0, 0.0, 0.0, 14.0)
		};
		if (!locked)
		{
			Button button = Ui.IconBtn("Btn", "x", CloseModal, "Đóng (Esc)", 14.0);
			button.Width = 30.0;
			button.Height = 30.0;
			button.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
			DockPanel.SetDock(button, Dock.Right);
			dockPanel.Children.Add(button);
		}
		DockPanel.SetDock(element, Dock.Right);
		dockPanel.Children.Add(element);
		dockPanel.Children.Add(Ui.T("Bản quyền CTECSoundPlayer", 19.0, "Ink", FontWeights.Bold));
		stackPanel.Children.Add(dockPanel);
		stackPanel.Children.Add(StatusCard(status));
		if (status.Mode != LicenseMode.Licensed)
		{
			stackPanel.Children.Add(BuyBlock());
		}
		else
		{
			stackPanel.Children.Add(MachineRow(big: false));
		}
		stackPanel.Children.Add(AuthorCard());
		stackPanel.Children.Add(LicenseFooter(status));
		return stackPanel;
	}

	private Border StatusCard(LicenseStatus st)
	{
		string text = st.LastDay.ToString("dd/MM/yyyy");
		(string, string, string, string, string) tuple = st.Mode switch
		{
			LicenseMode.Licensed => ("check", "Green", "GreenTint", "Đã kích hoạt · Vĩnh viễn", $"Đăng ký cho {st.License.Customer}. Key số {st.License.Id:0000}, cấp ngày {st.License.Issued:dd/MM/yyyy}. " + "Được cập nhật miễn phí mọi bản mới, hỗ trợ cài đặt, sử dụng qua Zalo."), 
			LicenseMode.Trial => (null, (st.DaysLeft <= 2) ? "Amber" : "Line2", (st.DaysLeft <= 2) ? "AmberTint" : "Panel2", (st.DaysLeft <= 1) ? "Đang dùng thử · hôm nay là ngày cuối" : $"Đang dùng thử · còn {st.DaysLeft} ngày", "Dùng đủ tính năng tới hết ngày " + text + ". Sau đó phải nhập key mới phát được nhạc; kho nhạc, nhóm, hiệu ứng vẫn giữ nguyên."), 
			LicenseMode.Emergency => ("alert", "Amber", "AmberTint", $"Đang mở khẩn cấp · tới {st.EmergencyUntil:HH:mm} ngày {st.EmergencyUntil:dd/MM}", "Máy này đã dùng lần mở khẩn cấp duy nhất. Nhập key trước khi hết giờ để dùng tiếp."), 
			LicenseMode.ClockBack => ("alert", "Red", "RedTint", "Ngày giờ trên máy đang bị lùi", "Ngày giờ của Windows sớm hơn lần mở phần mềm trước. Chỉnh lại ngày giờ cho đúng rồi mở lại phần mềm, hoặc nhập key để dùng ngay."), 
			_ => ("alert", "Red", "RedTint", "Hết hạn dùng thử", "Dùng thử đã hết sau ngày " + text + ". Nhập key để phát nhạc tiếp; kho nhạc, nhóm, hiệu ứng vẫn còn nguyên."), 
		};
		string item = tuple.Item1;
		string item2 = tuple.Item2;
		string item3 = tuple.Item3;
		string item4 = tuple.Item4;
		string item5 = tuple.Item5;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		if (item != null)
		{
			IconView iconView = Ui.Icon(item, 20.0);
			iconView.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			IconView iconView2 = iconView;
			DependencyProperty foregroundProperty = IconView.ForegroundProperty;
			string name;
			if (item2 == "Green")
			{
				name = "GreenText";
			}
			else
			{
				name = ((item2 == "Amber") ? "AmberText" : "Red");
			}
			iconView2.SetResourceReference(foregroundProperty, name);
			stackPanel.Children.Add(iconView);
		}
		stackPanel.Children.Add(Ui.T(item4, 17.0, "Ink", FontWeights.Bold));
		TextBlock textBlock = Ui.T(item5, 13.5, "Ink2", null, trim: false, wrap: true);
		textBlock.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		Border border = Ui.Card(Ui.Col(stackPanel, textBlock), new Thickness(14.0, 12.0, 14.0, 12.0), item2, item3);
		border.BorderThickness = new Thickness(1.5);
		return border;
	}

	private FrameworkElement BuyBlock()
	{
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(Ui.H3("MUA KEY"));
		TextBlock textBlock = new TextBlock
		{
			VerticalAlignment = VerticalAlignment.Center
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
		textBlock.Inlines.Add(new Run("100.000đ")
		{
			FontSize = 24.0,
			FontWeight = FontWeights.Bold
		});
		Run run = new Run("   mua một lần, dùng vĩnh viễn")
		{
			FontSize = 14.5,
			FontWeight = FontWeights.SemiBold
		};
		run.SetResourceReference(TextElement.ForegroundProperty, "AmberText");
		textBlock.Inlines.Add(run);
		stackPanel.Children.Add(textBlock);
		WrapPanel wrapPanel = new WrapPanel
		{
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		string[] perks = Perks;
		foreach (string text in perks)
		{
			IconView iconView = Ui.Icon("check", 15.0);
			iconView.SetResourceReference(IconView.ForegroundProperty, "GreenText");
			iconView.Margin = new Thickness(0.0, 0.0, 5.0, 0.0);
			StackPanel stackPanel2 = Ui.Row(0.0, iconView, Ui.T(text, 13.5));
			stackPanel2.Margin = new Thickness(0.0, 2.0, 18.0, 2.0);
			wrapPanel.Children.Add(stackPanel2);
		}
		stackPanel.Children.Add(wrapPanel);
		TextBlock textBlock2 = Ui.T("Gửi mã máy dưới đây qua Zalo, chuyển khoản xong sẽ nhận key.", 13.5, "Ink2", null, trim: false, wrap: true);
		textBlock2.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
		stackPanel.Children.Add(textBlock2);
		stackPanel.Children.Add(MachineRow(big: true));
		TextBlock textBlock3 = Ui.T("Dán key nhận được vào đây (bắt đầu bằng GSP1):", 13.0, "Ink2");
		textBlock3.Margin = new Thickness(0.0, 14.0, 0.0, 6.0);
		stackPanel.Children.Add(textBlock3);
		TextBox box = new TextBox
		{
			Height = 62.0,
			FontSize = 13.5,
			FontFamily = new FontFamily("Consolas"),
			TextWrapping = TextWrapping.Wrap,
			AcceptsReturn = true,
			VerticalContentAlignment = VerticalAlignment.Top,
			Padding = new Thickness(6.0, 5.0, 6.0, 5.0)
		};
		TextBlock err = Ui.T("", 13.0, "RedText", FontWeights.SemiBold, trim: false, wrap: true);
		err.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		err.Visibility = Visibility.Collapsed;
		box.PreviewKeyDown += (object _, KeyEventArgs e) =>
		{
			if (e.Key == Key.Return && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
			{
				e.Handled = true;
				Activate();
			}
		};
		Button button = Ui.Primary("Kích hoạt", Activate);
		button.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
		button.Height = 62.0;
		button.VerticalAlignment = VerticalAlignment.Top;
		DockPanel dockPanel = new DockPanel();
		DockPanel.SetDock(button, Dock.Right);
		dockPanel.Children.Add(button);
		dockPanel.Children.Add(box);
		stackPanel.Children.Add(dockPanel);
		stackPanel.Children.Add(err);
		return stackPanel;
		void Activate()
		{
			KeyCheck keyCheck = _lic.Activate(box.Text, out LicenseInfo info);
			if (keyCheck == KeyCheck.Ok)
			{
				Unlock("Đã kích hoạt bản quyền cho " + info.Customer + ". Cảm ơn đã ủng hộ CTECSoundPlayer!");
			}
			else
			{
				TextBlock textBlock4 = err;
				textBlock4.Text = keyCheck switch
				{
					KeyCheck.Empty => "Chưa dán key. Chép cả đoạn key trong tin nhắn Zalo rồi dán vào ô trên.", 
					KeyCheck.WrongMachine => $"Key này cấp cho máy khác (mã máy {MachineId.Code(info.Machine)}), không dùng được trên máy này. Gửi mã máy {_lic.MachineCode} để nhận key cho máy này.", 
					KeyCheck.WrongProduct => "Key này không dành cho CTECSoundPlayer trên Windows.", 
					KeyCheck.NewerFormat => "Key này dành cho bản CTECSoundPlayer mới hơn. Tải bản mới rồi nhập lại.", 
					_ => "Key không đúng. Chép lại đủ cả đoạn key trong tin nhắn (có thể bị thiếu, thừa ký tự) rồi dán lại.", 
				};
				err.Visibility = Visibility.Visible;
				box.Focus();
			}
		}
	}

	private FrameworkElement MachineRow(bool big)
	{
		TextBlock textBlock = new TextBlock
		{
			Text = _lic.MachineCode,
			FontFamily = new FontFamily("Consolas"),
			FontSize = (big ? 22 : 15),
			FontWeight = FontWeights.Bold,
			VerticalAlignment = VerticalAlignment.Center
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
		TextBlock textBlock2 = Ui.T("Mã máy", 13.0, "Ink2");
		textBlock2.Width = 70.0;
		Button item = CopyBtn("Sao chép", _lic.MachineCode);
		List<UIElement> list = new List<UIElement> { textBlock2, textBlock, item };
		if (big)
		{
			Button button = Ui.Btn("HBtn", "Nhắn Zalo", () =>
			{
				OpenUrl("https://zalo.me/0973630052");
			}, "Mở Zalo nhắn tới 0973630052 (Bùi Linh), dán mã máy vào tin nhắn");
			button.Height = 30.0;
			list.Add(button);
		}
		StackPanel stackPanel = Ui.Row(10.0, list.ToArray());
		stackPanel.Margin = new Thickness(0.0, big ? 10 : 12, 0.0, 0.0);
		return stackPanel;
	}

	private static Button CopyBtn(string label, string value)
	{
		Button b = null;
		b = Ui.Btn("HBtn", label, () =>
		{
			try
			{
				Clipboard.SetText(value);
				b.Content = "Đã chép";
			}
			catch (ExternalException)
			{
				b.Content = "Chưa chép được, bấm lại";
			}
		});
		b.Height = 30.0;
		return b;
	}

	private FrameworkElement AuthorCard()
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(Line("Tác giả", new Inline[1]
		{
			new Run("Tiến Vinh | GSound Karaoke")
			{
				FontWeight = FontWeights.Bold
			}
		}));
		stackPanel.Children.Add(Line("Zalo", new Inline[2]
		{
			Link("0973630052", "https://zalo.me/0973630052", "Mở Zalo nhắn tới 0973630052"),
			new Run(" (Bùi Linh)")
		}));
		stackPanel.Children.Add(Line("TikTok", new Inline[1] { Link("@tienvinhaudio", "https://www.tiktok.com/@tienvinhaudio", "Mở trang TikTok @tienvinhaudio") }));
		Border border = Ui.Card(stackPanel, new Thickness(14.0, 9.0, 14.0, 9.0), "Line2");
		border.Margin = new Thickness(0.0, 18.0, 0.0, 0.0);
		return border;
		static FrameworkElement Line(string label, params Inline[] value)
		{
			TextBlock textBlock = Ui.T(label, 13.0, "Ink2");
			textBlock.Width = 70.0;
			TextBlock textBlock2 = new TextBlock
			{
				FontSize = 14.0,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.Wrap
			};
			textBlock2.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
			textBlock2.Inlines.AddRange(value);
			DockPanel dockPanel = new DockPanel
			{
				Margin = new Thickness(0.0, 3.0, 0.0, 3.0)
			};
			DockPanel.SetDock(textBlock, Dock.Left);
			dockPanel.Children.Add(textBlock);
			dockPanel.Children.Add(textBlock2);
			return dockPanel;
		}
		Hyperlink Link(string text, string url, string tip)
		{
			Hyperlink hyperlink = new Hyperlink(new Run(text));
			hyperlink.ToolTip = tip;
			hyperlink.FontWeight = FontWeights.Bold;
			hyperlink.SetResourceReference(TextElement.ForegroundProperty, "BlueText");
			hyperlink.Click += (object _, RoutedEventArgs _) =>
			{
				OpenUrl(url);
			};
			return hyperlink;
		}
	}

	private FrameworkElement LicenseFooter(LicenseStatus st)
	{
		DockPanel foot = new DockPanel
		{
			Margin = new Thickness(0.0, 16.0, 0.0, 0.0),
			LastChildFill = false
		};
		if (!st.Locked)
		{
			Button element = Ui.Secondary("Đóng", CloseModal);
			DockPanel.SetDock(element, Dock.Right);
			foot.Children.Add(element);
			return foot;
		}
		Button element2 = Ui.Secondary("Thoát phần mềm", () =>
		{
			_confirmedExit = true;
			Close();
		});
		DockPanel.SetDock(element2, Dock.Right);
		foot.Children.Add(element2);
		if (st.CanUseEmergency)
		{
			Button button = Ui.Btn("Secondary", "Mở khẩn cấp 24 giờ…", null, "Cho đội đang tới chỗ diễn mà chưa kịp mua key. Mỗi máy một lần");
			button.Click += (object _, RoutedEventArgs _) =>
			{
				foot.Children.Clear();
				TextBlock textBlock = Ui.T("Mỗi máy chỉ mở khẩn cấp được một lần, dùng đủ tính năng trong 24 giờ. Mở ngay?", 13.5, "AmberText", FontWeights.SemiBold, trim: false, wrap: true);
				Button button2 = Ui.Secondary("Thôi", RedrawLicense);
				Button button3 = Ui.Primary("Mở 24 giờ", () =>
				{
					if (_lic.UseEmergency(DateTimeOffset.Now))
					{
						Unlock($"Đã mở khẩn cấp tới {_lic.Status.EmergencyUntil:HH:mm dd/MM}. Nhập key trước khi hết giờ để dùng tiếp");
					}
				});
				StackPanel element4 = Ui.Row(8.0, button2, button3);
				DockPanel.SetDock(element4, Dock.Right);
				textBlock.Margin = new Thickness(0.0, 0.0, 12.0, 0.0);
				textBlock.MaxWidth = 330.0;
				foot.LastChildFill = true;
				foot.Children.Add(element4);
				foot.Children.Add(textBlock);
			};
			DockPanel.SetDock(button, Dock.Left);
			foot.Children.Add(button);
		}
		else if (st.Mode == LicenseMode.Expired)
		{
			TextBlock element3 = Ui.T("Máy này đã dùng lần mở khẩn cấp.", 13.0, "Muted");
			DockPanel.SetDock(element3, Dock.Left);
			foot.Children.Add(element3);
		}
		return foot;
	}

	private void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
		catch (Win32Exception)
		{
			ShowToast("Không mở được trình duyệt. Zalo: 0973630052", ToastKind.Warn);
		}
	}

	private void SetBanner(string st, string text)
	{
		StBannerText.Text = text;
		(string, string) tuple;
		switch (st)
		{
		case "paused":
		case "fading":
			tuple = ("Amber", null);
			break;
		case "duck":
			tuple = ("Blue", null);
			break;
		case "replug":
			tuple = ("Green", null);
			break;
		default:
			tuple = (null, "Muted");
			break;
		}
		var (text2, name) = tuple;
		if (text2 != null)
		{
			StBanner.SetResourceReference(Border.BackgroundProperty, text2);
		}
		else
		{
			StBanner.Background = null;
		}
		bool flag;
		switch (st)
		{
		case "duck":
			StBannerText.Foreground = Ui.Hex("#06182e");
			return;
		case "replug":
			StBannerText.Foreground = Ui.Hex("#ffffff");
			return;
		case "paused":
		case "fading":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			StBannerText.Foreground = Ui.Hex("#1b1407");
		}
		else
		{
			StBannerText.SetResourceReference(TextBlock.ForegroundProperty, name);
		}
	}

	private void UpdateNow()
	{
		Player player = C.Current();
		List<Player> list = C.Players.Where((Player q) => q.Fade == FadeReason.Replace).ToList();
		FadingInfo.Text = ((list.Count > 0) ? ("Đang chuyển bài: " + string.Join(", ", list.Select((Player q) => q.Name + " nhỏ dần"))) : "");
		bool flag = C.Players.Any((Player q) => q.JackPaused);
		bool flag2 = C.Players.Any((Player q) => q.Paused);
		Player player2 = C.UserFades().FirstOrDefault();
		double num = (C.DuckOn ? 1.0 : 0.0);
		_duckVisual += (double)Math.Sign(num - _duckVisual) * Math.Min(Math.Abs(num - _duckVisual), (C.DuckOn ? 2.0 : (2.0 / 3.0)) * 0.033);
		string text = "";
		string text2 = ((player == null) ? "" : "CÒN");
		if (flag && !C.JackLost)
		{
			text = "replug";
			text2 = "BẤM P PHÁT TIẾP";
		}
		else if (flag2)
		{
			text = "paused";
			text2 = "TẠM DỪNG · bấm P";
		}
		else if (player2 != null)
		{
			text = "fading";
			text2 = "TẮT DẦN " + Text.Dec(player2.FadeRemain) + " s";
		}
		else if (C.DuckOn || _duckVisual > 0.02)
		{
			text = "duck";
			text2 = "HẠ NHẠC −12 dB";
		}
		SetBanner(text, text2);
		BtnCancelFade.Visibility = ((!(text == "fading")) ? Visibility.Collapsed : Visibility.Visible);
		PauseIcon.Kind = (flag2 ? "play" : "pause");
		Tip.SetText(BtnPause, flag2 ? "Phát tiếp" : "Tạm dừng");
		Ui.SetState(BtnPause, flag2 ? "on" : "");
		Ui.SetState(BtnDuck, C.DuckOn ? "blue" : "");
		if (player == null)
		{
			GSoundPlayer.Model.Track nextUp = C.NextUp;
			string text3 = "idle|" + nextUp?.Name + "|" + C.JackLost;
			if (_lastNowKey != text3)
			{
				_lastNowKey = text3;
				NowTagText.Text = "KHÔNG CÓ NHẠC";
				NowTag.SetResourceReference(Border.BackgroundProperty, "Line2");
				NowTagText.SetResourceReference(TextBlock.ForegroundProperty, "Ink2");
				NowTitle.Text = "Đang im";
				TextBlock endLine = EndLine;
				string text4;
				if (C.JackLost)
				{
					text4 = "Chưa có Output: cắm lại dây hoặc chọn Output ở Tùy chỉnh";
				}
				else
				{
					text4 = ((nextUp != null) ? ("Bấm GO (Space) để phát: " + nextUp.Name) : "Chọn một bài trong kho, rồi bấm GO (Space)");
				}
				endLine.Text = text4;
				EndLine.SetResourceReference(TextBlock.ForegroundProperty, C.JackLost ? "AmberText" : "Ink2");
				Remain.Text = "--:--";
				Remain.SetResourceReference(TextBlock.ForegroundProperty, "Line2");
				Remain.Opacity = 1.0;
				Ui.SetState(BtnLoop, "");
				Wave.Wave = null;
				Wave.Dur = 1.0;
				Wave.InP = 0.0;
				Wave.OutP = 1.0;
				Wave.Pos = null;
				Wave.FadeIn = (Wave.FadeOut = 0.0);
				Wave.Dim = false;
				Wave.Ghost = null;
				Wave.Refresh();
			}
			return;
		}
		double num2 = player.OutP - player.Pos;
		string text5 = player.Voice + "|" + player.Paused + "|" + player.Name;
		if (text5 != _lastNowKey)
		{
			_lastNowKey = text5;
			if (_scrub == null)
			{
				Wave.Ghost = null;
			}
			NowTagText.Text = (player.Paused ? "TẠM DỪNG" : "ĐANG PHÁT");
			NowTag.SetResourceReference(Border.BackgroundProperty, player.Paused ? "Amber" : "Green");
			NowTagText.Foreground = (player.Paused ? Ui.Hex("#1b1407") : Ui.Hex("#ffffff"));
			NowTitle.Text = player.Name;
			Wave.Wave = player.Item.Wave;
		}
		if (Wave.Wave != player.Item.Wave)
		{
			Wave.Wave = player.Item.Wave;
		}
		Ui.SetState(BtnLoop, player.LoopOn ? "blue" : "");
		string name = "Ink";
		string text6;
		if (player.LoopOn)
		{
			text6 = "Hết bài: LẶP LẠI (bấm L để tắt)";
			name = "BlueText";
		}
		else
		{
			GSoundPlayer.Model.Track track = C.NextQ();
			if (track == null)
			{
				text6 = (C.QueueDone ? "Hết bài: DỪNG (đã phát hết hàng chờ)" : "Hết bài: DỪNG (hàng chờ trống)");
			}
			else if (!C.AutoQueue)
			{
				text6 = "Hết bài: DỪNG · bài chờ: bấm GO";
				name = "AmberText";
			}
			else
			{
				text6 = "Hết bài: TỰ PHÁT " + track.Name + (C.NextWraps ? " (lặp lại hàng chờ)" : "");
				name = "GreenText";
			}
		}
		EndLine.Text = text6;
		EndLine.SetResourceReference(TextBlock.ForegroundProperty, name);
		Remain.Text = Text.Fmt(num2);
		bool flag3 = !player.LoopOn && !player.Paused && num2 <= (double)Math.Min(5, player.Warn);
		bool flag4 = !player.LoopOn && !player.Paused && num2 <= (double)player.Warn;
		if ((text == "paused" || text == "replug") ? true : false)
		{
			Remain.SetResourceReference(TextBlock.ForegroundProperty, "AmberText");
		}
		else if (text == "duck")
		{
			Remain.SetResourceReference(TextBlock.ForegroundProperty, "BlueText");
		}
		else if (flag3)
		{
			Remain.SetResourceReference(TextBlock.ForegroundProperty, "Red");
		}
		else if (flag4)
		{
			Remain.SetResourceReference(TextBlock.ForegroundProperty, "AmberText");
		}
		else
		{
			Remain.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
		}
		Remain.Opacity = ((flag3 && text == "" && _tickCount / 9 % 2 == 1) ? 0.35 : 1.0);
		Wave.Dur = ((player.Item.Dur > 0.0) ? player.Item.Dur : player.OutP);
		Wave.InP = player.InP;
		Wave.OutP = player.OutP;
		Wave.Pos = player.Pos;
		Wave.FadeIn = player.FadeIn;
		Wave.FadeOut = player.FadeOut;
		Wave.Dim = player.Paused;
		Wave.Refresh();
	}

	private void UpdateMeters()
	{
		(float L, float R) tuple = _engine.ReadMeters();
		float item = tuple.L;
		float item2 = tuple.R;
		double[] array = new double[2] { item, item2 };
		for (int i = 0; i < 2; i++)
		{
			double val = Math.Min(1.0, array[i]);
			_lvl[i] = Math.Max(val, _lvl[i] * 0.86);
			_peak[i] = Math.Max(_lvl[i], _peak[i] - 0.006);
		}
		MeterL.Set(_lvl[0], _peak[0]);
		MeterR.Set(_lvl[1], _peak[1]);
	}

	private void UpdateGo()
	{
		double num = C.GoLock - C.NowMs;
		bool flag = num > 0.0;
		BtnGo.Opacity = (flag ? 0.6 : 1.0);
		LockBar.Width = (flag ? (BtnGo.ActualWidth * (num / C.GoLockLen)) : 0.0);
		GoSmall.Text = (flag ? ("chờ " + Text.Dec(num / 1000.0) + " s") : "Space");
		bool flag2 = C.NextUp != null && !C.JackLost;
		if (_goReady != flag2)
		{
			_goReady = flag2;
			if (flag2)
			{
				BtnGo.SetResourceReference(Control.BackgroundProperty, "GoBg");
				BtnGo.BorderBrush = Ui.Hex("#2fdc7a");
				BtnGo.Foreground = Brushes.White;
			}
			else
			{
				BtnGo.SetResourceReference(Control.BackgroundProperty, "Panel2");
				BtnGo.BorderBrush = Brushes.Transparent;
				BtnGo.SetResourceReference(Control.ForegroundProperty, "Muted");
			}
		}
		CutInfo lastCut = C.LastCut;
		BtnUndo.IsEnabled = lastCut != null && !C.JackLost && !lastCut.Item.Missing;
		if (C.JackLost != _lastJackLost)
		{
			_lastJackLost = C.JackLost;
			_needRender = true;
		}
	}

	private void UpdateTransport()
	{
		Player player = C.ActiveP();
		bool flag = !C.JackLost;
		bool flag2 = C.Players.Count > 0;
		BtnBack.IsEnabled = player != null;
		BtnFwd.IsEnabled = player != null;
		BtnPrev.IsEnabled = flag && ((player == null) ? (C.History.Count > 0) : (player.Pos - player.InP > 3.0 || C.History.Count >= 2));
		BtnNext.IsEnabled = flag && C.NextUp != null;
		BtnFade.IsEnabled = player != null || C.UserFades().Any();
		BtnStop.IsEnabled = C.Players.Count > 0;
		BtnPause.IsEnabled = flag && player != null;
		BtnDuck.IsEnabled = flag2 || C.DuckOn;
		BtnLoop.IsEnabled = player != null;
		BtnPanic.IsEnabled = flag2 || C.Fx.Any((FxSlot f) => f.Playing) || C.DuckOn;
	}

	private void UpdateRowProgress()
	{
		foreach (Player player in C.Players)
		{
			if (!player.Fading)
			{
				double num = player.OutP - player.InP;
				LibGrid.UpdateProgress(player.Item, (num > 0.0) ? ((player.Pos - player.InP) / num) : 0.0);
			}
		}
	}

	private void OnGo(object s, RoutedEventArgs e)
	{
		C.Go();
	}

	private void OnFade(object s, RoutedEventArgs e)
	{
		C.EscAction();
	}

	private void OnStop(object s, RoutedEventArgs e)
	{
		C.StopNow();
	}

	private void OnPause(object s, RoutedEventArgs e)
	{
		C.TogglePause();
	}

	private void OnDuck(object s, RoutedEventArgs e)
	{
		C.ToggleDuck();
	}

	private void OnLoop(object s, RoutedEventArgs e)
	{
		C.ToggleLoop();
	}

	private void OnUndo(object s, RoutedEventArgs e)
	{
		C.ResumeCut();
	}

	private void OnCancelFade(object s, RoutedEventArgs e)
	{
		C.CancelUserFade();
	}

	private void OnPrev(object s, RoutedEventArgs e)
	{
		C.PrevTrack();
	}

	private void OnBack(object s, RoutedEventArgs e)
	{
		C.SeekBy(-10.0);
	}

	private void OnFwd(object s, RoutedEventArgs e)
	{
		C.SeekBy(10.0);
	}

	private void OnNext(object s, RoutedEventArgs e)
	{
		C.Go();
	}

	private void OnPanicDown(object s, MouseButtonEventArgs e)
	{
		_panicStart = C.NowMs;
		_panicTimer?.Stop();
		_panicTimer = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(16L)
		};
		_panicTimer.Tick += (object? _, EventArgs _) =>
		{
			Point position = Mouse.GetPosition(BtnPanic);
			if (position.X < 0.0 || position.Y < 0.0 || position.X > BtnPanic.ActualWidth || position.Y > BtnPanic.ActualHeight)
			{
				StopPanicHold();
			}
			else
			{
				double num = (C.NowMs - _panicStart) / 500.0;
				PanicFill.Width = Math.Min(1.0, num) * BtnPanic.ActualWidth;
				if (num >= 1.0)
				{
					StopPanicHold();
					C.Panic();
				}
			}
		};
		_panicTimer.Start();
	}

	private void StopPanicHold()
	{
		_panicTimer?.Stop();
		_panicTimer = null;
		PanicFill.Width = 0.0;
	}

	private void OnPanicUp(object s, MouseButtonEventArgs e)
	{
		if (_panicTimer != null && C.NowMs - _panicStart < 500.0)
		{
			ShowToast("Giữ nút nửa giây mới dừng tất cả, để bấm lệch trong tối không tắt nhạc", ToastKind.Warn);
		}
		StopPanicHold();
	}

	private void WireWave()
	{
		Wave.MouseMove += (object _, MouseEventArgs e) =>
		{
			if (!(_scrub != null))
			{
				Player player = C.ActiveP();
				Wave.Ghost = ((player != null) ? new double?(WaveT(e, player)) : ((double?)null));
				Wave.Refresh();
			}
		};
		Wave.MouseLeave += (object _, MouseEventArgs _) =>
		{
			if (_scrub == null)
			{
				Wave.Ghost = null;
				Wave.Refresh();
			}
		};
		Wave.MouseLeftButtonDown += OnWaveDown;
		Wave.PreviewMouseMove += OnWaveDragMove;
		Wave.MouseLeftButtonUp += OnWaveUp;
		Wave.LostMouseCapture += (object _, MouseEventArgs _) =>
		{
			if (_scrub != null)
			{
				_scrub = null;
				Wave.Ghost = null;
				Wave.Refresh();
			}
		};
	}

	private double WaveT(MouseEventArgs e, Player p)
	{
		double x = e.GetPosition(Wave).X;
		double num = ((p.Item.Dur > 0.0) ? p.Item.Dur : p.OutP);
		return Math.Clamp(x / Math.Max(1.0, Wave.ActualWidth) * num, p.InP, p.OutP);
	}

	private void SeekMsg(Player p, double t)
	{
		C.SeekTo(p, t, keepUndo: true);
		ShowToast("Đã tua tới " + Text.Fmt(t) + ". Nhầm thì bấm nút ↶ để về chỗ cũ");
	}

	private void OnWaveDown(object sender, MouseButtonEventArgs e)
	{
		Player player = C.ActiveP();
		if (player != null)
		{
			if (e.ClickCount == 2)
			{
				_scrub = null;
				Wave.ReleaseMouseCapture();
				SeekMsg(player, WaveT(e, player));
			}
			else
			{
				_scrub = new Scrub(player, e.GetPosition(this).X);
				Wave.CaptureMouse();
			}
		}
	}

	private void OnWaveDragMove(object sender, MouseEventArgs e)
	{
		if (!(_scrub == null))
		{
			if (Math.Abs(e.GetPosition(this).X - _scrub.X0) > 4.0)
			{
				_scrub.Moved = true;
			}
			if (_scrub.Moved)
			{
				Wave.Ghost = WaveT(e, _scrub.P);
				Wave.Refresh();
			}
		}
	}

	private void OnWaveUp(object sender, MouseButtonEventArgs e)
	{
		Scrub scrub = _scrub;
		if (scrub == null)
		{
			return;
		}
		double y = e.GetPosition(Wave).Y;
		double t = WaveT(e, scrub.P);
		_scrub = null;
		Wave.ReleaseMouseCapture();
		Wave.Ghost = null;
		if (scrub.Moved)
		{
			if (y >= -30.0 && y <= Wave.ActualHeight + 30.0 && C.Players.Contains(scrub.P) && !scrub.P.Fading)
			{
				SeekMsg(scrub.P, t);
			}
			else
			{
				ShowToast("Đã hủy tua");
			}
		}
		else if (C.NowMs - _waveHintAt > 8000.0)
		{
			_waveHintAt = C.NowMs;
			ShowToast("Kéo trên dạng sóng hoặc bấm đúp để tua");
		}
		Wave.Refresh();
	}

	private async Task OpenInitialOutputAsync()
	{
		_wantDevice = _set.DeviceName;
		_outputBusy = true;
		(int, bool) tuple = await Task.Run(() =>
		{
			IReadOnlyList<(string, bool)> source = _engine.ListDevices();
			string name = _wantDevice;
			bool flag = name != null && !source.Any(((string Name, bool IsDefault) d) => d.Name == name);
			return (_engine.OpenOutput(flag ? null : name, _set.Exclusive), missing: flag);
		});
		int item = tuple.Item1;
		bool item2 = tuple.Item2;
		_outputBusy = false;
		if (item != 0)
		{
			C.OnOutputLost();
			AlertTitle.Text = "CHƯA CÓ OUTPUT ÂM THANH";
			AlertMsg.Text = "Máy chưa nhận thiết bị phát tiếng nào. Cắm dây hoặc card âm thanh vào, phần mềm tự kết nối lại.";
			AlertBar.Visibility = Visibility.Visible;
			PillOutError("Chưa có Output âm thanh");
			return;
		}
		if (item2)
		{
			ShowToast("Không thấy “" + _wantDevice + "”, đang dùng Output mặc định", ToastKind.Warn);
		}
		if ((_set.DeviceName == null) | item2)
		{
			_wantDevice = _engine.OutputName;
			if (_set.DeviceName == null)
			{
				_set.DeviceName = _wantDevice;
				_set.Save();
			}
		}
		OutputStatus status = _engine.Status;
		_lastRate = status.SampleRate;
		_lastDevEvents = status.DeviceEvents;
		UpdateOutPill();
		if (status.ExclusiveFailed != 0)
		{
			ShowToast("Không giữ riêng được cổng ra (app khác đang giữ, hoặc Windows không cho). Đang phát chung", ToastKind.Warn);
		}
		LoadAllFx();
	}

	private void CheckOutput()
	{
		if (_outputBusy)
		{
			return;
		}
		OutputStatus status = _engine.Status;
		if (!C.JackLost && status.State == OutputState.Lost)
		{
			OnOutputLost();
			return;
		}
		if (C.JackLost && !_tempOut && (status.DeviceEvents != _lastDevEvents || C.NowMs - _lastRetry > 2000.0))
		{
			_lastDevEvents = status.DeviceEvents;
			_lastRetry = C.NowMs;
			TryReopenAsync();
		}
		if (status.State == OutputState.Running && status.SampleRate != _lastRate && _lastRate != 0)
		{
			_lastRate = status.SampleRate;
			LoadAllFx();
		}
	}

	private async void OnOutputLost()
	{
		C.OnOutputLost();
		AlertTitle.Text = "MẤT OUTPUT: dây jack vừa bị rút";
		AlertMsg.Text = "Đã tạm dừng mọi bài để nhạc không phát ra loa laptop. Cắm lại dây xong, nhạc vẫn chờ: bấm P để phát tiếp, nhạc lên dần 2 giây.";
		AlertBar.Visibility = Visibility.Visible;
		PillOutError("Mất Output: dây jack bị rút");
		_outputBusy = true;
		await Task.Run(() =>
		{
			_engine.CloseOutput();
		});
		_outputBusy = false;
	}

	private async Task TryReopenAsync()
	{
		string name = _wantDevice;
		_outputBusy = true;
		int num = await Task.Run(() =>
		{
			IReadOnlyList<(string, bool)> readOnlyList = _engine.ListDevices();
			return (readOnlyList.Count == 0 || (name != null && !readOnlyList.Any(((string Name, bool IsDefault) d) => d.Name == name))) ? (-2) : _engine.OpenOutput(name, _set.Exclusive);
		});
		_outputBusy = false;
		if (num == 0)
		{
			_lastRate = _engine.Status.SampleRate;
			if (_wantDevice == null)
			{
				_wantDevice = _engine.OutputName;
			}
			C.OnOutputBack();
			AlertBar.Visibility = Visibility.Collapsed;
			UpdateOutPill();
			LoadAllFx();
			ShowToast("Đã có lại Output. Nhạc vẫn chờ: bấm P để phát tiếp (lên dần 2 giây)", ToastKind.Ok);
		}
	}

	private async void OnTempOut(object sender, RoutedEventArgs e)
	{
		if (!_outputBusy)
		{
			_outputBusy = true;
			int num = await Task.Run(() => _engine.OpenOutput(null, exclusive: false));
			_outputBusy = false;
			if (num != 0)
			{
				ShowToast("Không mở được loa của máy", ToastKind.Warn);
				return;
			}
			_tempOut = true;
			C.OnOutputBack();
			AlertBar.Visibility = Visibility.Collapsed;
			UpdateOutPill();
			ShowToast("Đã chuyển ra loa laptop. Bấm P để phát tiếp", ToastKind.Warn);
		}
	}

	private void PillOutError(string text)
	{
		PillOutText.Text = text;
		PillOut.SetResourceReference(Border.BackgroundProperty, "RedTint");
		PillOut.SetResourceReference(Border.BorderBrushProperty, "Red");
		PillOutDot.SetResourceReference(Shape.FillProperty, "Red");
		PillOutText.SetResourceReference(TextBlock.ForegroundProperty, "RedText");
	}

	private void UpdateOutPill()
	{
		OutputStatus status = _engine.Status;
		string outputName = _engine.OutputName;
		bool flag = false;
		string text;
		if (_tempOut)
		{
			text = "Đang phát TẠM ra " + outputName;
		}
		else if (status.Exclusive != 0)
		{
			text = "Output: " + outputName + " · chặn app khác";
			flag = true;
		}
		else if (status.ExclusiveFailed != 0)
		{
			text = "Không giữ riêng được · " + outputName;
		}
		else
		{
			text = "Output: " + outputName;
			flag = true;
		}
		PillOutText.Text = text;
		PillOut.SetResourceReference(Border.BackgroundProperty, flag ? "Panel2" : "AmberTint");
		PillOut.SetResourceReference(Border.BorderBrushProperty, flag ? "BtnLine" : "Amber");
		PillOutDot.SetResourceReference(Shape.FillProperty, flag ? "Green2" : "Amber");
		PillOutText.SetResourceReference(TextBlock.ForegroundProperty, flag ? "Ink" : "AmberText");
		if (flag)
		{
			PillOutDot.Effect = new DropShadowEffect
			{
				Color = Color.FromRgb(53, 208, 127),
				BlurRadius = 6.0,
				ShadowDepth = 0.0
			};
		}
		else
		{
			PillOutDot.Effect = null;
		}
	}

	private void OnQueueOptions(object s, RoutedEventArgs e)
	{
		ShowQueueOptions();
	}

	private void ShowQueueOptions()
	{
		TextBlock textBlock = Ui.T("Tùy chọn hàng chờ", 17.0, "Ink", FontWeights.Bold);
		textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 14.0);
		TextBlock removeHint = Ui.T("", 13.0, "Ink2", null, trim: false, wrap: true);
		TextBlock loopHint = Ui.T("", 13.0, "Ink2", null, trim: false, wrap: true);
		SegGroup remove = new SegGroup(("0", "Không"), ("1", "Có"));
		SegGroup loop = new SegGroup(("1", "Có"), ("0", "Không"));
		remove.Picked += (string v) =>
		{
			bool flag = v == "1";
			int count = C.Queue.Count;
			int num = C.QueueAtIndex + 1;
			C.SetRemovePlayed(flag);
			_set.QueueRemovePlayed = flag;
			_set.Save();
			Draw();
			if (flag && num > 0)
			{
				ShowToast(C.LoopQueue ? ("Đã đưa " + num + " bài đã phát xuống cuối hàng chờ") : ("Đã bỏ " + (count - C.Queue.Count) + " bài đã phát khỏi hàng chờ"));
			}
		};
		loop.Picked += (string v) =>
		{
			C.LoopQueue = v == "1";
			_set.QueueLoop = C.LoopQueue;
			_set.Save();
			Draw();
		};
		Draw();
		FrameworkElement frameworkElement = CloseBtn("Xong (Esc)", CloseModal);
		ShowModal(Ui.Col(textBlock, Option("Phát xong thì bỏ bài khỏi hàng chờ", remove, removeHint), Option("Lặp lại hàng chờ", loop, loopHint), frameworkElement), 480.0);
		void Draw()
		{
			remove.Value = (C.RemovePlayed ? "1" : "0");
			loop.Value = (C.LoopQueue ? "1" : "0");
			removeHint.Text = (C.RemovePlayed ? "Phát xong bài ra khỏi hàng chờ. Bài kế tiếp luôn là bài đầu hàng chờ." : "Bài đã phát vẫn nằm trong hàng chờ, mờ đi. Bài kế tiếp là bài ngay sau bài vừa phát.");
			TextBlock textBlock2 = loopHint;
			string text;
			if (C.LoopQueue)
			{
				text = (C.RemovePlayed ? "Bài vừa phát xuống cuối hàng chờ, nên hàng chờ phát vòng tròn tới khi bấm dừng." : "Phát hết bài cuối thì quay về bài đầu hàng chờ, phát vòng tròn tới khi bấm dừng.");
			}
			else
			{
				text = "Phát hết bài cuối hàng chờ thì dừng.";
			}
			textBlock2.Text = text;
			_needRender = true;
		}
		static FrameworkElement Option(string label, SegGroup seg, TextBlock hint)
		{
			TextBlock textBlock2 = Ui.T(label, 14.5, "Ink", FontWeights.SemiBold);
			textBlock2.Margin = new Thickness(0.0, 0.0, 0.0, 6.0);
			seg.Margin = new Thickness(0.0, 0.0, 0.0, 6.0);
			StackPanel stackPanel = Ui.Col(textBlock2, seg, hint);
			stackPanel.Margin = new Thickness(0.0, 0.0, 0.0, 16.0);
			return stackPanel;
		}
	}

	[DllImport("kernel32.dll")]
	private static extern uint SetThreadExecutionState(uint flags);

	public MainWindow(string[] args, string? startupFile)
	{
		_args = args;
		_startupFile = startupFile;
		InitializeComponent();
		_set = Settings.Load();
		ApplyTheme(_set.Theme);
		_engine = AudioEngine.Create();
		_store = LibraryStore.Open(AppPaths.Database);
		InitLicense();
		_lib = new MusicLibrary(_store, new DispatcherSynchronizationContext(Dispatcher));
		C = new PlayerController(_engine)
		{
			FadeLen = _set.FadeLen,
			Xfade = _set.Xfade,
			FxFade = ((_set.FxFade > 0.0) ? 1 : 0),
			AutoQueue = _set.AutoQueue,
			LoopQueue = _set.QueueLoop
		};
		C.SetRemovePlayed(_set.QueueRemovePlayed);
		C.SetAutoLoud(_set.AutoLoud);
		_engine.SetMasterDb(_set.MasterDb);
		C.Toast += (string m, ToastKind k) =>
		{
			ShowToast(m, k);
		};
		C.Changed += () =>
		{
			_needRender = true;
		};
		C.GoDenied += DenyGo;
		C.GoPressed += FlashNext;
		C.Finished += OnTrackFinished;
		_lib.Updated += () =>
		{
			_needRender = true;
		};
		_lib.AnalysisProgress += UpdateImpProgress;
		_src = new SourceWatcher(_set);
		_src.SourcesChanged += () =>
		{
			Dispatcher.BeginInvoke(new Action(UpdateBadge));
		};
		_src.WatchedFileReady += (string root, string path) =>
		{
			Dispatcher.BeginInvoke((Func<Task>)(async () =>
			{
				ImportResult importResult = await _lib.ImportAsync(new string[1] { path }, new MusicLibrary.ImportOptions
				{
					WatchRoot = root
				});
				if (importResult.Added.Count > 0)
				{
					ShowToast("Thư mục theo dõi có bài mới: " + importResult.Added[0].Name, ToastKind.Ok);
				}
			}));
		};
		BuildChrome();
		BuildMaster();
		BuildSettings();
		BuildItemEditor();
		BuildFxGrid();
		BuildStatusKeys();
		WireLibrary();
		WireWave();
		WireInput();
		InitCues();
		RestoreFx();
		RestoreSession();
		_timer = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(33L)
		};
		_timer.Tick += (object? _, EventArgs _) =>
		{
			OnTick();
		};
		Loaded += OnLoaded;
		Closing += OnClosingWindow;
		Closed += OnClosedWindow;
		StateChanged += (object? _, EventArgs _) =>
		{
			UpdateMaxState();
		};
		SizeChanged += (object _, SizeChangedEventArgs _) =>
		{
			UpdateNarrow();
		};
		_tips = new TipHost(this, Root, () => new FrameworkElement[5] { TimeRow, NowTitle, NextInfo, BtnGo, SetPop });
		SetThreadExecutionState(2147483651u);
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		Root.Focus();
		UpdateMaxState();
		RenderAll();
		_timer.Start();
		_src.Start();
		UpdateBadge();
		if (_lic.Status.Locked)
		{
			ShowLicense();
		}
		else
		{
			await StartAudioAsync();
		}
		if (_args.Contains("--cue"))
		{
			SetCueMode(true);
		}
		if (_args.Contains("--cuetemplate"))
		{
			OpenCueTemplateDialog();
		}
	}

	private async Task StartAudioAsync()
	{
		await OpenInitialOutputAsync();
		await _lib.CheckMissingAsync();
		await _lib.ConvertVideosAsync();
		UpdateImpProgress();
		if (_startupFile != null)
		{
			OpenExternalFile(_startupFile);
			_startupFile = null;
		}
	}

	private void OnTick()
	{
		_tickCount++;
		C.Tick();
		CheckOutput();
		UpdateNow();
		UpdateMeters();
		UpdateRowProgress();
		UpdateFx();
		UpdateGo();
		UpdateTransport();
		UpdateToast();
		UpdateCueClock();
		if (_needRender)
		{
			_needRender = false;
			RenderAll();
		}
		if (_editItem != null)
		{
			DrawItemWave();
		}
		if (_tickCount % 15 == 0)
		{
			ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
		}
		if (_tickCount % 60 == 0)
		{
			_lib.Flush();
			SaveSession();
		}
	}

	private void ApplyTheme(string theme)
	{
		_set.Theme = theme;
		Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
		{
			Source = new Uri((theme == "light") ? "Themes/Light.xaml" : "Themes/Dark.xaml", UriKind.Relative)
		};
		ThemeIcon.Kind = ((theme == "light") ? "moon" : "sun");
		Tip.SetText(BtnTheme, (theme == "light") ? "Đổi sang giao diện tối" : "Đổi sang giao diện sáng, hợp với rạp ngoài trời, ban ngày");
		_needRender = true;
	}

	private void OnThemeClick(object s, RoutedEventArgs e)
	{
		ApplyTheme((_set.Theme == "light") ? "dark" : "light");
		_set.Save();
		Wave.Refresh();
	}

	private void OnLoudClick(object s, RoutedEventArgs e)
	{
		bool flag = !C.AutoLoud;
		C.SetAutoLoud(flag);
		_set.AutoLoud = flag;
		_set.Save();
		UpdateLoudPill();
		ShowToast(flag ? "Đã bật tự cân độ to: các bài to nhỏ đều nhau" : "Đã tắt tự cân độ to: bài nào phát đúng độ to của file đó", flag ? ToastKind.Ok : ToastKind.Warn);
	}

	private void UpdateLoudPill()
	{
		LoudIcon.Kind = (C.AutoLoud ? "loud" : "loud-off");
		Tip.SetText(BtnLoud, C.AutoLoud ? "Cân độ to: BẬT. Mọi bài to đều nhau, không sửa file gốc" : "Cân độ to: TẮT. Bài nào phát đúng độ to của file đó");
		Ui.SetState(BtnLoud, C.AutoLoud ? "ok" : "warn");
	}

	private void SetFadeLabel(double seconds)
	{
		string text = seconds.ToString("0.#", CultureInfo.InvariantCulture).Replace('.', ',');
		FadeLabel.Text = text + " s";
		Tip.SetText(BtnFade, "Tắt dần trong " + text + " s. Bấm lần hai: tắt nhanh");
	}

	private void UpdateNarrow()
	{
		bool flag = ActualWidth < 1290.0;
		PillOutText.MaxWidth = (flag ? 110 : 360);
		if (flag != _narrow)
		{
			_narrow = flag;
			Resources["CtlW"] = (flag ? 46.0 : 52.0);
			Resources["CtlGap"] = new Thickness(flag ? 3 : 5, 0.0, 0.0, 0.0);
			NowColTitle.Width = new GridLength(flag ? 190 : 260);
			NowColWave.MinWidth = (flag ? 70 : 120);
		}
	}

	private void BuildChrome()
	{
		UpdateLoudPill();
		UpdateLicensePill();
		SetFadeLabel(C.FadeLen);
		AutoQueueBox.IsChecked = C.AutoQueue;
		ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
	}

	private void UpdateMaxState()
	{
		bool flag = WindowState == WindowState.Maximized;
		BtnMax.Content = (flag ? "\ue923" : "\ue922");
		Thickness windowResizeBorderThickness = SystemParameters.WindowResizeBorderThickness;
		Root.Margin = ((flag && !_full) ? new Thickness(windowResizeBorderThickness.Left + 3.0, windowResizeBorderThickness.Top + 3.0, windowResizeBorderThickness.Right + 3.0, windowResizeBorderThickness.Bottom + 3.0) : new Thickness(0.0));
	}

	private void OnMinimize(object s, RoutedEventArgs e)
	{
		WindowState = WindowState.Minimized;
	}

	private void OnMaximize(object s, RoutedEventArgs e)
	{
		WindowState = ((WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
	}

	private void OnCloseClick(object s, RoutedEventArgs e)
	{
		Close();
	}

	private void OnFullClick(object s, RoutedEventArgs e)
	{
		_full = !_full;
		if (_full)
		{
			_beforeFull = WindowState;
			WindowChrome.GetWindowChrome(this).ResizeBorderThickness = new Thickness(0.0);
			WindowStyle = WindowStyle.None;
			WindowState = WindowState.Normal;
			WindowState = WindowState.Maximized;
		}
		else
		{
			WindowChrome.GetWindowChrome(this).ResizeBorderThickness = new Thickness(5.0);
			WindowStyle = WindowStyle.SingleBorderWindow;
			WindowState = _beforeFull;
		}
		UpdateMaxState();
	}

	private void OnClosingWindow(object? sender, CancelEventArgs e)
	{
		if (!_confirmedExit && !_lockShown)
		{
			e.Cancel = true;
			string text = (C.Silent ? "Hàng chờ và kho nhạc đã được lưu, mở lại vẫn còn nguyên." : "Nhạc đang phát sẽ tắt ngay khi thoát.");
			Button button = Ui.Secondary("Ở lại", CloseModal);
			Button button2 = Ui.Primary("Thoát phần mềm", () =>
			{
				_confirmedExit = true;
				Close();
			});
			button2.SetResourceReference(Control.BackgroundProperty, "Red");
			button2.SetResourceReference(Control.BorderBrushProperty, "Red");
			StackPanel stackPanel = Ui.Row(8.0, button, button2);
			stackPanel.HorizontalAlignment = HorizontalAlignment.Right;
			stackPanel.Margin = new Thickness(0.0, 16.0, 0.0, 0.0);
			ShowModal(Ui.Col(Ui.T("Thoát CTECSoundPlayer?", 18.0, "Ink", FontWeights.Bold), WithMargin(Ui.T(text, 14.0, "Ink2", null, trim: false, wrap: true), 0.0, 8.0, 0.0, 0.0), stackPanel), 440.0);
		}
	}

	private static T WithMargin<T>(T el, double l, double t, double r, double b) where T : FrameworkElement
	{
		el.Margin = new Thickness(l, t, r, b);
		return el;
	}

	private void OnClosedWindow(object? sender, EventArgs e)
	{
		_timer.Stop();
		CommitPendingDelete();
		SaveSession(cleanExit: true);
		SaveCues();
		_set.Save();
		C.Shutdown();
		_src.Dispose();
		FxSlot[] fx = C.Fx;
		foreach (FxSlot fxSlot in fx)
		{
			if (fxSlot.Sample != 0)
			{
				_engine.FreeSample(fxSlot.Sample);
			}
		}
		_engine.Dispose();
		_lib.Dispose();
		_store.Dispose();
		SetThreadExecutionState(2147483648u);
	}

	private void SaveSession(bool cleanExit = false)
	{
		Player player = C.ActiveP();
		string text = JsonSerializer.Serialize(new SessionData(C.Queue.Select((GSoundPlayer.Model.Track t) => t.Id).ToArray(), C.Pick?.Id, cleanExit ? ((long?)null) : player?.Item.Id, player?.Pos ?? 0.0, C.LastCut?.Item.Id, C.LastCut?.Pos ?? 0.0, C.QueueAt?.Id));
		if (!(text == _lastSession))
		{
			_lastSession = text;
			_lib.SetKv("phien", text);
		}
	}

	private void RestoreSession()
	{
		string kv = _lib.GetKv("phien");
		if (kv == null)
		{
			return;
		}
		SessionData sessionData;
		try
		{
			sessionData = JsonSerializer.Deserialize<SessionData>(kv);
		}
		catch (JsonException)
		{
			return;
		}
		if (sessionData == null)
		{
			return;
		}
		long[] queue = sessionData.Queue;
		foreach (long id in queue)
		{
			GSoundPlayer.Model.Track track = _lib.FindById(id);
			if (track != null)
			{
				C.Queue.Add(track);
			}
		}
		long? queueAt = sessionData.QueueAt;
		if (queueAt.HasValue)
		{
			long valueOrDefault = queueAt.GetValueOrDefault();
			GSoundPlayer.Model.Track track2 = _lib.FindById(valueOrDefault);
			if (track2 != null)
			{
				C.SetQueueAt(track2);
			}
		}
		queueAt = sessionData.Pick;
		if (queueAt.HasValue)
		{
			long valueOrDefault2 = queueAt.GetValueOrDefault();
			GSoundPlayer.Model.Track track3 = _lib.FindById(valueOrDefault2);
			if (track3 != null)
			{
				C.SetPick(track3);
			}
		}
		queueAt = sessionData.CutId;
		if (queueAt.HasValue)
		{
			long valueOrDefault3 = queueAt.GetValueOrDefault();
			GSoundPlayer.Model.Track track4 = _lib.FindById(valueOrDefault3);
			if (track4 != null)
			{
				C.LastCut = new CutInfo(track4, sessionData.CutPos, null);
			}
		}
		queueAt = sessionData.CurId;
		if (!queueAt.HasValue)
		{
			return;
		}
		long valueOrDefault4 = queueAt.GetValueOrDefault();
		GSoundPlayer.Model.Track t2 = _lib.FindById(valueOrDefault4);
		if (t2 != null)
		{
			C.LastCut = new CutInfo(t2, sessionData.CurPos, null);
			Dispatcher.BeginInvoke((Action)(() =>
			{
				ShowToast("Lần trước phần mềm tắt đột ngột khi đang phát “" + t2.Name + "”. Bấm ⌫ để phát tiếp", ToastKind.Warn);
			}), DispatcherPriority.Loaded);
		}
	}

	public void ToastError(string msg)
	{
		ShowToast(msg, ToastKind.Warn);
	}

	private void ShowToast(string msg, ToastKind kind = ToastKind.Normal, string? actionText = null, Action? action = null, int? ms = null)
	{
		if (kind == ToastKind.Warn && action == null)
		{
			WarnPopText.Text = msg;
			WarnPop.Visibility = Visibility.Visible;
			_warnUntil = C.NowMs + (double)(ms ?? 4500);
			return;
		}
		ToastText.Text = msg;
		var (name, name2) = kind switch
		{
			ToastKind.Ok => ("ToastOkBg", "ToastOkInk"), 
			ToastKind.Warn => ("ToastWarnBg", "ToastWarnInk"), 
			_ => ("ToastBg", "Ink"), 
		};
		ToastBar.SetResourceReference(Border.BackgroundProperty, name);
		ToastText.SetResourceReference(TextBlock.ForegroundProperty, name2);
		ToastBar.Visibility = Visibility.Visible;
		_toastUntil = C.NowMs + (double)(ms ?? ((actionText != null) ? 6000 : 3600));
		if (actionText != null && action != null)
		{
			ToastAction.Content = actionText;
			ToastAction.Visibility = Visibility.Visible;
			_toastClick = () =>
			{
				action();
				ToastBar.Visibility = Visibility.Collapsed;
			};
		}
		else
		{
			ToastAction.Visibility = Visibility.Collapsed;
			_toastClick = null;
		}
	}

	private void UpdateToast()
	{
		if (ToastBar.Visibility == Visibility.Visible && C.NowMs >= _toastUntil)
		{
			ToastBar.Visibility = Visibility.Collapsed;
		}
		if (WarnPop.Visibility == Visibility.Visible && C.NowMs >= _warnUntil)
		{
			WarnPop.Visibility = Visibility.Collapsed;
		}
	}

	private void FlashNext()
	{
		ColorAnimationUsingKeyFrames colorAnimationUsingKeyFrames = new ColorAnimationUsingKeyFrames
		{
			Duration = TimeSpan.FromMilliseconds(800L)
		};
		Color color = ((SolidColorBrush)Ui.Res("Amber")).Color;
		Color color2 = ((SolidColorBrush)Ui.Res("Line")).Color;
		colorAnimationUsingKeyFrames.KeyFrames.Add(new LinearColorKeyFrame(color, KeyTime.FromPercent(0.25)));
		colorAnimationUsingKeyFrames.KeyFrames.Add(new LinearColorKeyFrame(color, KeyTime.FromPercent(0.6)));
		colorAnimationUsingKeyFrames.KeyFrames.Add(new LinearColorKeyFrame(color2, KeyTime.FromPercent(1.0)));
		SolidColorBrush solidColorBrush = new SolidColorBrush(color2);
		NextBar.BorderBrush = solidColorBrush;
		colorAnimationUsingKeyFrames.Completed += (object? _, EventArgs _) =>
		{
			NextBar.SetResourceReference(Border.BorderBrushProperty, "Line");
		};
		solidColorBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimationUsingKeyFrames);
	}

	private void DenyGo()
	{
		TranslateTransform translateTransform = new TranslateTransform();
		BtnGo.RenderTransform = translateTransform;
		DoubleAnimationUsingKeyFrames doubleAnimationUsingKeyFrames = new DoubleAnimationUsingKeyFrames
		{
			Duration = TimeSpan.FromMilliseconds(200L)
		};
		doubleAnimationUsingKeyFrames.KeyFrames.Add(new LinearDoubleKeyFrame(-3.0, KeyTime.FromPercent(0.25)));
		doubleAnimationUsingKeyFrames.KeyFrames.Add(new LinearDoubleKeyFrame(3.0, KeyTime.FromPercent(0.75)));
		doubleAnimationUsingKeyFrames.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(1.0)));
		translateTransform.BeginAnimation(TranslateTransform.XProperty, doubleAnimationUsingKeyFrames);
	}

	private void BuildStatusKeys()
	{
		(string, string)[] array = new (string, string)[8]
		{
			("Space", "GO"),
			("Esc", "tắt dần"),
			("⌫", "phát lại"),
			("1–9", "hiệu ứng"),
			("0", "tắt hiệu ứng"),
			("M", "MC nói"),
			("P", "tạm dừng"),
			("/", "tìm bài")
		};
		int num = 0;
		(string, string)[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			(string, string) tuple = array2[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			Border border = new Border
			{
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(4.0),
				Padding = new Thickness(5.0, 0.0, 5.0, 0.0),
				Child = Ui.T(item, 12.5)
			};
			border.SetResourceReference(Border.BorderBrushProperty, "Line2");
			StackPanel stackPanel = Ui.Row(4.0, border, Ui.T(item2, 13.0, "Ink2"));
			stackPanel.Margin = new Thickness((num != 0) ? 10 : 0, 0.0, 0.0, 0.0);
			StatusKeys.Children.Insert(num++, stackPanel);
		}
		ToastAction.Click += (object _, RoutedEventArgs _) =>
		{
			_toastClick?.Invoke();
		};
	}

}
