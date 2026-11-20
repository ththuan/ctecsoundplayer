using System.Buffers;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.2326")]
internal static class _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	private const int WordCategoriesMask = 262463;

	internal static readonly SearchValues<char> s_ascii_20FF037E0000007E000000 = SearchValues.Create("-0123456789ABCDEFabcdef");

	internal static readonly SearchValues<char> s_ascii_FFFFFFFFFFFF00FCFF6FFFFFFF6FFFFF = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&'()*+,-./:;<=>?@ABCDEFGHIJKMNPQRSTUVWXYZ[\\]^_`abcdefghijkmnpqrstuvwxyz{|}~\u007f");

	internal static readonly SearchValues<string> s_indexOfAnyStrings_OrdinalIgnoreCase_0B7C444E57F9C25253986066043C8A3B194D6F8BD35379CECA7B7B434D8718AF;

	internal static readonly SearchValues<char> s_whitespace;

	private static ReadOnlySpan<byte> WordCharBitmap => new byte[16]
	{
		0, 0, 0, 0, 0, 0, 255, 3, 254, 255,
		255, 135, 254, 255, 255, 7
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfNonAsciiOrAny_15D5ACCE832D1BED535B6F0B09787C31D52588A8D196E534D4879D32DC902D20(this ReadOnlySpan<char> span)
	{
		int num = span.IndexOfAnyExcept(s_ascii_FFFFFFFFFFFF00FCFF6FFFFFFF6FFFFF);
		if ((uint)num < (uint)span.Length)
		{
			if (char.IsAscii(span[num]))
			{
				return num;
			}
			do
			{
				char c;
				if (((c = span[num]) < '\u0080') ? (("\0\0\0Ͽ退\0退\0"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\b\u0001LMOPlmop\t"))
				{
					return num;
				}
				num++;
			}
			while ((uint)num < (uint)span.Length);
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		return ((uint)num < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[num])) != ((uint)index < (uint)inputSpan.Length && IsBoundaryWordChar(inputSpan[index]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsBoundaryWordChar(char ch)
	{
		ReadOnlySpan<byte> wordCharBitmap = WordCharBitmap;
		int num = (int)ch >> 3;
		if ((uint)num < (uint)wordCharBitmap.Length)
		{
			return (wordCharBitmap[num] & (1 << (ch & 7))) != 0;
		}
		bool flag = (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		if (!flag)
		{
			bool flag2 = ((ch == '\u200c' || ch == '\u200d') ? true : false);
			flag = flag2;
		}
		return flag;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsPostWordCharBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		if ((uint)index < (uint)inputSpan.Length)
		{
			return !IsBoundaryWordChar(inputSpan[index]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsPreWordCharBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		if ((uint)num < (uint)inputSpan.Length)
		{
			return !IsBoundaryWordChar(inputSpan[num]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsWordChar(char ch)
	{
		ReadOnlySpan<byte> wordCharBitmap = WordCharBitmap;
		int num = (int)ch >> 3;
		if ((uint)num >= (uint)wordCharBitmap.Length)
		{
			return (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		}
		return (wordCharBitmap[num] & (1 << (ch & 7))) != 0;
	}

	static _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities()
	{
		InlineArray3<string> buffer = default;
		buffer[0] = "y2mate.";
		buffer[1] = "savefrom";
		buffer[2] = "ytmp3.";
		s_indexOfAnyStrings_OrdinalIgnoreCase_0B7C444E57F9C25253986066043C8A3B194D6F8BD35379CECA7B7B434D8718AF = SearchValues.Create(buffer, StringComparison.OrdinalIgnoreCase);
		s_whitespace = SearchValues.Create("\t\n\v\f\r \u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000");
	}
}
