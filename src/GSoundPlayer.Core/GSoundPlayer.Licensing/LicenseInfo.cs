using System;

namespace GSoundPlayer.Licensing;

public sealed record LicenseInfo(int Id, DateOnly Issued, byte[] Machine, string Customer);
