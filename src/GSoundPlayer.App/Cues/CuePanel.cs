using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GSoundPlayer.Controls;
using GSoundPlayer.Cues;
using GSoundPlayer.Library;
using GSoundPlayer.Model;
using GSoundPlayer.Playback;
using GSoundPlayer.EventCatalog;
using Microsoft.Win32;

namespace GSoundPlayer;

public partial class MainWindow
{
	private CueLibrary _cueLib = new CueLibrary();
	private CueShow _cueShow = new CueShow();
	private int _cueSel = -1;
	private int _cueActive = -1;
	private bool _cueRun;
	private DateTime _cueDeadline;
	private TimeSpan _cueRemain;
	private bool _cueReady;
	private bool _cueBusy;
	private int _cueClockTick;
	private List<CatalogTrack>? _cueCatalog;

	private void InitCues()
	{
		_cueReady = false;
		_cueLib = CueStore.Load();
		if (_cueLib.Shows.Count == 0)
		{
			_cueLib = CueLibrary.CreateDefault();
		}
		_cueShow = _cueLib.Shows.Find(s => s.Id == _cueLib.CurrentShowId) ?? _cueLib.Shows[0];
		_cueLib.CurrentShowId = _cueShow.Id;
		_cueRemain = TimeSpan.FromMinutes(Math.Clamp(_cueLib.MasterMinutes, 1, 240));
		CueMinutes.Text = _cueLib.MasterMinutes.ToString();
		FillCueShows();
		_cueSel = _cueShow.Cues.Count > 0 ? 0 : -1;
		_cueActive = -1;
		_cueReady = true;
		RefreshCuePanel();
		UpdateCueTimerLabel();
		PreviewKeyDown += OnCueKey;
	}

	private void SaveCues()
	{
		_cueLib.CurrentShowId = _cueShow.Id;
		CueStore.Save(_cueLib);
	}

	private void FillCueShows()
	{
		_cueBusy = true;
		CueShowBox.DisplayMemberPath = "Title";
		CueShowBox.ItemsSource = new List<CueShow>(_cueLib.Shows);
		CueShowBox.SelectedItem = _cueShow;
		Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate
		{
			_cueBusy = false;
		}));
	}

	private void SetCueMode(bool on)
	{
		CueDrawer.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
		LibPanel.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
		if (on)
		{
			RefreshCuePanel();
		}
	}

	private void OnCueClick(object s, RoutedEventArgs e)
	{
		SetCueMode(CueDrawer.Visibility != Visibility.Visible);
	}

	private void OnCueCloseClick(object s, RoutedEventArgs e)
	{
		SetCueMode(false);
	}

	private void OnCueBackClick(object s, RoutedEventArgs e)
	{
		SetCueMode(false);
	}

	private void OnCueEditClick(object s, RoutedEventArgs e)
	{
		if (_cueSel >= 0)
		{
			OpenCueEditor(_cueSel, delegate
			{
				SelectCue(_cueSel, scroll: false);
			});
		}
	}

	private void OnCuePlayClick(object s, RoutedEventArgs e)
	{
		PlayCue(_cueSel);
	}

	private void OnCueAssignClick(object s, RoutedEventArgs e)
	{
		AssignPickToCue(_cueSel);
	}

	private void OnCueBeatClick(object s, RoutedEventArgs e)
	{
		if (_cueSel >= 0)
		{
			_ = AskBeatForCueAsync(_cueSel);
		}
	}

	private void OnCueDeleteClick(object s, RoutedEventArgs e)
	{
		if (_cueSel >= 0)
		{
			AskDeleteCue(_cueSel);
		}
	}

	private void OnCueKey(object sender, KeyEventArgs e)
	{
		if (e.Handled)
		{
			return;
		}
		if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.K)
		{
			SetCueMode(CueDrawer.Visibility != Visibility.Visible);
			e.Handled = true;
			return;
		}
		if (CueDrawer.Visibility != Visibility.Visible || ModalOpen || MenuOpen)
		{
			return;
		}
		IInputElement focused = Keyboard.FocusedElement;
		if (focused is TextBox || focused is ComboBox || focused is Button || focused is CheckBox || focused is Slider)
		{
			return;
		}
		if (e.Key == Key.Enter)
		{
			PlayCue(_cueSel);
			e.Handled = true;
		}
		else if (e.Key == Key.Up)
		{
			SelectCue(Math.Max(0, _cueSel - 1), scroll: true);
			e.Handled = true;
		}
		else if (e.Key == Key.Down)
		{
			SelectCue(Math.Min(_cueShow.Cues.Count - 1, _cueSel + 1), scroll: true);
			e.Handled = true;
		}
	}

	private void OnCueShowDropped(object s, EventArgs e)
	{
		if (!_cueReady || _cueBusy || CueShowBox.SelectedItem is not CueShow show)
		{
			return;
		}
		_cueShow = show;
		_cueLib.CurrentShowId = show.Id;
		_cueSel = show.Cues.Count > 0 ? 0 : -1;
		_cueActive = -1;
		SaveCues();
		RefreshCuePanel();
	}

	private void OnCueNewShowClick(object s, RoutedEventArgs e)
	{
		OpenCueTemplateDialog();
	}

	private void OnCueMenuClick(object s, RoutedEventArgs e)
	{
		List<Mi> items = new List<Mi>
		{
			new Mi("Tạo từ mẫu sự kiện…", OpenCueTemplateDialog, "download"),
			new Mi("Tạo tất cả sự kiện mẫu", delegate { CreateAllTemplates(true); }, "download"),
			new Mi("Đổi tên kịch bản…", delegate
			{
				AskText("Đổi tên kịch bản", _cueShow.Title, delegate(string name)
				{
					string title = name.Trim();
					if (title.Length > 0)
					{
						_cueShow.Title = title;
						SaveCues();
						FillCueShows();
						RefreshCuePanel();
					}
				});
			}, "pencil"),
			new Mi("Nhập cue từ hàng chờ", delegate { ImportCuesFrom(C.Queue, "hàng chờ"); }, "list-plus"),
			new Mi("Nhập cue từ danh sách đang xem", delegate { ImportCuesFrom(_view, "danh sách đang xem"); }, "list-plus"),
			new Mi("Nhân bản kịch bản", DuplicateCueShow, "plus"),
			new Mi("Xóa kịch bản…", AskDeleteCueShow, "trash", Danger: true),
			new Mi("Mở thư mục dữ liệu", delegate
			{
				try
				{
					Directory.CreateDirectory(AppPaths.Root);
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppPaths.Root)
					{
						UseShellExecute = true
					});
				}
				catch
				{
				}
			}, "folder")
		};
		ShowMenu(BtnCueMenu, "KỊCH BẢN SỰ KIỆN", items);
	}

	private void DuplicateCueShow()
	{
		List<CueItem> cues = _cueShow.Cues.Select(delegate(CueItem c)
		{
			return new CueItem
			{
				Title = c.Title,
				TrackId = c.TrackId,
				TrackName = c.TrackName,
				Note = c.Note,
				Follow = c.Follow
			};
		}).ToList();
		CueShow show = new CueShow
		{
			Title = _cueShow.Title + " (bản sao)",
			Cues = cues
		};
		_cueLib.Shows.Add(show);
		_cueShow = show;
		_cueSel = cues.Count > 0 ? 0 : -1;
		_cueActive = -1;
		SaveCues();
		FillCueShows();
		RefreshCuePanel();
		ShowToast("Đã nhân bản kịch bản", ToastKind.Ok);
	}

	private void AskDeleteCueShow()
	{
		if (_cueLib.Shows.Count <= 1)
		{
			ShowToast("Phải còn ít nhất một kịch bản", ToastKind.Warn);
			return;
		}
		ConfirmDelete("Xóa kịch bản?", "Xóa “" + _cueShow.Title + "” cùng " + _cueShow.Cues.Count + " cue? Không ảnh hưởng kho nhạc.", "Xóa kịch bản", delegate
		{
			int index = _cueLib.Shows.IndexOf(_cueShow);
			_cueLib.Shows.Remove(_cueShow);
			_cueShow = _cueLib.Shows[Math.Clamp(index - 1, 0, _cueLib.Shows.Count - 1)];
			_cueSel = _cueShow.Cues.Count > 0 ? 0 : -1;
			_cueActive = -1;
			SaveCues();
			FillCueShows();
			RefreshCuePanel();
		});
	}

	private void ImportCuesFrom(IEnumerable<Track> tracks, string sourceLabel)
	{
		int added = 0;
		HashSet<long> seen = new HashSet<long>(_cueShow.Cues.Select(c => c.TrackId).Where(id => id != 0));
		foreach (Track track in tracks)
		{
			if (track == null)
			{
				continue;
			}
			_cueShow.Cues.Add(new CueItem
			{
				Title = track.Name,
				TrackId = track.Id,
				TrackName = track.Name,
				Follow = false
			});
			seen.Add(track.Id);
			added++;
		}
		if (added == 0)
		{
			ShowToast("Không có bài nào để nhập vào kịch bản", ToastKind.Warn);
			return;
		}
		_cueSel = _cueShow.Cues.Count - added;
		SaveCues();
		RefreshCuePanel();
		ShowToast("Đã thêm " + added + " cue từ " + sourceLabel, ToastKind.Ok);
	}

	private void OnCueAddClick(object s, RoutedEventArgs e)
	{
		OpenCueEditor(-1, null);
	}

	private void OnCuePrevClick(object s, RoutedEventArgs e)
	{
		SelectCue(Math.Max(0, _cueSel - 1), scroll: true);
	}

	private void OnCueNextClick(object s, RoutedEventArgs e)
	{
		SelectCue(Math.Min(_cueShow.Cues.Count - 1, _cueSel + 1), scroll: true);
	}

	private void OnCueGoClick(object s, RoutedEventArgs e)
	{
		PlayCue(_cueSel);
	}

	private void OnCueResetClick(object s, RoutedEventArgs e)
	{
		_cueActive = -1;
		SelectCue(_cueShow.Cues.Count > 0 ? 0 : -1, scroll: true);
		ShowToast("Về đầu kịch bản", ToastKind.Ok);
	}

	private void SelectCue(int index, bool scroll)
	{
		if (_cueShow.Cues.Count == 0)
		{
			_cueSel = -1;
			RefreshCuePanel();
			return;
		}
		_cueSel = Math.Clamp(index, 0, _cueShow.Cues.Count - 1);
		RefreshCuePanel();
		if (scroll && _cueSel >= 0 && _cueSel < CueRows.Children.Count)
		{
			(CueRows.Children[_cueSel] as FrameworkElement)?.BringIntoView();
		}
	}

	private void PlayCue(int index)
	{
		if (index < 0 || index >= _cueShow.Cues.Count)
		{
			ShowToast("Chưa có cue để chạy", ToastKind.Warn);
			return;
		}
		CueItem cue = _cueShow.Cues[index];
		Track track = CueTrack(cue);
		if (track == null)
		{
			ShowToast("Cue “" + cue.Title + "” chưa gán bài. Bấm nút bút chì để chọn bài hoặc thêm beat.", ToastKind.Warn);
			SelectCue(index, scroll: true);
			return;
		}
		C.PlayItem(track);
		_cueActive = index;
		_cueSel = Math.Min(index + 1, _cueShow.Cues.Count - 1);
		RefreshCuePanel();
		if (_cueSel >= 0 && _cueSel < CueRows.Children.Count)
		{
			(CueRows.Children[_cueSel] as FrameworkElement)?.BringIntoView();
		}
	}

	private Track? CueTrack(CueItem cue)
	{
		Track track = _lib.FindById(cue.TrackId);
		if (track == null && !string.IsNullOrWhiteSpace(cue.TrackName))
		{
			track = _lib.Tracks.FirstOrDefault(t => string.Equals(t.Name, cue.TrackName, StringComparison.OrdinalIgnoreCase));
		}
		return track;
	}

	private void OnTrackFinished(Track track)
	{
		if (_cueActive < 0 || _cueActive >= _cueShow.Cues.Count)
		{
			return;
		}
		CueItem cue = _cueShow.Cues[_cueActive];
		Track playing = CueTrack(cue);
		int finished = _cueActive;
		if (!cue.Follow || playing == null || !ReferenceEquals(playing, track))
		{
			_cueActive = -1;
			RefreshCuePanel();
			return;
		}
		_cueActive = -1;
		int next = finished + 1;
		if (next < _cueShow.Cues.Count)
		{
			Dispatcher.BeginInvoke(new Action(delegate
			{
				PlayCue(next);
			}));
		}
		else
		{
			ShowToast("Đã hết kịch bản", ToastKind.Ok);
			RefreshCuePanel();
		}
	}

	private void RefreshCuePanel()
	{
		if (!_cueReady || CueRows == null)
		{
			return;
		}
		CueRows.Children.Clear();
		int assigned = 0;
		if (_cueShow.Cues.Count == 0)
		{
			TextBlock empty = Ui.T("Kịch bản này chưa có cue.\nBấm “Thêm cue” hoặc “Tùy chọn → Tạo từ mẫu sự kiện…” để bắt đầu.", 13.0, "Ink2", null, trim: false, wrap: true);
			empty.Margin = new Thickness(16.0, 18.0, 16.0, 8.0);
			empty.TextAlignment = TextAlignment.Center;
			CueRows.Children.Add(empty);
		}
		for (int i = 0; i < _cueShow.Cues.Count; i++)
		{
			CueItem cue = _cueShow.Cues[i];
			if (CueTrack(cue) != null)
			{
				assigned++;
			}
			CueRows.Children.Add(MakeCueRow(i, cue));
		}
		CueCountText.Text = _cueShow.Cues.Count + " cue · " + assigned + " bài đã gán · " + _cueLib.Shows.Count + " kịch bản";
		UpdateCueNote();
		BtnCueGo.IsEnabled = _cueSel >= 0 && _cueSel < _cueShow.Cues.Count;
		BtnCuePrev.IsEnabled = _cueShow.Cues.Count > 1;
		BtnCueNext.IsEnabled = _cueShow.Cues.Count > 1;
		CueGoSmall.Text = _cueSel >= 0 ? "cue " + (_cueSel + 1) : "cue đang chọn";
	}

	private Border MakeCueRow(int index, CueItem cue)
	{
		bool selected = index == _cueSel;
		bool active = index == _cueActive;
		Track? track = CueTrack(cue);
		Border border = new Border
		{
			CornerRadius = new CornerRadius(7),
			BorderThickness = new Thickness(1),
			Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
			Margin = new Thickness(1.0, 2.0, 1.0, 2.0),
			Cursor = Cursors.Hand,
			Focusable = true,
			FocusVisualStyle = null,
			ToolTip = string.IsNullOrWhiteSpace(cue.Note) ? "Bấm để chọn · nhấp đôi để chạy · chuột phải để mở lựa chọn" : cue.Note
		};
		border.SetResourceReference(Border.BackgroundProperty, (selected || active) ? "GreenTint" : "Panel2");
		border.SetResourceReference(Border.BorderBrushProperty, selected ? "Green2" : (active ? "Green" : "Line"));
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		Border badge = new Border
		{
			Width = 24,
			Height = 24,
			CornerRadius = new CornerRadius(12),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = active ? "▶" : (index + 1).ToString(),
				FontSize = active ? 9.5 : 11.0,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			}
		};
		badge.SetResourceReference(Border.BackgroundProperty, active ? "Green" : "FxKeyBg");
		TextBlock badgeText = (TextBlock)badge.Child;
		badgeText.SetResourceReference(TextBlock.ForegroundProperty, active ? "Ink" : "Ink2");
		if (active)
		{
			badgeText.Foreground = Brushes.White;
		}
		grid.Children.Add(badge);
		StackPanel text = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(7.0, 0.0, 6.0, 0.0)
		};
		TextBlock title = new TextBlock
		{
			Text = cue.Title,
			FontSize = 13.5,
			FontWeight = FontWeights.SemiBold,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		title.SetResourceReference(TextBlock.ForegroundProperty, active ? "AmberText" : "Ink");
		text.Children.Add(title);
		TextBlock sub = new TextBlock
		{
			FontSize = 11.5,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		sub.SetResourceReference(TextBlock.ForegroundProperty, track == null ? "AmberText" : "Ink2");
		sub.Text = track == null ? "Chưa gán bài · chọn cue rồi bấm “Gán bài” hoặc “Thêm beat”" : (track.Missing ? "Thiếu file: " + track.FileName : track.Name);
		text.Children.Add(sub);
		if (!string.IsNullOrWhiteSpace(cue.Note))
		{
			TextBlock note = new TextBlock
			{
				Text = cue.Note.Length > 110 ? cue.Note.Substring(0, 110) + "…" : cue.Note,
				FontSize = 11.0,
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			note.SetResourceReference(TextBlock.ForegroundProperty, "Muted");
			text.Children.Add(note);
		}
		Grid.SetColumn(text, 1);
		grid.Children.Add(text);
		StackPanel right = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			VerticalAlignment = VerticalAlignment.Center
		};
		if (cue.Follow)
		{
			TextBlock follow = new TextBlock
			{
				Text = "TỰ CHẠY",
				FontSize = 9.5,
				FontWeight = FontWeights.ExtraBold,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 2.0, 0.0)
			};
			follow.SetResourceReference(TextBlock.ForegroundProperty, "AmberText");
			right.Children.Add(follow);
		}
		Grid.SetColumn(right, 2);
		grid.Children.Add(right);
		border.Child = grid;
		border.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs args)
		{
			border.Focus();
			if (args.ClickCount == 2)
			{
				PlayCue(index);
			}
			else
			{
				SelectCue(index, scroll: false);
			}
			args.Handled = true;
		};
		border.MouseRightButtonUp += delegate(object sender, MouseButtonEventArgs args)
		{
			args.Handled = true;
			CueRowMenu(border, index);
		};
		return border;
	}

	private void CueRowMenu(FrameworkElement anchor, int index)
	{
		List<Mi> items = new List<Mi>
		{
			new Mi("Chạy cue này", delegate { PlayCue(index); }, "play"),
			new Mi("Sửa cue…", delegate { OpenCueEditor(index, delegate { SelectCue(index, scroll: false); }); }, "pencil"),
			new Mi("Gán bài đang chọn trong kho", delegate { AssignPickToCue(index); }, "music"),
			new Mi("Thêm beat / file nhạc vào cue…", delegate { _ = AskBeatForCueAsync(index); }, "plus"),
			new Mi("Lên", delegate { MoveCue(index, -1); }, "chev-up"),
			new Mi("Xuống", delegate { MoveCue(index, 1); }, "chev-down"),
			new Mi("Xóa cue…", delegate { AskDeleteCue(index); }, "trash", Danger: true)
		};
		ShowMenu(anchor, "CUE " + (index + 1), items);
	}

	private void MoveCue(int index, int delta)
	{
		int target = index + delta;
		if (target < 0 || target >= _cueShow.Cues.Count)
		{
			return;
		}
		CueItem item = _cueShow.Cues[index];
		_cueShow.Cues.RemoveAt(index);
		_cueShow.Cues.Insert(target, item);
		_cueSel = target;
		if (_cueActive == index)
		{
			_cueActive = target;
		}
		else if (_cueActive == target)
		{
			_cueActive = index;
		}
		SaveCues();
		RefreshCuePanel();
		if (target < CueRows.Children.Count)
		{
			(CueRows.Children[target] as FrameworkElement)?.BringIntoView();
		}
	}

	private void AskDeleteCue(int index)
	{
		if (index < 0 || index >= _cueShow.Cues.Count)
		{
			return;
		}
		CueItem cue = _cueShow.Cues[index];
		ConfirmDelete("Xóa cue?", "Xóa “" + cue.Title + "” khỏi kịch bản?", "Xóa cue", delegate
		{
			_cueShow.Cues.RemoveAt(index);
			if (_cueActive == index)
			{
				_cueActive = -1;
			}
			_cueSel = Math.Clamp(index, 0, _cueShow.Cues.Count - 1);
			SaveCues();
			RefreshCuePanel();
		});
	}

	private void AssignPickToCue(int index)
	{
		Track? track = C.Pick;
		if (track == null && _sel >= 0 && _sel < _view.Count)
		{
			track = _view[_sel];
		}
		if (track == null)
		{
			ShowToast("Chọn một bài trong kho trước rồi gán vào cue", ToastKind.Warn);
			return;
		}
		AssignTrack(index, track);
	}

	private void AssignTrack(int index, Track track)
	{
		if (index < 0 || index >= _cueShow.Cues.Count)
		{
			return;
		}
		CueItem cue = _cueShow.Cues[index];
		cue.TrackId = track.Id;
		cue.TrackName = track.Name;
		if (string.IsNullOrWhiteSpace(cue.Title) || cue.Title.StartsWith("Cue ", StringComparison.Ordinal))
		{
			cue.Title = track.Name;
		}
		SaveCues();
		RefreshCuePanel();
		ShowToast("Đã gán “" + track.Name + "” vào cue " + (index + 1), ToastKind.Ok);
	}

	private async Task AskBeatForCueAsync(int index)
	{
		OpenFileDialog dialog = new OpenFileDialog
		{
			Title = "Chọn file beat / nhạc cho cue",
			Multiselect = false,
			Filter = "Nhạc và beat|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.ogg;*.wma|Tất cả|*.*"
		};
		if (dialog.ShowDialog(this) != true)
		{
			return;
		}
		string path = dialog.FileName;
		try
		{
			GSoundPlayer.Library.ImportResult result = await _lib.ImportAsync(new string[] { path });
			Track? track = result.Added.FirstOrDefault();
			if (track == null)
			{
				track = _lib.Tracks.FirstOrDefault(t => string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase));
			}
			if (track == null)
			{
				ShowToast("File này đã có trong kho nhưng chưa xác định được bài", ToastKind.Warn);
				return;
			}
			AssignTrack(index, track);
		}
		catch (Exception ex)
		{
			ShowToast("Không nạp được beat: " + ex.Message, ToastKind.Warn);
		}
	}

	private void OpenCueEditor(int index, Action? after)
	{
		bool editing = index >= 0 && index < _cueShow.Cues.Count;
		CueItem cue = editing ? _cueShow.Cues[index] : new CueItem();
		StackPanel root = new StackPanel();
		root.Children.Add(HeadRow(editing ? "SỬA CUE " + (index + 1) : "THÊM CUE", CloseBtn("Đóng (Esc)", CloseModal)));
		TextBox title = new TextBox
		{
			Height = 32,
			FontSize = 14,
			Text = cue.Title
		};
		root.Children.Add(Field("Tên cue / vai trò trong chương trình", title));
		(FrameworkElement searchBox, TextBox searchText) = FindBox("Tìm bài trong kho…");
		root.Children.Add(Field("Bài nhạc", searchBox));
		ListBox list = PickList(190);
		list.SelectionMode = SelectionMode.Single;
		list.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
		List<Track> source = _lib.Tracks.Where(t => !t.Missing).OrderBy(t => t.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), true)).ToList();
		void RefreshList()
		{
			string query = Text.Vn(searchText.Text.Trim());
			List<Track> tracks = source;
			if (query.Length > 0)
			{
				string[] words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				tracks = tracks.Where(delegate(Track t)
				{
					string hay = Text.Vn(t.Name + " " + t.FileName);
					return words.All(w => hay.Contains(w, StringComparison.Ordinal));
				}).ToList();
			}
			list.ItemsSource = tracks.Select(t => new PickItem
			{
				T = t
			}).ToList();
			Track? cur = CueTrack(cue);
			if (cur != null)
			{
				foreach (object item in list.Items)
				{
					if (item is PickItem pick && ReferenceEquals(pick.T, cur))
					{
						list.SelectedItem = item;
						if (list.SelectedItem != null)
						{
							list.ScrollIntoView(list.SelectedItem);
						}
						break;
					}
				}
			}
		}
		searchText.TextChanged += delegate
		{
			RefreshList();
		};
		RefreshList();
		root.Children.Add(list);
		Button pickNow = Ui.Secondary("Lấy bài đang phát / đang chọn trong kho", delegate
		{
			Track? track = C.Pick ?? (_sel >= 0 && _sel < _view.Count ? _view[_sel] : null);
			if (track != null)
			{
				foreach (object item in list.Items)
				{
					if (item is PickItem pick && ReferenceEquals(pick.T, track))
					{
						list.SelectedItem = item;
						list.ScrollIntoView(item);
						break;
					}
				}
			}
		});
		pickNow.HorizontalAlignment = HorizontalAlignment.Left;
		pickNow.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		root.Children.Add(pickNow);
		TextBox note = new TextBox
		{
			Height = 66,
			FontSize = 13.5,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			VerticalContentAlignment = VerticalAlignment.Top,
			Text = cue.Note
		};
		root.Children.Add(Field("Ghi chú MC", note));
		CheckBox follow = new CheckBox
		{
			Content = "Tự chạy cue tiếp khi bài này kết thúc",
			IsChecked = cue.Follow,
			Margin = new Thickness(0.0, 8.0, 0.0, 10.0)
		};
		root.Children.Add(follow);
		StackPanel actions = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		if (editing)
		{
			Button remove = Ui.Secondary("Xóa cue…", delegate
			{
				CloseModal();
				AskDeleteCue(index);
			});
			remove.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			remove.Foreground = Brushes.IndianRed;
			actions.Children.Add(remove);
		}
		Button cancel = Ui.Secondary("Hủy", CloseModal);
		cancel.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
		actions.Children.Add(cancel);
		Button save = Ui.Primary(editing ? "Lưu cue" : "Thêm cue", delegate
		{
			Track? track = (list.SelectedItem as PickItem)?.T;
			string cueTitle = title.Text.Trim();
			if (cueTitle.Length == 0)
			{
				cueTitle = track != null ? track.Name : "Cue " + (_cueShow.Cues.Count + 1);
			}
			CueItem saved = editing ? _cueShow.Cues[index] : new CueItem();
			saved.Title = cueTitle;
			saved.Note = note.Text.Trim();
			saved.Follow = follow.IsChecked == true;
			if (track != null)
			{
				saved.TrackId = track.Id;
				saved.TrackName = track.Name;
			}
			else
			{
				saved.TrackId = 0;
				saved.TrackName = "";
			}
			if (!editing)
			{
				_cueShow.Cues.Insert(index < 0 ? _cueShow.Cues.Count : Math.Min(index + 1, _cueShow.Cues.Count), saved);
			}
			_cueSel = editing ? index : _cueShow.Cues.IndexOf(saved);
			SaveCues();
			RefreshCuePanel();
			CloseModal();
			after?.Invoke();
		});
		actions.Children.Add(save);
		root.Children.Add(actions);
		ShowModal(root, 620.0);
	}

	private static FrameworkElement Field(string label, FrameworkElement control)
	{
		StackPanel panel = new StackPanel();
		TextBlock text = Ui.T(label, 12.5, "Ink2");
		text.Margin = new Thickness(0.0, 10.0, 0.0, 4.0);
		panel.Children.Add(text);
		panel.Children.Add(control);
		return panel;
	}

	private void UpdateCueNote()
	{
		bool hasCue = _cueSel >= 0 && _cueSel < _cueShow.Cues.Count;
		BtnCueEdit.IsEnabled = hasCue;
		BtnCueAssign.IsEnabled = hasCue;
		BtnCueBeat.IsEnabled = hasCue;
		BtnCueDelete.IsEnabled = hasCue;
		if (!hasCue)
		{
			CueDetailTitle.Text = "Chưa chọn cue";
			CueDetailTrack.Text = _cueShow.Cues.Count == 0
				? "Kịch bản này chưa có cue. Bấm “Thêm cue” hoặc tạo nhanh từ mẫu sự kiện."
				: "Chọn một cue trong danh sách bên trái để xem chi tiết.";
			CueDetailMeta.Text = "";
			CueDetailFollow.Text = "";
			CueNoteText.Text = "Chọn một cue để xem ghi chú MC.";
			BtnCuePlay.IsEnabled = false;
			return;
		}
		CueItem cue = _cueShow.Cues[_cueSel];
		Track? track = CueTrack(cue);
		CueDetailTitle.Text = cue.Title;
		CueDetailTrack.Text = track == null
			? "Chưa gán bài. Dùng “Gán bài” (lấy bài đang chọn trong kho) hoặc “Thêm beat” để nạp file mới."
			: (track.Missing ? "Thiếu file: " + track.FileName : track.Name);
		CueDetailMeta.Text = track == null
			? ""
			: "Thời lượng " + Text.Fmt(track.Len) + " · " + track.FileName;
		CueNoteText.Text = string.IsNullOrWhiteSpace(cue.Note) ? "Chưa có ghi chú cho cue này. Bấm “Sửa cue” để thêm." : cue.Note;
		CueDetailFollow.Text = cue.Follow ? "Tự chạy cue tiếp: BẬT" : "Tự chạy cue tiếp: TẮT";
		CueDetailFollow.SetResourceReference(TextBlock.ForegroundProperty, cue.Follow ? "GreenText" : "Muted");
		BtnCuePlay.IsEnabled = track != null && !track.Missing;
	}

	private int CueMinutesValue()
	{
		int minutes;
		if (!int.TryParse(CueMinutes.Text.Trim(), out minutes))
		{
			minutes = 5;
		}
		return Math.Clamp(minutes, 1, 240);
	}

	private void OnCueMinutesChanged(object s, TextChangedEventArgs e)
	{
		if (!_cueReady)
		{
			return;
		}
		int minutes = CueMinutesValue();
		_cueLib.MasterMinutes = minutes;
		if (!_cueRun)
		{
			_cueRemain = TimeSpan.FromMinutes(minutes);
			UpdateCueTimerLabel();
		}
	}

	private void OnCueTimerClick(object s, RoutedEventArgs e)
	{
		if (_cueRun)
		{
			_cueRun = false;
			TimeSpan left = _cueDeadline - DateTime.Now;
			_cueRemain = left > TimeSpan.Zero ? left : TimeSpan.Zero;
		}
		else
		{
			if (_cueRemain <= TimeSpan.Zero)
			{
				_cueRemain = TimeSpan.FromMinutes(CueMinutesValue());
			}
			_cueDeadline = DateTime.Now + _cueRemain;
			_cueRun = true;
		}
		UpdateCueTimerLabel();
	}

	private void OnCueTimerResetClick(object s, RoutedEventArgs e)
	{
		_cueRun = false;
		_cueRemain = TimeSpan.FromMinutes(CueMinutesValue());
		UpdateCueTimerLabel();
	}

	private void UpdateCueClock()
	{
		if (!_cueReady)
		{
			return;
		}
		_cueClockTick++;
		if (_cueRun)
		{
			TimeSpan left = _cueDeadline - DateTime.Now;
			if (left <= TimeSpan.Zero)
			{
				_cueRun = false;
				_cueRemain = TimeSpan.Zero;
				UpdateCueTimerLabel();
				ShowToast("Hết giờ kịch bản", ToastKind.Warn);
			}
			else
			{
				_cueRemain = left;
				if (_cueClockTick % 10 == 0)
				{
					UpdateCueTimerLabel();
				}
			}
		}
	}

	private void UpdateCueTimerLabel()
	{
		if (CueTimeText == null)
		{
			return;
		}
		int seconds = (int)Math.Ceiling(Math.Max(0.0, _cueRemain.TotalSeconds));
		CueTimeText.Text = $"{seconds / 60:00}:{seconds % 60:00}";
		string brush = _cueRun ? (seconds <= 60 ? "RedText" : "GreenText") : (seconds <= 60 && seconds > 0 ? "AmberText" : "Ink");
		CueTimeText.SetResourceReference(TextBlock.ForegroundProperty, brush);
	}

	private void OpenCueTemplateDialog()
	{
		SetCueMode(true);
		StackPanel root = new StackPanel();
		root.Children.Add(HeadRow("TẠO KỊCH BẢN SỰ KIỆN", CloseBtn("Đóng (Esc)", CloseModal)));
		TextBlock intro = Ui.T("Chọn mẫu theo loại sự kiện. CTECSoundPlayer tạo sẵn danh sách cue, gán bài có sẵn trong kho vào đúng vai trò, bật tự chạy lần lượt. Sau đó bạn có thể thêm beat, đổi bài, thêm cue và sửa ghi chú MC.", 13.5, "Ink2", null, trim: false, wrap: true);
		intro.Margin = new Thickness(0.0, 2.0, 0.0, 8.0);
		root.Children.Add(intro);
		StackPanel row = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		CheckBox assign = new CheckBox
		{
			Content = "Tự gán nhạc trong kho vào cue",
			IsChecked = true,
			VerticalAlignment = VerticalAlignment.Center
		};
		row.Children.Add(assign);
		Button createAll = Ui.Secondary("Tạo tất cả sự kiện mẫu", delegate
		{
			CreateAllTemplates(assign.IsChecked == true);
			CloseModal();
		});
		System.Windows.Automation.AutomationProperties.SetName(createAll, "CreateAllTemplates");
		createAll.Margin = new Thickness(6.0, 0.0, 0.0, 0.0);
		row.Children.Add(createAll);
		root.Children.Add(row);
		foreach (CueTemplate template in CueTemplates.All)
		{
			List<(CatalogTrack Catalog, Track Library)> candidates = TemplateTracks(template);
			Border card = new Border
			{
				CornerRadius = new CornerRadius(8),
				BorderThickness = new Thickness(1),
				Padding = new Thickness(12.0, 9.0, 10.0, 9.0),
				Margin = new Thickness(0.0, 0.0, 0.0, 7.0)
			};
			card.SetResourceReference(Border.BackgroundProperty, "InputBg");
			card.SetResourceReference(Border.BorderBrushProperty, "Line");
			DockPanel dock = new DockPanel
			{
				LastChildFill = true
			};
			CueTemplate captured = template;
			Button create = Ui.Primary("Tạo", delegate
			{
				CreateShowFromTemplate(captured, assign.IsChecked == true);
				CloseModal();
			});
			create.VerticalAlignment = VerticalAlignment.Center;
			create.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
			DockPanel.SetDock(create, Dock.Right);
			dock.Children.Add(create);
			StackPanel texts = new StackPanel();
			TextBlock heading = Ui.T(template.Title, 14.5, "Ink", FontWeights.Bold);
			texts.Children.Add(heading);
			TextBlock description = Ui.T(template.Description, 12.5, "Ink2", null, trim: false, wrap: true);
			description.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
			texts.Children.Add(description);
			string meta = template.CueTitles.Length > 0 ? template.CueTitles.Length + " cue" : (template.PerTrack ? "tạo cue theo bài đã tải" : "khung trống");
			if (candidates.Count > 0)
			{
				meta += " · " + candidates.Count + " bài sẵn sàng trong kho";
			}
			TextBlock metaText = Ui.T(meta, 11.5, candidates.Count > 0 ? "GreenText" : "Muted");
			metaText.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
			texts.Children.Add(metaText);
			dock.Children.Add(texts);
			card.Child = dock;
			root.Children.Add(card);
		}
		TextBlock tip = Ui.T("Mẹo: beat hoặc bài tự thêm có thể gán vào bất kỳ cue nào bằng nút bút chì hoặc “Thêm beat / file nhạc vào cue…”.", 12.5, "Muted", null, trim: false, wrap: true);
		tip.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		root.Children.Add(tip);
		ShowModal(root, 720.0);
	}

	private List<(CatalogTrack Catalog, Track Library)> TemplateTracks(CueTemplate template)
	{
		List<(CatalogTrack, Track)> result = new List<(CatalogTrack, Track)>();
		if (template.Categories.Length == 0 && template.Buses.Length == 0)
		{
			return result;
		}
		foreach (CatalogTrack catalog in LoadCueCatalog())
		{
			if (template.Categories.Length > 0 && !template.Categories.Contains(catalog.Category, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}
			if (template.Buses.Length > 0 && !template.Buses.Contains(catalog.Bus, StringComparer.OrdinalIgnoreCase))
			{
				continue;
			}
			Track? library = _lib.Tracks.FirstOrDefault(delegate(Track t)
			{
				return string.Equals(t.Name, catalog.Title, StringComparison.OrdinalIgnoreCase)
					|| t.FileName.StartsWith(catalog.Id + " -", StringComparison.OrdinalIgnoreCase);
			});
			if (library != null && !result.Any(x => ReferenceEquals(x.Item2, library)))
			{
				result.Add((catalog, library));
			}
		}
		return result;
	}

	private List<CatalogTrack> LoadCueCatalog()
	{
		if (_cueCatalog != null)
		{
			return _cueCatalog;
		}
		_cueCatalog = new List<CatalogTrack>();
		try
		{
			string path = Path.Combine(AppPaths.Root, "danh_muc_mau.json");
			if (File.Exists(path))
			{
				_cueCatalog = JsonSerializer.Deserialize<List<CatalogTrack>>(File.ReadAllText(path), new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				}) ?? new List<CatalogTrack>();
			}
		}
		catch
		{
		}
		return _cueCatalog;
	}

	private void CreateShowFromTemplate(CueTemplate template, bool autoAssign)
	{
		CueShow show = BuildShowFromTemplate(template, autoAssign, out int assigned);
		_cueLib.Shows.Add(show);
		_cueShow = show;
		_cueSel = show.Cues.Count > 0 ? 0 : -1;
		_cueActive = -1;
		SaveCues();
		FillCueShows();
		RefreshCuePanel();
		SetCueMode(true);
		string message = "Đã tạo “" + show.Title + "” · " + show.Cues.Count + " cue";
		if (assigned > 0)
		{
			message += " · gán sẵn " + assigned + " bài";
		}
		ShowToast(message, ToastKind.Ok);
	}

	private void CreateAllTemplates(bool autoAssign)
	{
		List<CueShow> created = new List<CueShow>();
		int assignedTotal = 0;
		foreach (CueTemplate template in CueTemplates.All)
		{
			if (template.Key == "empty" || template.Key == "beats")
			{
				continue;
			}
			if (_cueLib.Shows.Any(s => string.Equals(s.Title, template.Title, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}
			CueShow show = BuildShowFromTemplate(template, autoAssign, out int assigned);
			_cueLib.Shows.Add(show);
			created.Add(show);
			assignedTotal += assigned;
		}
		if (created.Count == 0)
		{
			ShowToast("Các kịch bản mẫu đã có sẵn trong danh sách kịch bản", ToastKind.Ok);
			return;
		}
		_cueShow = created[0];
		_cueSel = created[0].Cues.Count > 0 ? 0 : -1;
		_cueActive = -1;
		SaveCues();
		FillCueShows();
		RefreshCuePanel();
		SetCueMode(true);
		ShowToast("Đã tạo " + created.Count + " sự kiện mẫu · gán sẵn " + assignedTotal + " bài", ToastKind.Ok);
	}

	private CueShow BuildShowFromTemplate(CueTemplate template, bool autoAssign, out int assigned)
	{
		List<(CatalogTrack Catalog, Track Library)> candidates = autoAssign ? TemplateTracks(template) : new List<(CatalogTrack, Track)>();
		List<CueItem> cues = BuildTemplateCues(template, candidates);
		assigned = cues.Count(c => c.TrackId != 0);
		string title = template.Title;
		int suffix = 2;
		while (_cueLib.Shows.Any(s => string.Equals(s.Title, title, StringComparison.OrdinalIgnoreCase)))
		{
			title = template.Title + " " + suffix++;
		}
		return new CueShow
		{
			Title = title,
			Cues = cues
		};
	}

	private static readonly (string Key, string Name, string Color)[] MusicGroupDefs = new (string, string, string)[]
	{
		("ts_nghile", "CTEC · Nghi lễ", "#35d07f"),
		("ts_sankhau", "CTEC · Sân khấu", "#f2b43a"),
		("ts_tiec", "CTEC · Tiệc & Gala", "#9b7bff"),
		("ts_hieuung", "CTEC · Hiệu ứng", "#e5484d"),
		("free_energy", "Free · Năng lượng", "#35d07f"),
		("free_nhenhang", "Free · Nhẹ nhàng", "#4ea1ff"),
		("free_dienanh", "Free · Điện ảnh", "#f2b43a"),
		("free_tiec", "Free · Tiệc & Jazz", "#9b7bff"),
		("free_hieuung", "Free · Hiệu ứng", "#e5484d"),
		("beat", "Beat của tôi", "#f0782c")
	};

	private void GroupEventLibrary()
	{
		GroupEventLibrary(true);
	}

	private void GroupEventLibrary(bool showToast)
	{
		bool groupsChanged = false;
		foreach (var (key, name, color) in MusicGroupDefs)
		{
			if (!_groups.Any(g => g.Key == key))
			{
				_groups.Add(new Group(key, name, color));
				groupsChanged = true;
			}
		}
		if (groupsChanged)
		{
			SaveGroups();
		}
		int tagged = 0;
		foreach (CatalogTrack cat in LoadCueCatalog())
		{
			string group = CueCategoryGroup(cat.Category);
			if (group == null)
			{
				continue;
			}
			Track? track = _lib.Tracks.FirstOrDefault(t => string.Equals(t.Name, cat.Title, StringComparison.OrdinalIgnoreCase)
				|| t.FileName.StartsWith(cat.Id + " -", StringComparison.OrdinalIgnoreCase));
			if (track != null && !track.Tags.Contains(group))
			{
				track.Tags.Add(group);
				_lib.Save(track);
				tagged++;
			}
		}
		if (tagged > 0 || groupsChanged)
		{
			BuildChips();
			_needRender = true;
		}
		if (showToast)
		{
			ShowToast(tagged > 0 ? "Đã xếp " + tagged + " bài vào nhóm theo sự kiện" : "Kho nhạc đã được xếp nhóm", ToastKind.Ok);
		}
	}

	private static string? CueCategoryGroup(string category)
	{
		switch (category)
		{
			case "welcome":
			case "opening":
			case "award":
			case "launch":
			case "conference":
			case "closing":
				return "ts_nghile";
			case "sport":
			case "game":
				return "ts_sankhau";
			case "gala":
			case "waiting":
				return "ts_tiec";
			case "fx":
				return "ts_hieuung";
			default:
				return null;
		}
	}

	private void ShowMusicCredits()
	{
		StackPanel root = new StackPanel();
		root.Children.Add(HeadRow("NGUỒN NHẠC & GIẤY PHÉP", CloseBtn("Đóng (Esc)", CloseModal)));
		TextBlock intro = Ui.T("Kho nhạc gồm nhạc sự kiện và nhạc miễn phí có giấy phép rõ ràng. Khi dùng công khai, ghi nguồn theo đúng giấy phép của từng bài (thông tin nằm trong Ghi chú của bài).", 13.5, "Ink2", null, trim: false, wrap: true);
		intro.Margin = new Thickness(0.0, 2.0, 0.0, 12.0);
		root.Children.Add(intro);
		root.Children.Add(CreditCard("Kevin MacLeod · Incompetech", "Các bài nhạc miễn phí của Kevin MacLeod dùng giấy phép Creative Commons BY 4.0. Ghi nguồn: “Kevin MacLeod (incompetech.com), CC BY 4.0”.", "https://incompetech.com/music/royalty-free/"));
		root.Children.Add(CreditCard("Scott Buckley", "Các bài nhạc miễn phí của Scott Buckley dùng giấy phép Creative Commons BY 4.0. Ghi nguồn: “Scott Buckley (scottbuckley.com.au), CC BY 4.0”.", "https://www.scottbuckley.com.au/library/"));
		TextBlock note = Ui.T("Giấy phép CC BY 4.0 cho phép dùng miễn phí, kể cả sự kiện thương mại, miễn là ghi công tác giả và giữ thông tin giấy phép. Không bán lại các bản nhạc như sản phẩm riêng.", 12.5, "Muted", null, trim: false, wrap: true);
		note.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
		root.Children.Add(note);
		ShowModal(root, 640.0);
	}

	private FrameworkElement CreditCard(string title, string description, string url)
	{
		Border card = new Border
		{
			CornerRadius = new CornerRadius(8),
			BorderThickness = new Thickness(1),
			Padding = new Thickness(12.0, 10.0, 12.0, 10.0),
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		card.SetResourceReference(Border.BackgroundProperty, "InputBg");
		card.SetResourceReference(Border.BorderBrushProperty, "Line");
		DockPanel dock = new DockPanel();
		Button open = Ui.Secondary("Mở trang nguồn ↗", delegate
		{
			try
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
				{
					UseShellExecute = true
				});
			}
			catch
			{
			}
		});
		open.VerticalAlignment = VerticalAlignment.Center;
		open.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
		DockPanel.SetDock(open, Dock.Right);
		dock.Children.Add(open);
		StackPanel text = new StackPanel();
		text.Children.Add(Ui.T(title, 14.0, "Ink", FontWeights.Bold));
		TextBlock body = Ui.T(description, 12.5, "Ink2", null, trim: false, wrap: true);
		body.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
		text.Children.Add(body);
		dock.Children.Add(text);
		card.Child = dock;
		return card;
	}

	private static List<CueItem> BuildTemplateCues(CueTemplate template, List<(CatalogTrack Catalog, Track Library)> candidates)
	{
		List<CueItem> cues = new List<CueItem>();
		if (template.PerTrack)
		{
			if (candidates.Count > 0)
			{
				foreach (var pair in candidates.Take(template.Limit))
				{
					cues.Add(new CueItem
					{
						Title = pair.Library.Name,
						TrackId = pair.Library.Id,
						TrackName = pair.Library.Name,
						Follow = true
					});
				}
			}
			else
			{
				foreach (string title in template.CueTitles)
				{
					cues.Add(new CueItem
					{
						Title = title,
						Follow = true
					});
				}
			}
		}
		else
		{
			int candidateIndex = 0;
			foreach (string title in template.CueTitles)
			{
				CueItem cue = new CueItem
				{
					Title = title,
					Follow = true
				};
				if (candidateIndex < candidates.Count)
				{
					cue.TrackId = candidates[candidateIndex].Library.Id;
					cue.TrackName = candidates[candidateIndex].Library.Name;
					candidateIndex++;
				}
				cues.Add(cue);
			}
		}
		return cues;
	}
}
