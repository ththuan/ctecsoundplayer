using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.2326")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Bracketed_4 : Regex
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
				if (num <= inputSpan.Length - 4)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny('(', '[');
					if (num2 >= 0)
					{
						runtextpos = num + num2;
						return true;
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
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				char c;
				if (span.IsEmpty || (((c = span[0]) != '(') & (c != '[')))
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num7 = num;
				int num9 = span.IndexOfAny(')', ']');
				if (num9 < 0)
				{
					num9 = span.Length;
				}
				span = span.Slice(num9);
				num += num9;
				num8 = num;
				while (true)
				{
					num6 = Crawlpos();
					num5 = num;
					num4 = num;
					num3 = Crawlpos();
					if ((uint)span.Length < 8u || !span.StartsWith("official", StringComparison.OrdinalIgnoreCase))
					{
						goto IL_0116;
					}
					num2 = 0;
					num += 8;
					span = inputSpan.Slice(num);
					goto IL_031f;
					IL_0285:
					num = num4;
					span = inputSpan.Slice(num);
					UncaptureUntil(num3);
					if ((uint)span.Length < 4u || ((((c = span[0]) | 0x20) != 107) & (c != 'K')) || !span.Slice(1).StartsWith("bps", StringComparison.OrdinalIgnoreCase))
					{
						goto IL_0097;
					}
					num2 = 3;
					num += 4;
					span = inputSpan.Slice(num);
					goto IL_031f;
					IL_0097:
					UncaptureUntil(num6);
					if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num7 >= num8)
					{
						break;
					}
					num = --num8;
					span = inputSpan.Slice(num);
					continue;
					IL_0116:
					num = num4;
					span = inputSpan.Slice(num);
					UncaptureUntil(num3);
					if ((uint)span.Length < 5u || !span.StartsWith("lyric", StringComparison.OrdinalIgnoreCase))
					{
						goto IL_015e;
					}
					num2 = 1;
					num += 5;
					span = inputSpan.Slice(num);
					goto IL_031f;
					IL_031f:
					while (true)
					{
						Capture(1, num5, num);
						int num10 = span.IndexOfAny(')', ']');
						if (num10 < 0)
						{
							num10 = span.Length;
						}
						span = span.Slice(num10);
						num += num10;
						if (span.IsEmpty || (((c = span[0]) != ')') & (c != ']')))
						{
							if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							switch (num2)
							{
							case 3:
								break;
							case 0:
								goto IL_0116;
							case 1:
								goto IL_015e;
							case 2:
								goto IL_0285;
							default:
								continue;
							}
							break;
						}
						Capture(0, start, runtextpos = num + 1);
						return true;
					}
					goto IL_0097;
					IL_027e:
					num2 = 2;
					goto IL_031f;
					IL_015e:
					num = num4;
					span = inputSpan.Slice(num);
					UncaptureUntil(num3);
					if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.IsPreWordCharBoundary(inputSpan, num) && !span.IsEmpty)
					{
						char c2 = span[0];
						if ((uint)c2 <= 72u)
						{
							if (c2 != '4')
							{
								if (c2 == 'H')
								{
									goto IL_0203;
								}
							}
							else if ((uint)span.Length >= 2u && !((((c = span[1]) | 0x20) != 107) & (c != 'K')) && _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.IsPostWordCharBoundary(inputSpan, num + 2))
							{
								num += 2;
								span = inputSpan.Slice(num);
								goto IL_027e;
							}
						}
						else
						{
							if (c2 != 'M')
							{
								if (c2 == 'h')
								{
									goto IL_0203;
								}
								if (c2 != 'm')
								{
									goto IL_0285;
								}
							}
							if ((uint)span.Length >= 2u && (span[1] | 0x20) == 118 && _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.IsPostWordCharBoundary(inputSpan, num + 2))
							{
								num += 2;
								span = inputSpan.Slice(num);
								goto IL_027e;
							}
						}
					}
					goto IL_0285;
					IL_0203:
					if ((uint)span.Length >= 2u && (span[1] | 0x20) == 100 && _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.IsPostWordCharBoundary(inputSpan, num + 2))
					{
						num += 2;
						span = inputSpan.Slice(num);
						goto IL_027e;
					}
					goto IL_0285;
				}
				UncaptureUntil(0);
				return false;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
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

	internal static readonly _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Bracketed_4 Instance = new _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Bracketed_4();

	private _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Bracketed_4()
	{
		pattern = "[\\[(][^\\])]*(official|lyric|\\bmv\\b|\\bhd\\b|\\b4k\\b|kbps)[^\\])]*[\\])]";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
