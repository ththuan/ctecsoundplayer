using System.Windows;
using GSoundPlayer.Model;

namespace GSoundPlayer;

public sealed class PickItem
{
	public required Track T { get; init; }

	public bool In { get; init; }

	public int No { get; init; }

	public string Name => T.Name;

	public string Dur
	{
		get
		{
			if (!(T.Dur > 0.0))
			{
				return "";
			}
			return Text.Fmt(T.Len);
		}
	}

	public string NoText
	{
		get
		{
			if (No <= 0)
			{
				return "";
			}
			return No.ToString();
		}
	}

	public Visibility NoVis
	{
		get
		{
			if (No <= 0)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility CheckVis
	{
		get
		{
			if (!In)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public double Dim
	{
		get
		{
			if (!In)
			{
				return 1.0;
			}
			return 0.5;
		}
	}

	public string? Tip
	{
		get
		{
			if (!In)
			{
				return null;
			}
			return "Đã có trong nhóm";
		}
	}
}
