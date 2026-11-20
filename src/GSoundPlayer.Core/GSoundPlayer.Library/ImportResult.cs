using System.Collections.Generic;
using GSoundPlayer.Model;

namespace GSoundPlayer.Library;

public sealed record ImportResult(List<Track> Added, int Duplicates, int Bad, List<string> NoAudio);
