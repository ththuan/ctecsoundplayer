using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GSoundPlayer.Controls;

public sealed class IconView : FrameworkElement
{
	private static readonly Dictionary<string, Geometry> Shapes;

	public static readonly DependencyProperty KindProperty;

	public static readonly DependencyProperty IconSizeProperty;

	public static readonly DependencyProperty ForegroundProperty;

	public string Kind
	{
		get
		{
			return (string)GetValue(KindProperty);
		}
		set
		{
			SetValue(KindProperty, value);
		}
	}

	public double IconSize
	{
		get
		{
			return (double)GetValue(IconSizeProperty);
		}
		set
		{
			SetValue(IconSizeProperty, value);
		}
	}

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

	private static Geometry P(string d)
	{
		Geometry geometry = Geometry.Parse(d);
		geometry.Freeze();
		return geometry;
	}

	static IconView()
	{
		Shapes = new Dictionary<string, Geometry>
		{
			["fade"] = P("M3 5 C10 5 12 18 21 18 H3 Z"),
			["stop"] = P("M6 6 H18 V18 H6 Z"),
			["pause"] = P("M7 5 H10 V19 H7 Z M14 5 H17 V19 H14 Z"),
			["play"] = P("M7 4.5 L19 12 L7 19.5 Z"),
			["mic"] = P("M12 3 A3 3 0 0 0 9 6 V12 A3 3 0 0 0 15 12 V6 A3 3 0 0 0 12 3 Z M5 11 V12 A7 7 0 0 0 19 12 V11 M12 19 V22 M8.5 22 H15.5"),
			["loop"] = P("M17 2 L21 6 L17 10 M3 11 V10 A4 4 0 0 1 7 6 H21 M7 22 L3 18 L7 14 M21 13 V14 A4 4 0 0 1 17 18 H3"),
			["panic"] = P("M8 2.5 H16 L21.5 8 V16 L16 21.5 H8 L2.5 16 V8 Z M9 9 L15 15 M15 9 L9 15"),
			["prev"] = P("M19 5 V19 L8 12 Z M5 5 V19"),
			["next"] = P("M5 5 V19 L16 12 Z M19 5 V19"),
			["back"] = P("M3 12 A9 9 0 1 0 12 3 A9.75 9.75 0 0 0 5.26 5.74 L3 8 M3 3 V8 H8"),
			["fwd"] = P("M21 12 A9 9 0 1 1 12 3 A9.75 9.75 0 0 1 18.74 5.74 L21 8 M21 3 V8 H16"),
			["undo"] = P("M9 14 L4 9 L9 4 M4 9 H14.5 A5.5 5.5 0 0 1 14.5 20 H11"),
			["loud"] = P("M3.5 17 A9 9 0 1 1 20.5 17 M12 14 L16 10 M12 14 V14.01 M5.5 17 H8 M16 17 H18.5"),
			["loud-off"] = P("M3.5 17 A9 9 0 1 1 20.5 17 M12 14 L16 10 M12 14 V14.01 M3 4 L21 20"),
			["settings"] = P("M9.88 5.12 L10.37 2.64 L13.63 2.64 L14.12 5.12 A7.2 7.2 0 0 1 15.37 5.64 L17.46 4.23 L19.77 6.54 L18.36 8.63 A7.2 7.2 0 0 1 18.88 9.88 L21.36 10.37 L21.36 13.63 L18.88 14.12 A7.2 7.2 0 0 1 18.36 15.37 L19.77 17.46 L17.46 19.77 L15.37 18.36 A7.2 7.2 0 0 1 14.12 18.88 L13.63 21.36 L10.37 21.36 L9.88 18.88 A7.2 7.2 0 0 1 8.63 18.36 L6.54 19.77 L4.23 17.46 L5.64 15.37 A7.2 7.2 0 0 1 5.12 14.12 L2.64 13.63 L2.64 10.37 L5.12 9.88 A7.2 7.2 0 0 1 5.64 8.63 L4.23 6.54 L6.54 4.23 L8.63 5.64 A7.2 7.2 0 0 1 9.88 5.12 Z M15 12 A3 3 0 1 1 9 12 A3 3 0 1 1 15 12 Z"),
			["sun"] = P("M12 8 A4 4 0 1 1 12 16 A4 4 0 1 1 12 8 Z M12 2 V4 M12 20 V22 M4.93 4.93 L6.34 6.34 M17.66 17.66 L19.07 19.07 M2 12 H4 M20 12 H22 M6.34 17.66 L4.93 19.07 M19.07 4.93 L17.66 6.34"),
			["moon"] = P("M12 3 A6 6 0 0 0 21 12 A9 9 0 1 1 12 3 Z"),
			["full"] = P("M4 9 V4 H9 M15 4 H20 V9 M20 15 V20 H15 M9 20 H4 V15"),
			["volume"] = P("M4 9.5 H7.5 L12 5.5 V18.5 L7.5 14.5 H4 Z M15.5 9 A4 4 0 0 1 15.5 15 M18.5 6.5 A7.5 7.5 0 0 1 18.5 17.5"),
			["search"] = P("M17 10.5 A6.5 6.5 0 1 1 4 10.5 A6.5 6.5 0 1 1 17 10.5 Z M15.2 15.2 L20.5 20.5"),
			["folder"] = P("M3 6.5 A1.5 1.5 0 0 1 4.5 5 H9 L11 7.5 H19.5 A1.5 1.5 0 0 1 21 9 V17.5 A1.5 1.5 0 0 1 19.5 19 H4.5 A1.5 1.5 0 0 1 3 17.5 Z"),
			["more"] = P("M6.2 12 A1.2 1.2 0 1 1 3.8 12 A1.2 1.2 0 1 1 6.2 12 Z M13.2 12 A1.2 1.2 0 1 1 10.8 12 A1.2 1.2 0 1 1 13.2 12 Z M20.2 12 A1.2 1.2 0 1 1 17.8 12 A1.2 1.2 0 1 1 20.2 12 Z"),
			["trash"] = P("M4 6.5 H20 M9.5 6.5 V4.5 A1 1 0 0 1 10.5 3.5 H13.5 A1 1 0 0 1 14.5 4.5 V6.5 M6 6.5 L7 19.5 A1.5 1.5 0 0 0 8.5 21 H15.5 A1.5 1.5 0 0 0 17 19.5 L18 6.5 M10 10.5 V17 M14 10.5 V17"),
			["pencil"] = P("M15.5 4.5 L19.5 8.5 L8.5 19.5 L4 20 L4.5 15.5 Z M13.5 6.5 L17.5 10.5"),
			["plus"] = P("M12 5 V19 M5 12 H19"),
			["list-plus"] = P("M4 6 H16 M4 11 H16 M4 16 H11 M17.5 13.5 V20.5 M14 17 H21"),
			["chev-left"] = P("M15 5 L8 12 L15 19"),
			["chev-right"] = P("M9 5 L16 12 L9 19"),
			["chev-up"] = P("M5 15 L12 8 L19 15"),
			["chev-down"] = P("M5 9 L12 16 L19 9"),
			["x"] = P("M6 6 L18 18 M18 6 L6 18"),
			["star"] = P("M12 3 L14.41 9.28 L21.13 9.63 L15.9 13.87 L17.64 20.37 L12 16.7 L6.36 20.37 L8.1 13.87 L2.87 9.63 L9.59 9.28 Z"),
			["scissors"] = P("M9 6.5 A2.5 2.5 0 1 1 4 6.5 A2.5 2.5 0 1 1 9 6.5 Z M9 17.5 A2.5 2.5 0 1 1 4 17.5 A2.5 2.5 0 1 1 9 17.5 Z M8.6 8 L20 18 M8.6 16 L20 6"),
			["download"] = P("M12 4 V15 M7.5 10.5 L12 15 L16.5 10.5 M4 17 V19 A1 1 0 0 0 5 20 H19 A1 1 0 0 0 20 19 V17"),
			["upload"] = P("M12 15 V4 M7.5 8.5 L12 4 L16.5 8.5 M4 17 V19 A1 1 0 0 0 5 20 H19 A1 1 0 0 0 20 19 V17"),
			["alert"] = P("M10.3 4.5 A2 2 0 0 1 13.7 4.5 L21.2 17.5 A2 2 0 0 1 19.5 20.5 H4.5 A2 2 0 0 1 2.8 17.5 Z M12 9.5 V14 M12 17 V17.1"),
			["keyboard"] = P("M4 6 H20 A1 1 0 0 1 21 7 V17 A1 1 0 0 1 20 18 H4 A1 1 0 0 1 3 17 V7 A1 1 0 0 1 4 6 Z M7 10 H7.01 M10.5 10 H10.51 M14 10 H14.01 M17.5 10 H17.51 M7 13.5 H7.01 M17.5 13.5 H17.51 M9.5 14.5 H14.5"),
			["music"] = P("M9 18 V5 L20 3 V16 M9 18 A3 3 0 1 1 3 18 A3 3 0 1 1 9 18 Z M20 16 A3 3 0 1 1 14 16 A3 3 0 1 1 20 16 Z"),
			["check"] = P("M5 12.5 L10 17.5 L19 7")
		};
		KindProperty = DependencyProperty.Register("Kind", typeof(string), typeof(IconView), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
		IconSizeProperty = DependencyProperty.Register("IconSize", typeof(double), typeof(IconView), new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));
		ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(IconView), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));
		FrameworkElement.HorizontalAlignmentProperty.OverrideMetadata(typeof(IconView), new FrameworkPropertyMetadata(HorizontalAlignment.Center));
		FrameworkElement.VerticalAlignmentProperty.OverrideMetadata(typeof(IconView), new FrameworkPropertyMetadata(VerticalAlignment.Center));
		UIElement.SnapsToDevicePixelsProperty.OverrideMetadata(typeof(IconView), new FrameworkPropertyMetadata(false));
	}

	public static bool Has(string kind)
	{
		return Shapes.ContainsKey(kind);
	}

	protected override Size MeasureOverride(Size available)
	{
		return new Size(IconSize, IconSize);
	}

	protected override void OnRender(DrawingContext dc)
	{
		if (Shapes.TryGetValue(Kind, out Geometry value))
		{
			Geometry geometry = value.CloneCurrentValue();
			double num = IconSize / 24.0;
			geometry.Transform = new ScaleTransform(num, num);
			Pen pen = new Pen(Foreground, 1.7)
			{
				StartLineCap = PenLineCap.Round,
				EndLineCap = PenLineCap.Round,
				LineJoin = PenLineJoin.Round
			};
			dc.DrawGeometry(null, pen, geometry);
		}
	}
}
