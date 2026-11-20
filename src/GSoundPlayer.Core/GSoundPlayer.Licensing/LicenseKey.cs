using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace GSoundPlayer.Licensing;

public static class LicenseKey
{
	public const string Prefix = "GSP1";

	public const byte FormatVersion = 1;

	public const byte ProductWindows = 1;

	public const int MaxNameBytes = 40;

	private const int Header = 17;

	private const int SigLen = 64;

	public static readonly DateOnly Epoch = new DateOnly(2026, 1, 1);

	public const string PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEwCk1MxipfEFZyo2MizqbrDYMafXKVcb1G4uE9SbviRpAqMdEi/QrWDH7NnJm4VDXIbPe4coHS+91RMtdzovHVg==";

	public static byte[] Payload(LicenseInfo info, byte product = 1, byte version = 1)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(info.Customer.Trim());
		int num = bytes.Length;
		if ((num > 40 || num == 0) ? true : false)
		{
			throw new ArgumentException($"Tên khách phải có, tối đa {40} byte (chữ có dấu tính 2–3 byte)");
		}
		if (info.Machine.Length != 7)
		{
			throw new ArgumentException("Mã máy sai độ dài");
		}
		num = info.Id;
		if ((num < 1 || num > 65535) ? true : false)
		{
			throw new ArgumentException("Số key phải từ 1 tới 65535");
		}
		int num2 = info.Issued.DayNumber - Epoch.DayNumber;
		if ((num2 < 0 || num2 > 65535) ? true : false)
		{
			throw new ArgumentException("Ngày cấp không hợp lệ");
		}
		byte[] array = new byte[17 + bytes.Length];
		array[0] = version;
		array[1] = product;
		BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(2), (ushort)info.Id);
		BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(4), (ushort)num2);
		info.Machine.CopyTo(array, 9);
		array[16] = (byte)bytes.Length;
		bytes.CopyTo(array, 17);
		return array;
	}

	public static string Create(LicenseInfo info, ECDsa signer, byte product = 1, byte version = 1)
	{
		byte[] array = Payload(info, product, version);
		byte[] array2 = signer.SignData(array, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
		byte[] array3 = array;
		byte[] array4 = array2;
		int num = 0;
		byte[] array5 = new byte[array3.Length + array4.Length];
		ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(array3);
		readOnlySpan.CopyTo(new Span<byte>(array5).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		ReadOnlySpan<byte> readOnlySpan2 = new ReadOnlySpan<byte>(array4);
		readOnlySpan2.CopyTo(new Span<byte>(array5).Slice(num, readOnlySpan2.Length));
		num += readOnlySpan2.Length;
		return Pretty(Base32.Encode(new ReadOnlySpan<byte>(array5)));
	}

	public static string Pretty(string body)
	{
		StringBuilder stringBuilder = new StringBuilder("GSP1");
		for (int i = 0; i < body.Length; i += 5)
		{
			stringBuilder.Append('-').Append(body, i, Math.Min(5, body.Length - i));
		}
		return stringBuilder.ToString();
	}

	public static KeyCheck Check(string? text, ReadOnlySpan<byte> machine, out LicenseInfo? info, string? publicKey = null)
	{
		info = null;
		string text2 = Base32.Clean(text);
		if (text2.Length == 0)
		{
			return KeyCheck.Empty;
		}
		if (text2.StartsWith("GSP1", StringComparison.Ordinal))
		{
			text2 = text2.Substring("GSP1".Length);
		}
		byte[] array = Base32.Decode(text2);
		if (array == null || array.Length < 82)
		{
			return KeyCheck.Malformed;
		}
		if (array[0] != 1)
		{
			if (array[0] <= 1 || array[0] >= 10)
			{
				return KeyCheck.Malformed;
			}
			return KeyCheck.NewerFormat;
		}
		byte b = array[16];
		bool flag = ((b > 40 || b == 0) ? true : false);
		if (flag || array.Length != 17 + b + 64)
		{
			return KeyCheck.Malformed;
		}
		Span<byte> span = array.AsSpan(0, 17 + b);
		using (ECDsa eCDsa = ECDsa.Create())
		{
			try
			{
				eCDsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey ?? "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEwCk1MxipfEFZyo2MizqbrDYMafXKVcb1G4uE9SbviRpAqMdEi/QrWDH7NnJm4VDXIbPe4coHS+91RMtdzovHVg=="), out var _);
			}
			catch (Exception ex) when ((ex is CryptographicException || ex is FormatException) ? true : false)
			{
				return KeyCheck.BadSignature;
			}
			if (!eCDsa.VerifyData(span, array.AsSpan(17 + b, 64), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
			{
				return KeyCheck.BadSignature;
			}
		}
		string customer;
		try
		{
			customer = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(span.Slice(17));
		}
		catch (DecoderFallbackException)
		{
			return KeyCheck.Malformed;
		}
		ushort id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2));
		DateOnly issued = Epoch.AddDays(BinaryPrimitives.ReadUInt16BigEndian(span.Slice(4)));
		info = new LicenseInfo(id, issued, span.Slice(9, 7).ToArray(), customer);
		if (span[1] != 1)
		{
			return KeyCheck.WrongProduct;
		}
		if (span[6] != 0 || span[7] != 0 || span[8] != 0)
		{
			return KeyCheck.NewerFormat;
		}
		if (!((ReadOnlySpan<byte>)span.Slice(9, 7)).SequenceEqual(machine))
		{
			return KeyCheck.WrongMachine;
		}
		return KeyCheck.Ok;
	}
}
