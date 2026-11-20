using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace GSoundPlayer.Model;

public static class Text
{
	public static string Vn(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		string text = s.Normalize(NormalizationForm.FormD);
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		string text2 = text;
		foreach (char c in text2)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				char value = ((c != 'Đ' && c != 'đ') ? char.ToLowerInvariant(c) : 'd');
				stringBuilder2.Append(value);
			}
		}
		return stringBuilder.ToString();
	}

	public static string Fmt(double t, bool tenth = false)
	{
		t = Math.Max(0.0, t);
		if (tenth)
		{
			int num = (int)Math.Floor(t / 60.0);
			double num2 = t - (double)(num * 60);
			return num + ":" + ((num2 < 10.0) ? "0" : "") + num2.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
		}
		int num3 = (int)Math.Ceiling(t - 0.001);
		return $"{num3 / 60:00}:{num3 % 60:00}";
	}

	public static string Dec(double v, int d = 1)
	{
		return v.ToString("F" + d, CultureInfo.InvariantCulture).Replace('.', ',').Replace('-', '−');
	}

	public static string Db(double v)
	{
		return ((v > 0.0) ? "+" : "") + Dec(v) + " dB";
	}

	private static Regex JunkA()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__JunkA_0.Instance;
	}

	private static Regex JunkB()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__JunkB_1.Instance;
	}

	private static Regex JunkC()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__JunkC_2.Instance;
	}

	private static Regex Sites()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Sites_3.Instance;
	}

	private static Regex Bracketed()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Bracketed_4.Instance;
	}

	private static Regex Words()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Words_5.Instance;
	}

	private static Regex Underscores()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Underscores_6.Instance;
	}

	private static Regex TrailDash()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__TrailDash_7.Instance;
	}

	private static Regex LeadDash()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__LeadDash_8.Instance;
	}

	private static Regex Spaces()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Spaces_9.Instance;
	}

	private static Regex LeadNumber()
	{
		return _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__LeadNumber_10.Instance;
	}

	public static string FxName(string fileName)
	{
		string item = CleanName(fileName).Name;
		string text = LeadNumber().Replace(item, "");
		if (text.Length <= 0)
		{
			return item;
		}
		return text;
	}

	public static (string Name, bool Junk) CleanName(string fileName)
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
		if (JunkA().IsMatch(fileNameWithoutExtension) || JunkB().IsMatch(fileNameWithoutExtension) || JunkC().IsMatch(fileNameWithoutExtension))
		{
			return (Name: "Bài chưa đặt tên " + (StableHash(fileName) % 900 + 100), Junk: true);
		}
		fileNameWithoutExtension = Sites().Replace(fileNameWithoutExtension, "");
		fileNameWithoutExtension = Bracketed().Replace(fileNameWithoutExtension, "");
		fileNameWithoutExtension = Words().Replace(fileNameWithoutExtension, "");
		fileNameWithoutExtension = Underscores().Replace(fileNameWithoutExtension, " ");
		fileNameWithoutExtension = TrailDash().Replace(fileNameWithoutExtension, "");
		fileNameWithoutExtension = LeadDash().Replace(fileNameWithoutExtension, "");
		fileNameWithoutExtension = Spaces().Replace(fileNameWithoutExtension, " ").Trim();
		return (Name: (fileNameWithoutExtension.Length > 0) ? fileNameWithoutExtension : Path.GetFileNameWithoutExtension(fileName), Junk: false);
	}

	public static string ShortName(string name)
	{
		return Regex.Replace(name, "\\s*-\\s*remix\\s*$", "", RegexOptions.IgnoreCase);
	}

	public static string SafeFileName(string n)
	{
		return Regex.Replace(Regex.Replace(n, "[\\\\/:*?\"<>|\\x00-\\x1f]", "-"), "\\s+", " ").Trim().TrimEnd('.');
	}

	public static uint StableHash(string s)
	{
		uint num = 2166136261u;
		foreach (char c in s)
		{
			num ^= c;
			num *= 16777619;
		}
		return num;
	}
}
