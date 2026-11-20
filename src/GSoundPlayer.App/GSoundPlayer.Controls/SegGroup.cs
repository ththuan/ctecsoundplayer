using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GSoundPlayer.Controls;

public sealed class SegGroup : Border
{
	private readonly StackPanel _panel = new StackPanel
	{
		Orientation = Orientation.Horizontal
	};

	private readonly List<(string Value, Button Btn)> _items = new List<(string, Button)>();

	public string? Value
	{
		set
		{
			foreach (var item in _items)
			{
				var (text, _) = item;
				Ui.SetState(item.Btn, (text == value) ? "on" : "");
			}
		}
	}

	public event Action<string>? Picked;

	public SegGroup(params (string Value, string Label)[] items)
	{
		BorderThickness = new Thickness(1.5);
		CornerRadius = new CornerRadius(6.0);
		SetResourceReference(Border.BorderBrushProperty, "BtnLine");
		ClipToBounds = true;
		HorizontalAlignment = HorizontalAlignment.Left;
		Child = _panel;
		for (int i = 0; i < items.Length; i++)
		{
			(string Value, string Label) tuple = items[i];
			string item = tuple.Value;
			string item2 = tuple.Label;
			string value = item;
			Button button = Ui.Btn("Seg", item2, () =>
			{
				Picked?.Invoke(value);
			});
			_items.Add((item, button));
			_panel.Children.Add(button);
		}
		if (_items.Count > 0)
		{
			List<(string Value, Button Btn)> items2 = _items;
			items2[items2.Count - 1].Btn.BorderThickness = new Thickness(0.0);
		}
	}
}
