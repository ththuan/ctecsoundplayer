using System;

namespace GSoundPlayer.Licensing;

public static class Trial
{
	public const int Days = 7;

	public static readonly TimeSpan EmergencyLength = TimeSpan.FromHours(24);

	public static readonly TimeSpan ClockSlack = TimeSpan.FromDays(2);

	public static (LicenseStatus Status, TrialRecord Record) Evaluate(TrialRecord? rec, bool tampered, DateTimeOffset now)
	{
		DateOnly dateOnly = DateOnly.FromDateTime(now.DateTime);
		if ((object)rec == null)
		{
			rec = new TrialRecord(tampered ? dateOnly.AddDays(-7) : dateOnly, now, null);
		}
		DateOnly dateOnly2 = rec.Start.AddDays(6);
		bool hasValue = rec.EmergencyAt.HasValue;
		bool emergencyUsed;
		if (now < rec.LastSeen - ClockSlack)
		{
			DateOnly lastDay = dateOnly2;
			emergencyUsed = hasValue;
			return (Status: new LicenseStatus(LicenseMode.ClockBack, null, 0, lastDay, default, emergencyUsed), Record: rec);
		}
		TrialRecord item = rec with
		{
			LastSeen = ((now > rec.LastSeen) ? now : rec.LastSeen)
		};
		if (dateOnly <= dateOnly2)
		{
			int daysLeft = Math.Min(7, dateOnly2.DayNumber - dateOnly.DayNumber + 1);
			DateOnly lastDay2 = dateOnly2;
			emergencyUsed = hasValue;
			return (Status: new LicenseStatus(LicenseMode.Trial, null, daysLeft, lastDay2, default, emergencyUsed), Record: item);
		}
		DateTimeOffset? emergencyAt = rec.EmergencyAt;
		if (emergencyAt.HasValue)
		{
			DateTimeOffset valueOrDefault = emergencyAt.GetValueOrDefault();
			if (now < valueOrDefault + EmergencyLength)
			{
				return (Status: new LicenseStatus(LicenseMode.Emergency, null, 0, dateOnly2, valueOrDefault + EmergencyLength, EmergencyUsed: true), Record: item);
			}
		}
		DateOnly lastDay3 = dateOnly2;
		emergencyUsed = hasValue;
		return (Status: new LicenseStatus(LicenseMode.Expired, null, 0, lastDay3, default, emergencyUsed), Record: item);
	}
}
