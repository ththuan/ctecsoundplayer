using System;

namespace GSoundPlayer.Licensing;

public sealed record LicenseStatus(LicenseMode Mode, LicenseInfo? License = null, int DaysLeft = 0, DateOnly LastDay = default(DateOnly), DateTimeOffset EmergencyUntil = default(DateTimeOffset), bool EmergencyUsed = false)
{
	public bool Locked
	{
		get
		{
			LicenseMode mode = Mode;
			if ((uint)(mode - 3) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public bool CanUseEmergency
	{
		get
		{
			if (Mode == LicenseMode.Expired)
			{
				return !EmergencyUsed;
			}
			return false;
		}
	}
}
