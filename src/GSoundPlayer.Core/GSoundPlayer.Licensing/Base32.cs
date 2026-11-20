using System;
using System.Text;

namespace GSoundPlayer.Licensing;

public static class Base32
{
	public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

	public static string Encode(ReadOnlySpan<byte> data)
	{
		StringBuilder stringBuilder = new StringBuilder((data.Length * 8 + 4) / 5);
		int num = 0;
		int num2 = 0;
		ReadOnlySpan<byte> readOnlySpan = data;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			num = (num << 8) | b;
			num2 += 8;
			while (num2 >= 5)
			{
				num2 -= 5;
				stringBuilder.Append("0123456789ABCDEFGHJKMNPQRSTVWXYZ"[(num >> num2) & 0x1F]);
			}
			num &= (1 << num2) - 1;
		}
		if (num2 > 0)
		{
			stringBuilder.Append("0123456789ABCDEFGHJKMNPQRSTVWXYZ"[(num << 5 - num2) & 0x1F]);
		}
		return stringBuilder.ToString();
	}

	public static string Clean(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			bool flag = char.IsWhiteSpace(c);
			if (!flag)
			{
				bool flag2;
				switch (c)
				{
				case '-':
				case '.':
				case '_':
				case '‐':
				case '‑':
				case '–':
				case '—':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (!flag)
			{
				char c2 = char.ToUpperInvariant(c);
				StringBuilder stringBuilder2 = stringBuilder;
				char value;
				switch (c2)
				{
				case 'O':
					value = '0';
					break;
				case 'I':
				case 'L':
					value = '1';
					break;
				default:
					value = c2;
					break;
				}
				stringBuilder2.Append(value);
			}
		}
		return stringBuilder.ToString();
	}

	public static byte[]? Decode(string clean)
	{
		byte[] array = new byte[clean.Length * 5 / 8];
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (char value in clean)
		{
			int num4 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".IndexOf(value);
			if (num4 < 0)
			{
				return null;
			}
			num = (num << 5) | num4;
			num2 += 5;
			if (num2 >= 8)
			{
				num2 -= 8;
				array[num3++] = (byte)(num >> num2);
				num &= (1 << num2) - 1;
			}
		}
		if (num != 0)
		{
			return null;
		}
		return array;
	}
}
