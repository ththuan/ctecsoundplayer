using System;

namespace GSoundPlayer.Licensing;

public sealed record TrialRecord(DateOnly Start, DateTimeOffset LastSeen, DateTimeOffset? EmergencyAt);
