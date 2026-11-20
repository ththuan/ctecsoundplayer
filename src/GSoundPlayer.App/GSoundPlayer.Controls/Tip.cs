using System;
using System.Windows;

namespace GSoundPlayer.Controls;

public static class Tip
{
	public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached("Text", typeof(string), typeof(Tip), new FrameworkPropertyMetadata(null, OnChanged));

	public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached("Key", typeof(string), typeof(Tip), new FrameworkPropertyMetadata(null, OnChanged));

	internal static event Action<DependencyObject>? Changed;

	public static string? GetText(DependencyObject o)
	{
		return (string)o.GetValue(TextProperty);
	}

	public static void SetText(DependencyObject o, string? v)
	{
		o.SetValue(TextProperty, v);
	}

	public static string? GetKey(DependencyObject o)
	{
		return (string)o.GetValue(KeyProperty);
	}

	public static void SetKey(DependencyObject o, string? v)
	{
		o.SetValue(KeyProperty, v);
	}

	private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		Changed?.Invoke(d);
	}
}
