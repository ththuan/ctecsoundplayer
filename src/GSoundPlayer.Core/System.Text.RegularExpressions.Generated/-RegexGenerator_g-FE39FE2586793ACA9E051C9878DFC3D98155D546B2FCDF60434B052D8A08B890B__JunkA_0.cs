using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.2326")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__JunkA_0 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 7 && num == 0)
				{
					return true;
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					UncaptureUntil(0);
					return false;
				}
				num7 = num;
				num5 = num;
				num4 = Crawlpos();
				if ((uint)span.Length < 3u || !span.StartsWith("aud", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0074;
				}
				num2 = 0;
				num += 3;
				span = inputSpan.Slice(num);
				goto IL_02ac;
				IL_01d9:
				num2 = 3;
				goto IL_02ac;
				IL_00bc:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 3u || !span.StartsWith("ptt", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0104;
				}
				num2 = 2;
				num += 3;
				span = inputSpan.Slice(num);
				goto IL_02ac;
				IL_01e0:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 5u || !span.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_0228;
				}
				num2 = 4;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_02ac;
				IL_02ac:
				while (true)
				{
					Capture(1, num7, num);
					char c;
					if (!span.IsEmpty && (((c = span[0]) == ' ') | (c == '-') | (c == '_')))
					{
						span = span.Slice(1);
						num++;
					}
					int i;
					for (i = 0; (uint)i < (uint)span.Length && char.IsDigit(span[i]); i++)
					{
					}
					if (i < 4)
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
							goto IL_00bc;
						case 2:
							goto IL_0104;
						case 3:
							goto IL_01c3;
						case 4:
							goto IL_0228;
						case 5:
							UncaptureUntil(0);
							return false;
						default:
							continue;
						}
						break;
					}
					span = span.Slice(i);
					Capture(0, start, runtextpos = num + i);
					return true;
				}
				goto IL_0074;
				IL_0074:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 3u || !span.StartsWith("vid", StringComparison.OrdinalIgnoreCase))
				{
					goto IL_00bc;
				}
				num2 = 1;
				num += 3;
				span = inputSpan.Slice(num);
				goto IL_02ac;
				IL_01c3:
				if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num3 == 0)
				{
					goto IL_0175;
				}
				if (num3 != 1)
				{
					goto IL_01d9;
				}
				goto IL_01e0;
				IL_0104:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length >= 2u && span.StartsWith("re", StringComparison.OrdinalIgnoreCase))
				{
					num6 = num;
					capturePosition = Crawlpos();
					if ((uint)span.Length < 3u || (span[2] | 0x20) != 99)
					{
						goto IL_0175;
					}
					num3 = 0;
					num += 3;
					span = inputSpan.Slice(num);
					goto IL_01d9;
				}
				goto IL_01e0;
				IL_0228:
				num = num5;
				span = inputSpan.Slice(num);
				UncaptureUntil(num4);
				if ((uint)span.Length < 5u || !span.StartsWith("voice", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = 5;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_02ac;
				IL_0175:
				num = num6;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				if ((uint)span.Length >= 9u && span.Slice(2).StartsWith("cording", StringComparison.OrdinalIgnoreCase))
				{
					num3 = 1;
					num += 9;
					span = inputSpan.Slice(num);
					goto IL_01d9;
				}
				goto IL_01e0;
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

	internal static readonly _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__JunkA_0 Instance = new _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__JunkA_0();

	private _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__JunkA_0()
	{
		pattern = "^(AUD|VID|PTT|REC|Recording|audio|voice)[-_ ]?\\d{4,}";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
