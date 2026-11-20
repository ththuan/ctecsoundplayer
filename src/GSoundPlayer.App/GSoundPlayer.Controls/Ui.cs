using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GSoundPlayer.Controls;

public static class Ui
{
	public static readonly DependencyProperty RadiusProperty = DependencyProperty.RegisterAttached("Radius", typeof(CornerRadius), typeof(Ui), new FrameworkPropertyMetadata(new CornerRadius(0.0)));

	public static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached("State", typeof(string), typeof(Ui), new FrameworkPropertyMetadata(""));

	public static readonly FontFamily AppFont = new FontFamily("Segoe UI, Segoe UI Symbol, Segoe UI Emoji");

	public static CornerRadius GetRadius(DependencyObject o)
	{
		return (CornerRadius)o.GetValue(RadiusProperty);
	}

	public static void SetRadius(DependencyObject o, CornerRadius v)
	{
		o.SetValue(RadiusProperty, v);
	}

	public static string GetState(DependencyObject o)
	{
		return (string)o.GetValue(StateProperty);
	}

	public static void SetState(DependencyObject o, string v)
	{
		o.SetValue(StateProperty, v);
	}

	public static Brush Res(string key)
	{
		return (Brush)Application.Current.FindResource(key);
	}

	public static TabularText Num(string text, double size, string brush)
	{
		TabularText tabularText = new TabularText();
		tabularText.Text = text;
		tabularText.FontSize = size;
		tabularText.VerticalAlignment = VerticalAlignment.Center;
		tabularText.SetResourceReference(TabularText.ForegroundProperty, brush);
		return tabularText;
	}

	public static Brush Hex(string hex)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
		solidColorBrush.Freeze();
		return solidColorBrush;
	}

	public static TextBlock T(string text, double size = 14.0, string brush = "Ink", FontWeight? weight = null, bool trim = false, bool wrap = false)
	{
		TextBlock textBlock = new TextBlock
		{
			Text = text,
			FontSize = size,
			VerticalAlignment = VerticalAlignment.Center
		};
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, brush);
		if (weight.HasValue)
		{
			textBlock.FontWeight = weight.Value;
		}
		if (trim)
		{
			textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		}
		if (wrap)
		{
			textBlock.TextWrapping = TextWrapping.Wrap;
		}
		return textBlock;
	}

	public static Button Btn(string style, object content, Action? click = null, string? tip = null)
	{
		Button button = new Button
		{
			Content = content
		};
		button.SetResourceReference(FrameworkElement.StyleProperty, style);
		if (tip != null)
		{
			button.ToolTip = tip;
		}
		if (click != null)
		{
			button.Click += (object _, RoutedEventArgs _) =>
			{
				click();
			};
		}
		return button;
	}

	public static Button Primary(string text, Action? click = null)
	{
		return Btn("Primary", text, click);
	}

	public static Button Secondary(string text, Action? click = null)
	{
		return Btn("Secondary", text, click);
	}

	public static IconView Icon(string kind, double size = 16.0)
	{
		return new IconView
		{
			Kind = kind,
			IconSize = size
		};
	}

	public static StackPanel IconText(string kind, string text, double size = 16.0, double gap = 6.0)
	{
		TextBlock element = new TextBlock
		{
			Text = text,
			VerticalAlignment = VerticalAlignment.Center
		};
		IconView iconView = Icon(kind, size);
		iconView.Margin = new Thickness(0.0, 0.0, gap, 0.0);
		return new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children = 
			{
				(UIElement)iconView,
				(UIElement)element
			}
		};
	}

	public static Button IconBtn(string style, string kind, Action? click = null, string? tip = null, double size = 16.0)
	{
		return Btn(style, Icon(kind, size), click, tip);
	}

	public static StackPanel Row(double gap, params UIElement[] kids)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		for (int i = 0; i < kids.Length; i++)
		{
			if (i > 0 && kids[i] is FrameworkElement frameworkElement)
			{
				frameworkElement.Margin = new Thickness(gap, frameworkElement.Margin.Top, frameworkElement.Margin.Right, frameworkElement.Margin.Bottom);
			}
			stackPanel.Children.Add(kids[i]);
		}
		return stackPanel;
	}

	public static StackPanel Col(params UIElement[] kids)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Vertical
		};
		foreach (UIElement element in kids)
		{
			stackPanel.Children.Add(element);
		}
		return stackPanel;
	}

	public static TextBlock H3(string text)
	{
		TextBlock textBlock = T(text, 13.0, "AmberText", FontWeights.Bold);
		textBlock.Margin = new Thickness(0.0, 14.0, 0.0, 6.0);
		return textBlock;
	}

	public static Border Card(UIElement child, Thickness? padding = null, string border = "Line", string? bg = null)
	{
		Border border2 = new Border
		{
			Child = child,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Padding = (padding ?? new Thickness(7.0, 10.0, 7.0, 10.0))
		};
		border2.SetResourceReference(Border.BorderBrushProperty, border);
		if (bg != null)
		{
			border2.SetResourceReference(Border.BackgroundProperty, bg);
		}
		return border2;
	}
}
