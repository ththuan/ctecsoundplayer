using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace GSoundPlayer.Controls;

public sealed class TipHost
{
	private const double Gap = 6.0;

	private const double Edge = 4.0;

	private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(350L);

	private static readonly TimeSpan Warm = TimeSpan.FromMilliseconds(400L);

	private readonly Grid _root;

	private readonly Func<IEnumerable<FrameworkElement>> _keepClear;

	private readonly Border _box;

	private readonly TextBlock _text;

	private readonly Border _kbd;

	private readonly TextBlock _kbdText;

	private readonly DispatcherTimer _timer;

	private FrameworkElement? _for;

	private FrameworkElement? _mute;

	private DateTime _warmUntil;

	public Rect? ShownRect
	{
		get
		{
			if (_box.Visibility != Visibility.Visible)
			{
				return null;
			}
			return new Rect(Canvas.GetLeft(_box), Canvas.GetTop(_box), _box.DesiredSize.Width, _box.DesiredSize.Height);
		}
	}

	public TipHost(Window window, Grid root, Func<IEnumerable<FrameworkElement>> keepClear)
	{
		_root = root;
		_keepClear = keepClear;
		_text = new TextBlock
		{
			FontSize = 12.5,
			LineHeight = 16.0,
			TextWrapping = TextWrapping.Wrap,
			VerticalAlignment = VerticalAlignment.Center
		};
		_text.SetResourceReference(TextBlock.ForegroundProperty, "Ink");
		_kbdText = new TextBlock
		{
			FontSize = 11.5,
			FontWeight = FontWeights.Bold
		};
		_kbdText.SetResourceReference(TextBlock.ForegroundProperty, "Ink2");
		_kbd = new Border
		{
			Child = _kbdText,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(4.0),
			Padding = new Thickness(5.0, 0.0, 5.0, 0.0),
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		_kbd.SetResourceReference(Border.BorderBrushProperty, "Line2");
		DockPanel.SetDock(_kbd, Dock.Right);
		DockPanel dockPanel = new DockPanel
		{
			LastChildFill = true
		};
		dockPanel.Children.Add(_kbd);
		dockPanel.Children.Add(_text);
		_box = new Border
		{
			Child = dockPanel,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(9.0, 5.0, 9.0, 5.0),
			MaxWidth = 420.0,
			Visibility = Visibility.Collapsed,
			Effect = new DropShadowEffect
			{
				BlurRadius = 18.0,
				ShadowDepth = 6.0,
				Direction = 270.0,
				Opacity = 0.35
			}
		};
		_box.SetResourceReference(Border.BackgroundProperty, "MenuBg");
		_box.SetResourceReference(Border.BorderBrushProperty, "Line2");
		Canvas canvas = new Canvas
		{
			IsHitTestVisible = false
		};
		Grid.SetRowSpan(canvas, Math.Max(1, root.RowDefinitions.Count));
		Grid.SetColumnSpan(canvas, Math.Max(1, root.ColumnDefinitions.Count));
		Panel.SetZIndex(canvas, 1000);
		canvas.Children.Add(_box);
		root.Children.Add(canvas);
		_timer = new DispatcherTimer(DispatcherPriority.Normal)
		{
			Interval = Delay
		};
		_timer.Tick += (object? _, EventArgs _) =>
		{
			_timer.Stop();
			Show();
		};
		window.PreviewMouseMove += OnMove;
		window.PreviewMouseDown += (object _, MouseButtonEventArgs e) =>
		{
			Hide();
			_mute = Find(e.GetPosition(_root));
		};
		window.PreviewKeyDown += (object _, KeyEventArgs _) =>
		{
			Hide();
		};
		window.MouseLeave += (object _, MouseEventArgs _) =>
		{
			Hide();
		};
		window.Deactivated += (object? _, EventArgs _) =>
		{
			Hide();
		};
		Tip.Changed += (DependencyObject d) =>
		{
			if (d == _for && _box.Visibility == Visibility.Visible)
			{
				Show();
			}
		};
	}

	private void OnMove(object sender, MouseEventArgs e)
	{
		FrameworkElement frameworkElement = ((e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed) ? null : Find(e.GetPosition(_root)));
		if (_mute != null && frameworkElement == _mute)
		{
			return;
		}
		_mute = null;
		if (frameworkElement == _for)
		{
			return;
		}
		Hide();
		if (frameworkElement != null)
		{
			_for = frameworkElement;
			_for.IsVisibleChanged += OnTargetVisibleChanged;
			if (DateTime.UtcNow < _warmUntil)
			{
				Show();
			}
			else
			{
				_timer.Start();
			}
		}
	}

	private void OnTargetVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (!(bool)e.NewValue)
		{
			Hide();
		}
	}

	private FrameworkElement? Find(Point p)
	{
		FrameworkElement found = null;
		VisualTreeHelper.HitTest(_root, null, (HitTestResult r) =>
		{
			DependencyObject dependencyObject = r.VisualHit;
			while (dependencyObject != null && dependencyObject != _root)
			{
				if (dependencyObject is FrameworkElement frameworkElement && Tip.GetText(frameworkElement) != null)
				{
					found = frameworkElement;
					break;
				}
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}
			return HitTestResultBehavior.Stop;
		}, new PointHitTestParameters(p));
		return found;
	}

	public void Hide()
	{
		_timer.Stop();
		if (_box.Visibility == Visibility.Visible)
		{
			_box.Visibility = Visibility.Collapsed;
			_warmUntil = DateTime.UtcNow + Warm;
		}
		if (_for != null)
		{
			_for.IsVisibleChanged -= OnTargetVisibleChanged;
		}
		_for = null;
	}

	private void Show()
	{
		FrameworkElement frameworkElement = _for;
		double w;
		double h;
		double W;
		List<Rect> clear;
		if (frameworkElement != null && frameworkElement.IsVisible)
		{
			string text = Tip.GetText(frameworkElement);
			if (text != null)
			{
				_text.Text = text;
				string key = Tip.GetKey(frameworkElement);
				_kbdText.Text = key ?? "";
				_kbd.Visibility = ((key == null) ? Visibility.Collapsed : Visibility.Visible);
				_box.Visibility = Visibility.Visible;
				_box.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				w = _box.DesiredSize.Width;
				h = _box.DesiredSize.Height;
				W = _root.ActualWidth;
				double H = _root.ActualHeight;
				Rect rect = Bounds(frameworkElement);
				clear = (from frameworkElement2 in _keepClear()
					where frameworkElement2.IsVisible && frameworkElement2.ActualWidth > 0.0
					select frameworkElement2).Select(Bounds).ToList();
				double x = Fit(rect.Left + rect.Width / 2.0 - w / 2.0);
				List<double> list = new double[2]
				{
					rect.Top - h - 6.0,
					rect.Bottom + 6.0
				}.Where((double y) => y >= 4.0 && y + h <= H - 4.0).ToList();
				Point? point = null;
				foreach (double item in list)
				{
					if (!point.HasValue && !Hit(x, item).HasValue)
					{
						point = new Point(x, item);
					}
				}
				foreach (double item2 in list)
				{
					if (point.HasValue)
					{
						break;
					}
					Rect value = Hit(x, item2).Value;
					double[] array = new double[2]
					{
						Fit(value.Right + 6.0),
						Fit(value.Left - w - 6.0)
					};
					foreach (double x2 in array)
					{
						if (!point.HasValue && !Hit(x2, item2).HasValue)
						{
							point = new Point(x2, item2);
						}
					}
				}
				Point valueOrDefault = point.GetValueOrDefault();
				if (!point.HasValue)
				{
					valueOrDefault = new Point(x, rect.Bottom + 6.0);
					point = valueOrDefault;
				}
				Canvas.SetLeft(_box, Math.Round(point.Value.X));
				Canvas.SetTop(_box, Math.Round(point.Value.Y));
				return;
			}
		}
		Hide();
		double Fit(double val)
		{
			return Math.Max(4.0, Math.Min(W - w - 4.0, val));
		}
		Rect? Hit(double num2, double y)
		{
			foreach (Rect item3 in clear)
			{
				if (num2 < item3.Right && num2 + w > item3.Left && y < item3.Bottom && y + h > item3.Top)
				{
					return item3;
				}
			}
			return null;
		}
	}

	private Rect Bounds(FrameworkElement el)
	{
		return el.TransformToAncestor(_root).TransformBounds(new Rect(el.RenderSize));
	}

	public void ShowNow(FrameworkElement el)
	{
		Hide();
		_for = el;
		_for.IsVisibleChanged += OnTargetVisibleChanged;
		Show();
	}
}
