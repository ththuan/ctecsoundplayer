using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.15.2326")]
[SkipLocalsInit]
internal sealed class _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__TrailDash_7 : Regex
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
				if (num <= inputSpan.Length - 2)
				{
					while (true)
					{
						ReadOnlySpan<char> span = inputSpan.Slice(num);
						int num2 = span.IndexOf('-');
						if (num2 < 0)
						{
							break;
						}
						int num3 = num2 - 1;
						while ((uint)num3 < (uint)span.Length && char.IsWhiteSpace(span[num3]))
						{
							num3--;
						}
						if (num2 - num3 - 1 < 1)
						{
							num += num2 + 1;
							continue;
						}
						runtextpos = num + num3 + 1;
						runtrackpos = num + num2;
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				num = runtrackpos;
				readOnlySpan = inputSpan.Slice(num);
				if (runtextpos < num)
				{
					runtextpos = num;
				}
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '-')
				{
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[i]); i++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				num3 = num;
				while (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
				{
					if (_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 >= num3)
					{
						return false;
					}
					num = --num3;
					readOnlySpan = inputSpan.Slice(num);
				}
				runtextpos = num;
				Capture(0, start, num);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__TrailDash_7 Instance = new _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__TrailDash_7();

	private _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__TrailDash_7()
	{
		pattern = "\\s+-\\s*$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFE39FE2586793ACA9E051C9878DFC3D98155D546B2FCDF60434B052D8A08B890B__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
