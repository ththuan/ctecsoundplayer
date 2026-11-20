using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.2326")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Words_5 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 7)
				{
					ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
					int num2;
					for (num2 = 0; num2 < readOnlySpan.Length - 6; num2++)
					{
						int num3 = readOnlySpan.Slice(num2).IndexOfNonAsciiOrAny_15D5ACCE832D1BED535B6F0B09787C31D52588A8D196E534D4879D32DC902D20();
						if (num3 < 0)
						{
							break;
						}
						num2 += num3;
						if ((uint)(num2 + 2) >= (uint)readOnlySpan.Length)
						{
							break;
						}
						char c;
						if ((((c = readOnlySpan[num2 + 1]) < '\u0080') ? (("\0\0\0Ͽ@Ȁ@Ȁ"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\b\u0001FGYZfgyz\t")) && (((c = readOnlySpan[num2 + 2]) < '\u0080') ? (("\0\0\0Ͽ@\u0004@\u0004"[(int)c >> 4] & (1 << (c & 0xF))) != 0) : RegexRunner.CharInClass(c, "\0\b\u0001FGRSfgrs\t")))
						{
							runtextpos = num + num2;
							return true;
						}
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int capturePosition = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int start2 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (!_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.IsBoundary(inputSpan, num))
				{
					UncaptureUntil(0);
					return false;
				}
				num7 = num;
				num5 = num;
				num4 = Crawlpos();
				if ((uint)span.Length >= 9u && span.StartsWith("official ", StringComparison.OrdinalIgnoreCase))
				{
					num += 9;
					span = inputSpan.Slice(num);
					start2 = num;
					num6 = num;
					capturePosition = Crawlpos();
					if ((uint)span.Length < 2u || !span.StartsWith("mv", StringComparison.OrdinalIgnoreCase))
					{
						goto IL_00c0;
					}
					num3 = 0;
					num += 2;
					span = inputSpan.Slice(num);
					goto IL_01bb;
				}
				goto IL_01cc;
				IL_0262:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 3u || !char.IsDigit(span[0]) || !char.IsDigit(span[1]) || !char.IsDigit(span[2]))
				{
					UncaptureUntil(0);
					return false;
				}
				int i;
				for (i = 3; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				char c;
				if ((uint)span.Length < 4u || ((((c = span[0]) | 0x20) != 107) & (c != 'K')) || !span.Slice(1).StartsWith("bps", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = 2;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_037d;
				IL_0109:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				if ((uint)span.Length < 11u || !span.StartsWith("music video", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0151;
				}
				num3 = 2;
				num += 11;
				span = inputSpan.Slice(num);
				goto IL_01bb;
				IL_0151:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				if ((uint)span.Length >= 5u && span.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
				{
					num3 = 3;
					num += 5;
					span = inputSpan.Slice(num);
					goto IL_01bb;
				}
				goto IL_01cc;
				IL_00c0:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				if ((uint)span.Length < 5u || !span.StartsWith("video", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0109;
				}
				num3 = 1;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_01bb;
				IL_01cc:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length >= 5u && span.StartsWith("lyric", StringComparison.OrdinalIgnoreCase))
				{
					if ((uint)span.Length > 5u && (span[5] | 0x20) == 115)
					{
						span = span.Slice(1);
						num++;
					}
					if ((uint)span.Length >= 11u && span.Slice(5).StartsWith(" video", StringComparison.OrdinalIgnoreCase))
					{
						num2 = 1;
						num += 11;
						span = inputSpan.Slice(num);
						goto IL_037d;
					}
				}
				goto IL_0262;
				IL_01bb:
				Capture(2, start2, num);
				num2 = 0;
				goto IL_037d;
				IL_037d:
				while (true)
				{
					Capture(1, num7, num);
					if (!_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.IsPostWordCharBoundary(inputSpan, num))
					{
						if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						switch (num2)
						{
						case 0:
							break;
						case 1:
							goto IL_0262;
						case 2:
							UncaptureUntil(0);
							return false;
						default:
							continue;
						}
						break;
					}
					runtextpos = num;
					Capture(0, start, num);
					return true;
				}
				if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				switch (num3)
				{
				case 0:
					break;
				case 1:
					goto IL_0109;
				case 2:
					goto IL_0151;
				default:
					goto IL_01bb;
				case 3:
					goto IL_01cc;
				}
				goto IL_00c0;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num8)
				{
					while (Crawlpos() > num8)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Words_5 Instance = new _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Words_5();

	private _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Words_5()
	{
		pattern = "\\b(official (mv|video|music video|audio)|lyrics? video|\\d{3}\\s*kbps)\\b";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}
