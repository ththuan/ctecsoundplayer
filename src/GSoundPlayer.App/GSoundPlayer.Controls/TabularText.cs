using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GSoundPlayer.Controls;

public sealed class TabularText : FrameworkElement
{
	public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(TabularText));

	public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(TabularText));

	public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(TabularText));

	public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(TabularText));

	private string _text = "";

	private static readonly Dictionary<(FontFamily, double, FontWeight, double, TextFormattingMode), double> Cells = new Dictionary<(FontFamily, double, FontWeight, double, TextFormattingMode), double>();

	public Brush Foreground
	{
		get
		{
			return (Brush)GetValue(ForegroundProperty);
		}
		set
		{
			SetValue(ForegroundProperty, value);
		}
	}

	public FontFamily FontFamily
	{
		get
		{
			return (FontFamily)GetValue(FontFamilyProperty);
		}
		set
		{
			SetValue(FontFamilyProperty, value);
		}
	}

	public double FontSize
	{
		get
		{
			return (double)GetValue(FontSizeProperty);
		}
		set
		{
			SetValue(FontSizeProperty, value);
		}
	}

	public FontWeight FontWeight
	{
		get
		{
			return (FontWeight)GetValue(FontWeightProperty);
		}
		set
		{
			SetValue(FontWeightProperty, value);
		}
	}

	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value == null)
			{
				value = "";
			}
			if (!(value == _text))
			{
				bool num = value.Length == _text.Length && !value.Where((char c, int i) => c != _text[i] && (!char.IsDigit(c) || !char.IsDigit(_text[i]))).Any();
				_text = value;
				if (!num)
				{
					InvalidateMeasure();
				}
				InvalidateVisual();
			}
		}
	}

	private FormattedText Ft(string s)
	{
		return new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(FontFamily, FontStyles.Normal, FontWeight, FontStretches.Normal), FontSize, Foreground, null, TextOptions.GetTextFormattingMode(this), VisualTreeHelper.GetDpi(this).PixelsPerDip);
	}

	private double Cell()
	{
		(FontFamily, double, FontWeight, double, TextFormattingMode) key = (FontFamily, FontSize, FontWeight, VisualTreeHelper.GetDpi(this).PixelsPerDip, TextOptions.GetTextFormattingMode(this));
		if (!Cells.TryGetValue(key, out var value))
		{
			value = Math.Ceiling(Enumerable.Range(0, 10).Max((int d) => Ft(d.ToString()).WidthIncludingTrailingWhitespace));
			Cells[key] = value;
		}
		return value;
	}

	private double TotalWidth(double cell)
	{
		return _text.Sum((char c) => (!char.IsDigit(c)) ? Ft(c.ToString()).WidthIncludingTrailingWhitespace : cell);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(TotalWidth(Cell()), Ft("0").Height);
	}

	protected override void OnRender(DrawingContext g)
	{
		double num = Cell();
		double num2 = Math.Max(0.0, ActualWidth - TotalWidth(num));
		string text = _text;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			FormattedText formattedText = Ft(c.ToString());
			double widthIncludingTrailingWhitespace = formattedText.WidthIncludingTrailingWhitespace;
			if (char.IsDigit(c))
			{
				g.DrawText(formattedText, new Point(Math.Round(num2 + (num - widthIncludingTrailingWhitespace) / 2.0), 0.0));
				num2 += num;
			}
			else
			{
				g.DrawText(formattedText, new Point(Math.Round(num2), 0.0));
				num2 += widthIncludingTrailingWhitespace;
			}
		}
	}
}
