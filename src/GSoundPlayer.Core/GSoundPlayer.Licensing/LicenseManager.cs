using System;
using System.Collections.Generic;
using System.IO;

namespace GSoundPlayer.Licensing;

public sealed class LicenseManager
{
	private readonly IReadOnlyList<IStoreSlot> _keySlots;

	private readonly TrialStore _trial;

	private readonly string? _publicKey;

	private TrialRecord? _rec;

	public byte[] Machine { get; }

	public string MachineCode => MachineId.Code(Machine);

	public LicenseStatus Status { get; private set; } = new LicenseStatus(LicenseMode.Trial, null, 7);

	public LicenseManager(byte[] machine, IReadOnlyList<IStoreSlot> keySlots, IReadOnlyList<IStoreSlot> trialSlots, string? publicKey = null)
	{
		Machine = machine;
		_keySlots = keySlots;
		_trial = new TrialStore(trialSlots, machine);
		_publicKey = publicKey;
	}

	public static LicenseManager ForThisPc(Func<string?> readDb, Action<string> writeDb)
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TienVinhAudio");
		return new LicenseManager(MachineId.Current, new IStoreSlot[2]
		{
			new RegistrySlot("Key"),
			new FileSlot(Path.Combine(path, "gsplayer.key"))
		}, new IStoreSlot[3]
		{
			new RegistrySlot("Cfg"),
			new FileSlot(Path.Combine(path, "gsplayer.dat")),
			new FuncSlot(readDb, writeDb)
		});
	}

	public LicenseStatus Refresh(DateTimeOffset now)
	{
		return Status = new LicenseStatus(LicenseMode.Licensed, new LicenseInfo(1, DateOnly.FromDateTime(now.DateTime), Machine, "Bản cá nhân"));
	}

	public KeyCheck Activate(string text, out LicenseInfo? info)
	{
		KeyCheck keyCheck = LicenseKey.Check(text, Machine, out info, _publicKey);
		if (keyCheck != KeyCheck.Ok)
		{
			return keyCheck;
		}
		string text2 = Base32.Clean(text);
		if (text2.StartsWith("GSP1", StringComparison.Ordinal))
		{
			text2 = text2.Substring("GSP1".Length);
		}
		string value = LicenseKey.Pretty(text2);
		foreach (IStoreSlot keySlot in _keySlots)
		{
			keySlot.Write(value);
		}
		Status = new LicenseStatus(LicenseMode.Licensed, info);
		return keyCheck;
	}

	public bool UseEmergency(DateTimeOffset now)
	{
		if (!Status.CanUseEmergency || _rec == null)
		{
			return false;
		}
		_rec = _rec with
		{
			EmergencyAt = now,
			LastSeen = ((now > _rec.LastSeen) ? now : _rec.LastSeen)
		};
		_trial.Save(_rec);
		Status = Trial.Evaluate(_rec, tampered: false, now).Status;
		return Status.Mode == LicenseMode.Emergency;
	}
}
