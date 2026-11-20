using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using GSoundPlayer.Model;

namespace GSoundPlayer.Controls;

public sealed class WaveformView : FrameworkElement
{
	public float[]? Wave;

	public double Dur = 1.0;

	public double InP;

	public double OutP = 1.0;

	public double FadeIn;

	public double FadeOut;

	public double? Pos;

	public double? Ghost;

	public bool Dim;

	private (int Px, int Ghost, bool Dim, double InP, double OutP, double Fi, double Fo, float[]? W, double Dur, Brush Bg) _last;

	private static readonly Typeface Face = new Typeface(Ui.AppFont, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

	public bool Handles { get; set; }

	public void Refresh()
	{
		double actualWidth = ActualWidth;
		double num = ((Dur > 0.0) ? Dur : 1.0);
		(int, int, bool, double, double, double, double, float[], double, Brush) last = (Pos.HasValue ? ((int)(Pos.Value / num * actualWidth)) : (-1), Ghost.HasValue ? ((int)(Ghost.Value / num * actualWidth)) : (-1), Dim, InP, OutP, FadeIn, FadeOut, Wave, Dur, Ui.Res("WaveBg"));
		if (!last.Equals(_last))
		{
			_last = last;
			InvalidateVisual();
		}
	}

	private static double AmpAt(float[]? w, double dur, double t)
	{
		if (w == null || w.Length == 0 || dur <= 0.0)
		{
			return 0.0;
		}
		int num = Math.Clamp((int)(t / dur * (double)w.Length), 0, w.Length - 1);
		return w[num];
	}

	protected override void OnRender(DrawingContext g)
	{
		double w = ActualWidth;
		double actualHeight = ActualHeight;
		if (w <= 0.0 || actualHeight <= 0.0)
		{
			return;
		}
		double dur = ((Dur > 0.0) ? Dur : 1.0);
		Brush brush = Ui.Res("WaveBg");
		Brush brush2 = Ui.Res("WaveOut");
		Brush brush3 = Ui.Res("WavePlayed");
		Brush brush4 = Ui.Res("WaveDim");
		Brush brush5 = Ui.Res("WaveRest");
		g.DrawRoundedRectangle(brush, null, new Rect(0.0, 0.0, w, actualHeight), 6.0, 6.0);
		double num = actualHeight / 2.0;
		int num2 = (int)(w / 3.0);
		for (int i = 0; i < num2; i++)
		{
			double num3 = ((double)i + 0.5) / (double)num2 * dur;
			double num4 = AmpAt(Wave, dur, num3);
			bool flag = num3 >= InP && num3 <= OutP;
			bool flag2 = (Pos.HasValue && num3 <= Pos) & flag;
			Brush brush6;
			if (flag)
			{
				if (flag2)
				{
					brush6 = (Dim ? brush4 : brush3);
				}
				else
				{
					brush6 = brush5;
				}
			}
			else
			{
				brush6 = brush2;
			}
			double num5 = Math.Max(1.0, num4 * (actualHeight * 0.84) / 2.0);
			g.DrawRectangle(brush6, null, new Rect(i * 3, num - num5, 2.0, num5 * 2.0));
		}
		Brush brush7 = Ui.Res("WaveFade");
		if (FadeIn > 0.0 || FadeOut > 0.0)
		{
			Pen pen = new Pen(brush7, 1.5);
			pen.Freeze();
			if (FadeIn > 0.0)
			{
				g.DrawLine(pen, new Point(X(InP), actualHeight - 3.0), new Point(X(InP + FadeIn), 3.0));
			}
			if (FadeOut > 0.0)
			{
				g.DrawLine(pen, new Point(X(OutP - FadeOut), 3.0), new Point(X(OutP), actualHeight - 3.0));
			}
		}
		if (Handles)
		{
			Brush brush8 = Ui.Res("WaveHandle");
			Brush b = Ui.Res("WaveHandleInk");
			(double, string)[] array = new (double, string)[2]
			{
				(InP, "VÀO"),
				(OutP, "RA")
			};
			for (int j = 0; j < array.Length; j++)
			{
				(double, string) tuple = array[j];
				double item = tuple.Item1;
				string item2 = tuple.Item2;
				double num6 = Math.Round(X(item));
				g.DrawRectangle(brush8, null, new Rect(num6 - 1.0, 0.0, 3.0, actualHeight));
				FormattedText formattedText = Label(item2, b);
				double num7 = formattedText.Width + 8.0;
				double num8 = ((item2 == "RA") ? (num6 - num7 + 1.0) : num6);
				g.DrawRectangle(brush8, null, new Rect(num8, 0.0, num7, 16.0));
				g.DrawText(formattedText, new Point(num8 + 4.0, 1.0));
			}
		}
		if (Pos.HasValue)
		{
			g.DrawRectangle(Ui.Res("WaveHead"), null, new Rect(Math.Round(X(Pos.Value)) - 1.0, 0.0, 2.0, actualHeight));
		}
		if (Ghost.HasValue)
		{
			double num9 = Math.Round(X(Ghost.Value));
			g.DrawRectangle(brush7, null, new Rect(num9 - 1.0, 0.0, 2.0, actualHeight));
			FormattedText formattedText2 = Label(Text.Fmt(Ghost.Value), Ui.Hex("#1b1407"));
			double num10 = formattedText2.Width + 8.0;
			double num11 = ((num9 + num10 + 2.0 > w) ? (num9 - num10 - 2.0) : (num9 + 2.0));
			g.DrawRectangle(brush7, null, new Rect(num11, 0.0, num10, 15.0));
			g.DrawText(formattedText2, new Point(num11 + 4.0, 0.0));
		}
		double X(double t)
		{
			return t / dur * w;
		}
	}

	private FormattedText Label(string s, Brush b)
	{
		return new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, 11.0, b, VisualTreeHelper.GetDpi(this).PixelsPerDip);
	}
}
