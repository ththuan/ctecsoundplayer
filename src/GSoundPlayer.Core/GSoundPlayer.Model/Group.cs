using System.Collections.Generic;

namespace GSoundPlayer.Model;

public sealed record Group(string Key, string Name, string Color)
{
	public List<long> Order { get; init; } = new List<long>();
}
