using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace GSoundPlayer.Licensing;

public sealed class TrialStore(IReadOnlyList<IStoreSlot> slots, byte[] machine)
{
	public (TrialRecord? Record, bool Tampered) Load()
	{
		TrialRecord trialRecord = null;
		bool flag = false;
		foreach (IStoreSlot item in slots)
		{
			string text = item.Read();
			if (!string.IsNullOrWhiteSpace(text))
			{
				TrialRecord trialRecord2 = Decode(text, machine);
				if (trialRecord2 == null)
				{
					flag = true;
				}
				else
				{
					trialRecord = ((trialRecord == null) ? trialRecord2 : new TrialRecord((trialRecord2.Start < trialRecord.Start) ? trialRecord2.Start : trialRecord.Start, (trialRecord2.LastSeen > trialRecord.LastSeen) ? trialRecord2.LastSeen : trialRecord.LastSeen, Earliest(trialRecord.EmergencyAt, trialRecord2.EmergencyAt)));
				}
			}
		}
		return (Record: trialRecord, Tampered: flag && trialRecord == null);
	}

	public void Save(TrialRecord r)
	{
		string text = Encode(r, machine);
		foreach (IStoreSlot item in slots)
		{
			if (item.Read() != text)
			{
				item.Write(text);
			}
		}
	}

	private static DateTimeOffset? Earliest(DateTimeOffset? a, DateTimeOffset? b)
	{
		if (a.HasValue)
		{
			if (b.HasValue)
			{
				if (!(a < b))
				{
					return b;
				}
				return a;
			}
			return a;
		}
		return b;
	}

	public static string Encode(TrialRecord r, byte[] machine)
	{
		long value = r.EmergencyAt?.ToUnixTimeSeconds() ?? 0;
		byte[] bytes;
		byte[] array = Mac(bytes = Encoding.ASCII.GetBytes($"1|{r.Start:yyyyMMdd}|{r.LastSeen.ToUnixTimeSeconds()}|{value}"), machine);
		int num = 0;
		byte[] array2 = new byte[bytes.Length + array.Length];
		ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(bytes);
		readOnlySpan.CopyTo(new Span<byte>(array2).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		ReadOnlySpan<byte> readOnlySpan2 = new ReadOnlySpan<byte>(array);
		readOnlySpan2.CopyTo(new Span<byte>(array2).Slice(num, readOnlySpan2.Length));
		num += readOnlySpan2.Length;
		return Convert.ToBase64String(new ReadOnlySpan<byte>(array2));
	}

	public static TrialRecord? Decode(string text, byte[] machine)
	{
		byte[] array;
		try
		{
			array = Convert.FromBase64String(text.Trim());
		}
		catch (FormatException)
		{
			return null;
		}
		if (array.Length < 9)
		{
			return null;
		}
		Span<byte> span = array.AsSpan(0, array.Length - 8);
		if (!CryptographicOperations.FixedTimeEquals(array.AsSpan(array.Length - 8), Mac(span, machine)))
		{
			return null;
		}
		string[] array2 = Encoding.ASCII.GetString(span).Split('|');
		if (array2.Length != 4 || array2[0] != "1")
		{
			return null;
		}
		if (!DateOnly.TryParseExact(array2[1], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			return null;
		}
		if (!long.TryParse(array2[2], CultureInfo.InvariantCulture, out var result2) || !long.TryParse(array2[3], CultureInfo.InvariantCulture, out var result3))
		{
			return null;
		}
		try
		{
			return new TrialRecord(result, DateTimeOffset.FromUnixTimeSeconds(result2).ToLocalTime(), (result3 == 0L) ? ((DateTimeOffset?)null) : new DateTimeOffset?(DateTimeOffset.FromUnixTimeSeconds(result3).ToLocalTime()));
		}
		catch (ArgumentOutOfRangeException)
		{
			return null;
		}
	}

	private static byte[] Mac(ReadOnlySpan<byte> body, byte[] machine)
	{
		ReadOnlySpan<byte> readOnlySpan = "GSoundPlayer/trial/v1\n"u8;
		int num = 0;
		byte[] array = new byte[readOnlySpan.Length + machine.Length];
		readOnlySpan.CopyTo(new Span<byte>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		ReadOnlySpan<byte> readOnlySpan2 = new ReadOnlySpan<byte>(machine);
		readOnlySpan2.CopyTo(new Span<byte>(array).Slice(num, readOnlySpan2.Length));
		num += readOnlySpan2.Length;
		return HMACSHA256.HashData(SHA256.HashData(new ReadOnlySpan<byte>(array)), body)[..8];
	}
}
