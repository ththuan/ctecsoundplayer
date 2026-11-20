using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GSoundPlayer.Model;

namespace GSoundPlayer.Controls;

public sealed class LibraryRow : Border
{
	private readonly Grid _g = new Grid();

	private readonly Button _star;

	private readonly Button _edit;

	private readonly Button _q;

	private readonly Button _play;

	private readonly TextBlock _name;

	private readonly TextBlock _flags;

	private readonly TextBlock _flags2;

	private readonly TextBlock _tagText;

	private readonly IconView _cut;

	private readonly IconView _nowIcon;

	private readonly TabularText _dur;

	private readonly Ellipse _dot;

	private readonly StackPanel _tag;

	private readonly StackPanel _fact;

	private readonly Button _ren;

	private readonly Rectangle _prog;

	private bool _pick;

	private bool _tile;

	private bool _playing;

	private bool _sel;

	private bool _fav;

	public Track? Track { get; private set; }

	public int Index { get; private set; }

	public event Action<LibraryRow, RowAction>? Action;

	public LibraryRow()
	{
		BorderThickness = new Thickness(1.0);
		CornerRadius = new CornerRadius(5.0);
		Background = Brushes.Transparent;
		Cursor = Cursors.Hand;
		_g.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(26.0)
		});
		_g.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		_g.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_g.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_g.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_g.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_g.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		_star = new Button
		{
			Content = Ui.Icon("star", 15.0),
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0)
		};
		_star.SetResourceReference(FrameworkElement.StyleProperty, "Btn");
		_star.ToolTip = "Bài hay dùng";
		_star.Click += (object _, RoutedEventArgs _) =>
		{
			Action?.Invoke(this, RowAction.Fav);
		};
		_star.MouseEnter += (object _, MouseEventArgs _) =>
		{
			if (!_fav)
			{
				_star.SetResourceReference(Control.ForegroundProperty, "AmberText");
			}
		};
		_star.MouseLeave += (object _, MouseEventArgs _) =>
		{
			_star.SetResourceReference(Control.ForegroundProperty, _fav ? "AmberText" : "StarOff");
		};
		Grid.SetColumn(_star, 0);
		_g.Children.Add(_star);
		_nowIcon = Ui.Icon("music", 15.0);
		_nowIcon.SetResourceReference(IconView.ForegroundProperty, "GreenText");
		_nowIcon.Visibility = Visibility.Collapsed;
		Grid.SetColumn(_nowIcon, 0);
		_g.Children.Add(_nowIcon);
		_name = Ui.T("", 14.5, "Ink", FontWeights.Medium, trim: true);
		Grid.SetColumn(_name, 1);
		_g.Children.Add(_name);
		_ren = new Button
		{
			Content = Ui.IconText("pencil", "đặt tên", 12.5, 3.0),
			FontSize = 12.5,
			Background = Brushes.Transparent,
			BorderThickness = new Thickness(0.0),
			Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
			Visibility = Visibility.Collapsed
		};
		_ren.SetResourceReference(FrameworkElement.StyleProperty, "Btn");
		_ren.SetResourceReference(Control.ForegroundProperty, "AmberText");
		_ren.Click += (object _, RoutedEventArgs _) =>
		{
			Action?.Invoke(this, RowAction.Rename);
		};
		Grid.SetColumn(_ren, 2);
		_g.Children.Add(_ren);
		_flags = Ui.T("", 12.5, "AmberText", FontWeights.Bold);
		_cut = Ui.Icon("scissors", 13.0);
		_cut.SetResourceReference(IconView.ForegroundProperty, "AmberText");
		_cut.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
		_cut.ToolTip = "Đã chỉnh đoạn phát, fade hoặc âm lượng";
		_flags2 = Ui.T("", 12.5, "AmberText", FontWeights.Bold);
		_flags2.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.Children.Add(_flags);
		stackPanel.Children.Add(_cut);
		stackPanel.Children.Add(_flags2);
		Grid.SetColumn(stackPanel, 3);
		_g.Children.Add(stackPanel);
		_dot = new Ellipse
		{
			Width = 7.0,
			Height = 7.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_tagText = Ui.T("", 12.5, "Ink2", null, trim: true);
		_tagText.Margin = new Thickness(4.0, 0.0, 0.0, 0.0);
		_tag = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Width = 80.0,
			Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		_tag.Children.Add(_dot);
		_tag.Children.Add(_tagText);
		Grid.SetColumn(_tag, 4);
		_g.Children.Add(_tag);
		_dur = Ui.Num("", 13.0, "Ink2");
		_dur.MinWidth = 44.0;
		_dur.Margin = new Thickness(5.0, 0.0, 0.0, 0.0);
		Grid.SetColumn(_dur, 5);
		_g.Children.Add(_dur);
		_edit = FactBtn("pencil", "Sửa đoạn phát, fade, âm lượng của bài này (Ctrl+E)", RowAction.Edit);
		_q = FactBtn("list-plus", "Xếp vào hàng chờ", RowAction.Queue);
		_play = new Button
		{
			Width = 30.0,
			Height = 23.0,
			Margin = new Thickness(1.0, 0.0, 1.0, 0.0)
		};
		_play.SetResourceReference(FrameworkElement.StyleProperty, "Btn");
		Ui.SetRadius(_play, new CornerRadius(4.0));
		_play.Click += (object _, RoutedEventArgs _) =>
		{
			Action?.Invoke(this, _playing ? RowAction.Stop : RowAction.Play);
		};
		SetPlayButton(playing: false);
		_fact = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Visibility = Visibility.Hidden,
			Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		_fact.Children.Add(_edit);
		_fact.Children.Add(_q);
		_fact.Children.Add(_play);
		Grid.SetColumn(_fact, 6);
		_g.Children.Add(_fact);
		_prog = new Rectangle
		{
			Height = 2.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Bottom,
			Visibility = Visibility.Collapsed,
			IsHitTestVisible = false
		};
		_prog.SetResourceReference(Shape.FillProperty, "Green2");
		Grid.SetColumnSpan(_prog, 7);
		_g.Children.Add(_prog);
		Child = _g;
		MouseEnter += (object _, MouseEventArgs _) =>
		{
			UpdateFact();
		};
		MouseLeave += (object _, MouseEventArgs _) =>
		{
			UpdateFact();
		};
	}

	private Button FactBtn(string icon, string tip, RowAction a)
	{
		Button button = new Button();
		button.Content = Ui.Icon(icon, 15.0);
		button.Width = 30.0;
		button.Height = 23.0;
		button.Margin = new Thickness(1.0, 0.0, 1.0, 0.0);
		button.ToolTip = tip;
		button.SetResourceReference(FrameworkElement.StyleProperty, "Btn");
		Ui.SetRadius(button, new CornerRadius(4.0));
		button.Click += (object _, RoutedEventArgs _) =>
		{
			Action?.Invoke(this, a);
		};
		return button;
	}

	private void SetPlayButton(bool playing)
	{
		_playing = playing;
		if (playing)
		{
			_play.Content = Ui.Icon("stop", 12.0);
			_play.ToolTip = "Dừng bài này ngay. Nhầm thì bấm ⌫ để phát lại từ chỗ vừa dừng";
			_play.SetResourceReference(Control.BackgroundProperty, "RedTint");
			_play.SetResourceReference(Control.BorderBrushProperty, "Red");
			_play.SetResourceReference(Control.ForegroundProperty, "RedText");
		}
		else
		{
			_play.Content = Ui.Icon("play", 13.0);
			_play.ToolTip = "Phát ngay";
			_play.SetResourceReference(Control.BackgroundProperty, "Green");
			_play.SetResourceReference(Control.BorderBrushProperty, "Green");
			_play.Foreground = Brushes.White;
		}
	}

	private void UpdateFact()
	{
		bool flag = IsMouseOver || _pick || _sel;
		_fact.Visibility = (_tile ? Visibility.Collapsed : ((!flag && !_playing) ? Visibility.Hidden : Visibility.Visible));
		Button edit = _edit;
		Visibility visibility = (_q.Visibility = ((!flag) ? Visibility.Hidden : Visibility.Visible));
		edit.Visibility = visibility;
	}

	public void Bind(Track t, int index, bool sel, bool pick, bool playing, int queuePos, bool tight, double fontSize, bool tile, Func<string, Group?>? groupOf, bool canPlay = true)
	{
		Track = t;
		Index = index;
		_pick = pick;
		_tile = tile;
		_sel = sel;
		_fav = t.Fav;
		_name.Text = Text.ShortName(t.Name);
		_nowIcon.Visibility = ((!(playing & tile)) ? Visibility.Collapsed : Visibility.Visible);
		SetPlayButton(playing);
		_g.ColumnDefinitions[0].Width = new GridLength((!tile) ? 26 : (playing ? 26 : 0));
		_name.MaxHeight = (tile ? 44.0 : double.PositiveInfinity);
		_dur.VerticalAlignment = ((!tile) ? VerticalAlignment.Center : VerticalAlignment.Bottom);
		_dur.Margin = (tile ? new Thickness(5.0, 0.0, 6.0, 6.0) : new Thickness(5.0, 0.0, 0.0, 0.0));
		_name.FontSize = (tile ? 16.0 : fontSize);
		_name.FontWeight = ((pick | playing | tile) ? FontWeights.Bold : FontWeights.Medium);
		_name.TextWrapping = ((!tile) ? TextWrapping.NoWrap : TextWrapping.Wrap);
		_name.SetResourceReference(TextBlock.ForegroundProperty, t.Missing ? "RedText" : "Ink");
		_star.SetResourceReference(Control.ForegroundProperty, t.Fav ? "AmberText" : "StarOff");
		_star.Visibility = (tile ? Visibility.Collapsed : Visibility.Visible);
		_flags.Text = ((queuePos >= 0) ? ("#" + (queuePos + 1)) : "");
		_q.IsEnabled = queuePos < 0;
		_q.ToolTip = ((queuePos >= 0) ? ("Bài đã ở hàng chờ, thứ " + (queuePos + 1)) : "Xếp vào hàng chờ");
		if (!playing)
		{
			_play.IsEnabled = canPlay && !t.Missing;
			if (t.Missing)
			{
				_play.ToolTip = "Thiếu file nhạc, chưa phát được";
			}
		}
		else
		{
			_play.IsEnabled = true;
		}
		_cut.Visibility = ((!t.Shaped) ? Visibility.Collapsed : Visibility.Visible);
		_flags2.Text = ((t.Analyzing ? "… " : "") + (t.Missing ? "THIẾU FILE" : "")).Trim();
		_flags2.Visibility = ((_flags2.Text.Length <= 0) ? Visibility.Collapsed : Visibility.Visible);
		_ren.Visibility = ((!t.Junk || tile) ? Visibility.Collapsed : Visibility.Visible);
		List<Group> list = t.Tags.Select((string k) => groupOf?.Invoke(k)).OfType<Group>().ToList();
		if (list.Count > 0)
		{
			_dot.Fill = Ui.Hex(list[0].Color);
			_dot.Visibility = Visibility.Visible;
			_tagText.Text = list[0].Name + ((list.Count > 1) ? (" +" + (list.Count - 1)) : "");
			_tag.ToolTip = "Nhóm: " + string.Join(", ", list.Select((Group g) => g.Name));
		}
		else
		{
			_dot.Visibility = Visibility.Collapsed;
			_tagText.Text = "";
			_tag.ToolTip = null;
		}
		_tag.Width = (tight ? 60 : 92);
		_tagText.MaxWidth = _tag.Width - 12.0;
		_tag.Visibility = (tile ? Visibility.Collapsed : Visibility.Visible);
		_dur.Text = ((t.Dur > 0.0) ? Text.Fmt(t.Len) : "");
		if (playing)
		{
			SetResourceReference(Border.BorderBrushProperty, "GreenRow");
			SetResourceReference(Border.BackgroundProperty, "GreenTint");
		}
		else if (pick)
		{
			SetResourceReference(Border.BorderBrushProperty, "Amber");
			SetResourceReference(Border.BackgroundProperty, "AmberTint");
		}
		else if (sel)
		{
			SetResourceReference(Border.BorderBrushProperty, "SelBorder");
			SetResourceReference(Border.BackgroundProperty, "SelBg");
		}
		else if (tile)
		{
			SetResourceReference(Border.BorderBrushProperty, "Line2");
			SetResourceReference(Border.BackgroundProperty, "Panel2");
		}
		else
		{
			BorderBrush = Brushes.Transparent;
			Background = Brushes.Transparent;
		}
		_name.Margin = (tile ? new Thickness(playing ? 2 : 12, 0.0, 4.0, 0.0) : new Thickness(0.0));
		_prog.Visibility = ((!playing) ? Visibility.Collapsed : Visibility.Visible);
		UpdateFact();
	}

	public void SetProgress(double f)
	{
		_prog.Width = Math.Max(0.0, ActualWidth - 8.0) * Math.Clamp(f, 0.0, 1.0);
	}

	protected override void OnMouseEnter(MouseEventArgs e)
	{
		base.OnMouseEnter(e);
		if (!_pick && Track != null && Background == Brushes.Transparent)
		{
			SetResourceReference(Border.BackgroundProperty, "HoverBg");
		}
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		base.OnMouseLeave(e);
		if (!_pick && Track != null && BorderBrush == Brushes.Transparent)
		{
			Background = Brushes.Transparent;
		}
	}
}
