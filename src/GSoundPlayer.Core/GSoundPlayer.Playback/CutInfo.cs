using GSoundPlayer.Model;

namespace GSoundPlayer.Playback;

public sealed record CutInfo(Track Item, double Pos, bool? Loop);
