using System;
using System.Collections.Generic;

namespace GSoundPlayer.Model;

public sealed class Track
{
	public long Id;

	public string FilePath = "";

	public string FileName = "";

	private string _name = "";

	public List<string> Tags = new List<string>();

	public bool Fav;

	public double Dur;

	public double? Lufs;

	public double? TruePeakDb;

	public double InP;

	public double? OutP;

	public double FadeIn;

	public double FadeOut;

	public double Vol;

	public int Warn = 10;

	public bool Loud = true;

	public string Note = "";

	public bool Edited;

	public bool Junk;

	public bool AutoTrim;

	public long LastPlayed;

	public long Imported;

	public string QuickHash = "";

	public string? WatchRoot;

	public float[]? Wave;

	public bool Analyzed;

	public bool Analyzing;

	public bool Missing;

	public string SearchKey { get; private set; } = "";

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			SearchKey = Text.Vn(value);
		}
	}

	public bool Shaped
	{
		get
		{
			if (!(InP > 0.05) || AutoTrim)
			{
				double? outP = OutP;
				if (outP.HasValue)
				{
					double valueOrDefault = outP.GetValueOrDefault();
					if (Dur <= 0.0 || valueOrDefault < Dur - 0.05)
					{
						goto IL_008e;
					}
				}
				if (!(FadeIn > 0.05) && !(FadeOut > 0.05))
				{
					return Math.Abs(Vol) > 0.05;
				}
			}
			goto IL_008e;
			IL_008e:
			return true;
		}
	}

	public double Out => OutP ?? Dur;

	public double Len => Math.Max(0.0, Out - InP);

	public (string, double, double, double, double) Signature => (FilePath, InP, Out, FadeIn, FadeOut);
}
