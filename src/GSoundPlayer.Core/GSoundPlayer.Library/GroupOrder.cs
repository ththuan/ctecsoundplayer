using System;
using System.Collections.Generic;
using System.Linq;
using GSoundPlayer.Model;

namespace GSoundPlayer.Library;

public static class GroupOrder
{
	public static List<Track> Sort(IEnumerable<Track> members, IReadOnlyList<long> order)
	{
		Dictionary<long, int> pos = new Dictionary<long, int>();
		for (int i = 0; i < order.Count; i++)
		{
			pos.TryAdd(order[i], i);
		}
		return members.OrderBy((Track t) => (!pos.TryGetValue(t.Id, out var value)) ? int.MaxValue : value).ThenBy((Track t) => t.SearchKey, StringComparer.Ordinal).ToList();
	}

	public static List<Track> Place(IReadOnlyList<Track> members, IEnumerable<Track> moving, int? at)
	{
		List<Track> list = moving.Distinct().ToList();
		HashSet<Track> set = list.ToHashSet();
		List<Track> list2 = members.Where((Track t) => !set.Contains(t)).ToList();
		int num;
		if (at.HasValue)
		{
			int valueOrDefault = at.GetValueOrDefault();
			num = members.Take(Math.Clamp(valueOrDefault, 0, members.Count)).Count((Track t) => !set.Contains(t));
		}
		else
		{
			num = list2.Count;
		}
		int index = num;
		list2.InsertRange(index, list);
		return list2;
	}

	public static List<Track> Shift(IReadOnlyList<Track> members, IEnumerable<Track> selected, int dir)
	{
		List<Track> list = members.ToList();
		HashSet<Track> hashSet = selected.ToHashSet();
		if (dir < 0)
		{
			for (int i = 1; i < list.Count; i++)
			{
				if (hashSet.Contains(list[i]) && !hashSet.Contains(list[i - 1]))
				{
					List<Track> list2 = list;
					int index = i - 1;
					int index2 = i;
					Track value = list[i];
					Track value2 = list[i - 1];
					list2[index] = value;
					list[index2] = value2;
				}
			}
		}
		else
		{
			for (int num = list.Count - 2; num >= 0; num--)
			{
				if (hashSet.Contains(list[num]) && !hashSet.Contains(list[num + 1]))
				{
					List<Track> list2 = list;
					int index2 = num + 1;
					int index = num;
					Track value2 = list[num];
					Track value = list[num + 1];
					list2[index2] = value2;
					list[index] = value;
				}
			}
		}
		return list;
	}

	public static List<long> Ids(IReadOnlyList<Track> members, IReadOnlyList<long> old, IReadOnlySet<long> inLibrary)
	{
		List<long> list = members.Select((Track t) => t.Id).ToList();
		HashSet<long> hashSet = list.ToHashSet();
		for (int num = 0; num < old.Count; num++)
		{
			if (!inLibrary.Contains(old[num]) && hashSet.Add(old[num]))
			{
				list.Insert(Math.Min(num, list.Count), old[num]);
			}
		}
		return list;
	}

	public static List<long> Prune(IReadOnlyList<long> order, IReadOnlySet<long> members)
	{
		return order.Where(members.Contains).Distinct().ToList();
	}
}
