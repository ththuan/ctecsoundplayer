using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace GSoundPlayer.Licensing;

public static class MachineId
{
	public const int Bytes = 7;

	public const int CodeChars = 12;

	private static byte[]? _current;

	private const uint Rsmb = 1381190978u;

	private static readonly byte[] Placeholder = new byte[16]
	{
		0, 2, 0, 3, 0, 4, 0, 5, 0, 6,
		0, 7, 0, 8, 0, 9
	};

	public static byte[] Current => _current ?? (_current = FromSource(Source()));

	public static byte[] FromSource(string source)
	{
		return SHA256.HashData(Encoding.UTF8.GetBytes("GSoundPlayer/machine/v1\n" + source))[..7];
	}

	public static string Code(ReadOnlySpan<byte> id)
	{
		if (id.Length != 7)
		{
			throw new ArgumentException("Mã máy phải " + 7 + " byte");
		}
		ulong num = 0uL;
		ReadOnlySpan<byte> readOnlySpan = id;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			num = (num << 8) | b;
		}
		num = (num << 4) | Check(id);
		StringBuilder stringBuilder = new StringBuilder(14);
		for (int j = 0; j < 12; j++)
		{
			if (j > 0 && j % 4 == 0)
			{
				stringBuilder.Append('-');
			}
			stringBuilder.Append("0123456789ABCDEFGHJKMNPQRSTVWXYZ"[(int)((num >> 5 * (11 - j)) & 0x1F)]);
		}
		return stringBuilder.ToString();
	}

	public static bool TryParseCode(string? text, out byte[] id)
	{
		id = Array.Empty<byte>();
		string text2 = Base32.Clean(text);
		if (text2.Length != 12)
		{
			return false;
		}
		ulong num = 0uL;
		string text3 = text2;
		foreach (char value in text3)
		{
			int num2 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ".IndexOf(value);
			if (num2 < 0)
			{
				return false;
			}
			num = (num << 5) | (uint)num2;
		}
		byte[] array = new byte[7];
		ulong num3 = num >> 4;
		for (int num4 = 6; num4 >= 0; num4--)
		{
			array[num4] = (byte)num3;
			num3 >>= 8;
		}
		if ((byte)(num & 0xF) != Check(array))
		{
			return false;
		}
		id = array;
		return true;
	}

	private static byte Check(ReadOnlySpan<byte> id)
	{
		return (byte)(SHA256.HashData(id)[0] >> 4);
	}

	public static string Source()
	{
		try
		{
			byte[] array = ReadSmbios();
			if (array != null)
			{
				byte[] array2 = SmbiosUuid(array);
				if (array2 != null && UsableUuid(array2))
				{
					return "uuid:" + Convert.ToHexString(array2);
				}
			}
		}
		catch (Exception ex) when ((ex is EntryPointNotFoundException || ex is DllNotFoundException || ex is OutOfMemoryException) ? true : false)
		{
		}
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
			if (registryKey?.GetValue("MachineGuid") is string { Length: >0 } text)
			{
				return "guid:" + text.Trim().ToLowerInvariant();
			}
		}
		catch (Exception ex2) when ((ex2 is SecurityException || ex2 is UnauthorizedAccessException || ex2 is IOException) ? true : false)
		{
		}
		return "name:" + Environment.MachineName.ToUpperInvariant();
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern uint GetSystemFirmwareTable(uint provider, uint tableId, byte[]? buffer, uint size);

	private static byte[]? ReadSmbios()
	{
		uint systemFirmwareTable = GetSystemFirmwareTable(1381190978u, 0u, null, 0u);
		if (systemFirmwareTable == 0 || systemFirmwareTable > 1048576)
		{
			return null;
		}
		byte[] array = new byte[systemFirmwareTable];
		if (GetSystemFirmwareTable(1381190978u, 0u, array, systemFirmwareTable) != systemFirmwareTable)
		{
			return null;
		}
		return array;
	}

	public static byte[]? SmbiosUuid(ReadOnlySpan<byte> raw)
	{
		if (raw.Length < 8)
		{
			return null;
		}
		int length = (int)Math.Min(BitConverter.ToUInt32(raw.Slice(4, 4)), (uint)(raw.Length - 8));
		ReadOnlySpan<byte> readOnlySpan = raw.Slice(8, length);
		int num = 0;
		while (num + 4 <= readOnlySpan.Length)
		{
			byte b = readOnlySpan[num];
			byte b2 = readOnlySpan[num + 1];
			if (b2 < 4 || num + b2 > readOnlySpan.Length)
			{
				return null;
			}
			if (b == 1 && b2 >= 25)
			{
				return readOnlySpan.Slice(num + 8, 16).ToArray();
			}
			if (b == 127)
			{
				return null;
			}
			int i;
			for (i = num + b2; i + 1 < readOnlySpan.Length && (readOnlySpan[i] != 0 || readOnlySpan[i + 1] != 0); i++)
			{
			}
			num = i + 2;
		}
		return null;
	}

	public static bool UsableUuid(ReadOnlySpan<byte> uuid)
	{
		if (uuid.Length != 16 || uuid.SequenceEqual(Placeholder))
		{
			return false;
		}
		ReadOnlySpan<byte> readOnlySpan = uuid;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			if (readOnlySpan[i] != uuid[0])
			{
				return true;
			}
		}
		return false;
	}
}
