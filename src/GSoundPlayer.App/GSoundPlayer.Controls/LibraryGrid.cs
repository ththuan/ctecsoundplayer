using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GSoundPlayer.Model;

namespace GSoundPlayer.Controls;

public sealed class LibraryGrid : Panel
{
	private readonly List<LibraryRow> _pool = new List<LibraryRow>();

	private readonly TextBlock _empty = Ui.T("", 13.5, "Ink2");

	public IReadOnlyList<Track> Items = Array.Empty<Track>();

	public int Selected;

	public Track? Pick;

	public bool TileMode;

	public bool CanPlay = true;

	public Func<Track, bool>? IsPlaying;

	public Func<Track, int>? QueuePos;

	public Func<string, Group?>? GroupOf;

	public string EmptyText = "Không thấy bài nào. Thử gõ ít chữ hơn, hoặc chọn nhóm “Tất cả”.";

	private Point? _down;

	private LibraryRow? _downRow;

	private double _rowH = 25.0;

	private int _shownFrom;

	private int _shownTo;

	public int Page { get; private set; }

	public int PageSize { get; private set; } = 1;

	public int Rows { get; private set; } = 1;

	public int Columns { get; private set; } = 2;

	public int PageCount => Math.Max(1, (int)Math.Ceiling((double)Items.Count / (double)PageSize));

	public event Action<Track, int>? RowClicked;

	public event Action<Track>? RowDoubleClicked;

	public event Action<Track, RowAction>? RowActionInvoked;

	public event Action<Track, FrameworkElement>? RowMenu;

	public event Action<Track, FrameworkElement>? RowDrag;

	public event Action? Paged;

	public LibraryGrid()
	{
		_empty.HorizontalAlignment = HorizontalAlignment.Center;
		_empty.Margin = new Thickness(14.0);
		Children.Add(_empty);
		Background = Brushes.Transparent;
	}

	public void Refresh(bool resetPage = false)
	{
		if (resetPage)
		{
			Page = 0;
		}
		InvalidateMeasure();
		InvalidateArrange();
	}

	public void GoPage(int delta)
	{
		int num = Math.Clamp(Page + delta, 0, PageCount - 1);
		if (num != Page)
		{
			Page = num;
			Refresh();
			Paged?.Invoke();
		}
	}

	public void EnsureVisible(int index)
	{
		if (PageSize > 0)
		{
			int num = index / PageSize;
			if (num != Page)
			{
				Page = num;
				Refresh();
				Paged?.Invoke();
			}
		}
	}

	private void Layout(double w, double h)
	{
		int count = Items.Count;
		if (TileMode)
		{
			Columns = 3;
			int num = Math.Max(1, (int)Math.Ceiling((double)count / 3.0));
			_rowH = Math.Min(69.0, Math.Max(44.0, Math.Floor((h - 4.0) / (double)num)));
			Rows = Math.Max(1, (int)Math.Floor((h - 4.0) / _rowH));
			PageSize = Rows * Columns;
		}
		else
		{
			Columns = ((w >= 1400.0) ? ((int)Math.Floor((w + 14.0) / 470.0)) : ((!(w >= 640.0)) ? 1 : 2));
			double num2 = h - 7.0;
			int num3 = (int)Math.Ceiling((double)Math.Max(1, count) / (double)Columns);
			_rowH = Math.Max(25.0, Math.Min(34.0, Math.Floor(num2 / (double)num3)));
			Rows = Math.Max(1, (int)Math.Floor(num2 / _rowH));
			PageSize = Rows * Columns;
		}
	}

	private LibraryRow GetRow(int i)
	{
		while (_pool.Count <= i)
		{
			LibraryRow libraryRow = new LibraryRow();
			libraryRow.MouseLeftButtonDown += OnRowMouseDown;
			libraryRow.MouseMove += OnRowMouseMove;
			libraryRow.MouseLeftButtonUp += (object _, MouseButtonEventArgs _) =>
			{
				_down = null;
			};
			libraryRow.MouseRightButtonUp += (object s, MouseButtonEventArgs e) =>
			{
				if (s is LibraryRow libraryRow2)
				{
					Track track = libraryRow2.Track;
					if (track != null)
					{
						e.Handled = true;
						RowMenu?.Invoke(track, libraryRow2);
					}
				}
			};
			libraryRow.Action += (LibraryRow row, RowAction a) =>
			{
				if (row.Track != null)
				{
					RowActionInvoked?.Invoke(row.Track, a);
				}
			};
			_pool.Add(libraryRow);
			Children.Add(libraryRow);
		}
		return _pool[i];
	}

	private void OnRowMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is LibraryRow libraryRow))
		{
			return;
		}
		Track track = libraryRow.Track;
		if (track != null)
		{
			if (e.ClickCount >= 2)
			{
				_down = null;
				RowDoubleClicked?.Invoke(track);
			}
			else
			{
				_down = e.GetPosition(this);
				_downRow = libraryRow;
				RowClicked?.Invoke(track, libraryRow.Index);
			}
		}
	}

	private void OnRowMouseMove(object sender, MouseEventArgs e)
	{
		Point? down = _down;
		if (!down.HasValue)
		{
			return;
		}
		Point valueOrDefault = down.GetValueOrDefault();
		if (e.LeftButton != MouseButtonState.Pressed || sender != _downRow)
		{
			return;
		}
		Track track = _downRow.Track;
		if (track != null)
		{
			Point position = e.GetPosition(this);
			if (!(Math.Abs(position.X - valueOrDefault.X) < 8.0) || !(Math.Abs(position.Y - valueOrDefault.Y) < 8.0))
			{
				_down = null;
				RowDrag?.Invoke(track, _downRow);
			}
		}
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		base.OnMouseWheel(e);
		e.Handled = true;
	}

	protected override Size MeasureOverride(Size available)
	{
		double num = (double.IsInfinity(available.Width) ? 800.0 : available.Width);
		double num2 = (double.IsInfinity(available.Height) ? 500.0 : available.Height);
		Layout(num, num2);
		if (Page >= PageCount)
		{
			Page = PageCount - 1;
		}
		_shownFrom = Page * PageSize;
		_shownTo = Math.Min(Items.Count, _shownFrom + PageSize);
		double num3 = (num - 16.0 - (double)((Columns - 1) * 14)) / (double)Columns;
		bool tight = num3 < 440.0;
		double fontSize = ((_rowH >= 30.0) ? 15.5 : 14.5);
		for (int i = _shownFrom; i < _shownTo; i++)
		{
			LibraryRow row = GetRow(i - _shownFrom);
			Track track = Items[i];
			row.Visibility = Visibility.Visible;
			row.Bind(track, i, i == Selected, track == Pick, IsPlaying?.Invoke(track) ?? false, QueuePos?.Invoke(track) ?? (-1), tight, fontSize, TileMode, GroupOf, CanPlay);
			row.Measure(new Size(num3, _rowH - 1.0));
		}
		for (int j = _shownTo - _shownFrom; j < _pool.Count; j++)
		{
			_pool[j].Visibility = Visibility.Collapsed;
		}
		_empty.Text = ((Items.Count == 0) ? EmptyText : "");
		_empty.Measure(new Size(num, num2));
		return new Size(num, num2);
	}

	protected override Size ArrangeOverride(Size size)
	{
		double num = (size.Width - 16.0 - (double)((Columns - 1) * 14)) / (double)Columns;
		for (int i = _shownFrom; i < _shownTo; i++)
		{
			int num2 = i - _shownFrom;
			int num3;
			int num4;
			if (TileMode)
			{
				num3 = num2 % Columns;
				num4 = num2 / Columns;
			}
			else
			{
				num3 = num2 / Rows;
				num4 = num2 % Rows;
			}
			int num5 = ((!TileMode) ? 1 : 4);
			_pool[num2].Arrange(new Rect(8.0 + (double)num3 * (num + 14.0), 2.0 + (double)num4 * _rowH, num, _rowH - (double)num5));
		}
		_empty.Arrange(new Rect(0.0, 0.0, size.Width, Math.Min(size.Height, 60.0)));
		return size;
	}

	public void UpdateProgress(Track t, double f)
	{
		foreach (LibraryRow item in _pool)
		{
			if (item.Visibility == Visibility.Visible && item.Track == t)
			{
				item.SetProgress(f);
			}
		}
	}
}
