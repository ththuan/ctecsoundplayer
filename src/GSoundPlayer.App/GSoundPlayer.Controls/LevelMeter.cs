using System.Windows;
using System.Windows.Media;

namespace GSoundPlayer.Controls;

public sealed class LevelMeter : FrameworkElement
{
	private double _level;

	private double _peak;

	private (int, int, Brush) _last;

	public void Set(double level, double peak)
	{
		_level = level;
		_peak = peak;
		double actualHeight = ActualHeight;
		(int, int, Brush) last = ((int)(level * actualHeight), (int)(peak * actualHeight), Ui.Res("MeterBg"));
		if (!last.Equals(_last))
		{
			_last = last;
			InvalidateVisual();
		}
	}

	protected override void OnRender(DrawingContext g)
	{
		double actualWidth = ActualWidth;
		double actualHeight = ActualHeight;
		g.DrawRoundedRectangle(Ui.Res("MeterBg"), null, new Rect(0.0, 0.0, actualWidth, actualHeight), 3.0, 3.0);
		if (!(actualWidth <= 0.0) && !(actualHeight <= 0.0))
		{
			Color color = ((SolidColorBrush)Ui.Res("Green")).Color;
			Color color2 = ((SolidColorBrush)Ui.Res("Green2")).Color;
			Color color3 = ((SolidColorBrush)Ui.Res("Amber")).Color;
			Color color4 = ((SolidColorBrush)Ui.Res("Red")).Color;
			LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new GradientStopCollection
			{
				new GradientStop(color, 0.0),
				new GradientStop(color2, 0.7),
				new GradientStop(color3, 0.88),
				new GradientStop(color4, 1.0)
			}, new Point(0.0, 1.0), new Point(0.0, 0.0))
			{
				MappingMode = BrushMappingMode.RelativeToBoundingBox
			};
			linearGradientBrush.Freeze();
			g.PushClip(new RectangleGeometry(new Rect(0.0, actualHeight - _level * actualHeight, actualWidth, _level * actualHeight)));
			g.DrawRoundedRectangle(linearGradientBrush, null, new Rect(0.0, 0.0, actualWidth, actualHeight), 3.0, 3.0);
			g.Pop();
			g.PushOpacity(0.8);
			g.DrawRectangle(Ui.Res("MeterPeak"), null, new Rect(0.0, actualHeight - _peak * actualHeight, actualWidth, 2.0));
			g.Pop();
		}
	}
}
