using System.Collections.Generic;

namespace GSoundPlayer.Cues;

public sealed class CueTemplate
{
	public required string Key { get; init; }
	public required string Title { get; init; }
	public required string Description { get; init; }
	public string[] CueTitles { get; init; } = System.Array.Empty<string>();
	public string[] Categories { get; init; } = System.Array.Empty<string>();
	public string[] Buses { get; init; } = System.Array.Empty<string>();
	public bool PerTrack { get; init; }
	public int Limit { get; init; } = 20;
}

public static class CueTemplates
{
	public static readonly CueTemplate[] All = new CueTemplate[]
	{
		new CueTemplate
		{
			Key = "welcome",
			Title = "Đón khách · Khai mạc",
			Description = "Nhạc đón khách, nhạc chờ và nhạc vào lễ khai mạc. Phù hợp trước giờ khai mạc.",
			CueTitles = new[] { "Nhạc đón khách", "Nhạc chờ chương trình", "Nhạc vào khai mạc", "Giới thiệu đại biểu", "Phát biểu khai mạc" },
			Categories = new[] { "welcome", "opening" },
			Buses = new[] { "music" }
		},
		new CueTemplate
		{
			Key = "award",
			Title = "Lễ trao giải",
			Description = "Nhạc vào sân khấu, công bố giải, trao giải và chụp hình lưu niệm.",
			CueTitles = new[] { "Nhạc vào sân khấu", "Công bố giải", "Nhạc trao giải", "Chụp hình lưu niệm" },
			Categories = new[] { "award" },
			Buses = new[] { "music" }
		},
		new CueTemplate
		{
			Key = "sport",
			Title = "Sân đấu · Thi đấu",
			Description = "Nhạc vào sân, nhạc bàn thắng và kết thúc trận. Dùng cho giải đấu, thi đấu nội bộ.",
			CueTitles = new[] { "Nhạc vào sân", "Bàn thắng / điểm số", "Kết thúc trận" },
			Categories = new[] { "sport" },
			Buses = new[] { "music" }
		},
		new CueTemplate
		{
			Key = "game",
			Title = "Minigame · Lucky draw",
			Description = "Nhạc minigame, công bố kết quả và nhạc trao quà.",
			CueTitles = new[] { "Nhạc minigame", "Công bố kết quả", "Nhạc trao quà" },
			Categories = new[] { "game" },
			Buses = new[] { "music" }
		},
		new CueTemplate
		{
			Key = "launch",
			Title = "Khai trương · Ra mắt",
			Description = "Nhạc mở màn, cắt băng khánh thành và phát biểu.",
			CueTitles = new[] { "Nhạc mở màn", "Cắt băng khánh thành", "Đại biểu phát biểu" },
			Categories = new[] { "launch", "opening" },
			Buses = new[] { "music" }
		},
		new CueTemplate
		{
			Key = "conference",
			Title = "Hội nghị · Hội thảo",
			Description = "Nhạc vào hội nghị, giải lao và bế mạc.",
			CueTitles = new[] { "Nhạc vào hội nghị", "Giải lao", "Bế mạc" },
			Categories = new[] { "conference", "closing" },
			Buses = new[] { "music" }
		},
		new CueTemplate
		{
			Key = "gala",
			Title = "Gala · Tiệc",
			Description = "Nhạc mở tiệc, nhạc nền giao lưu và nhạc kết thúc.",
			CueTitles = new[] { "Nhạc mở tiệc", "Nhạc nền giao lưu", "Nhạc kết thúc" },
			Categories = new[] { "gala", "closing" },
			Buses = new[] { "music" }
		},
		new CueTemplate
		{
			Key = "fx",
			Title = "Hiệu ứng sân khấu",
			Description = "Sting, whoosh, trống hồi hộp và pháo tay. Có thể phát chồng lên nhạc nền.",
			CueTitles = new[] { "Sting mở màn", "Whoosh chuyển cảnh", "Trống hồi hộp", "Pháo tay", "Nhạc hiệu kết thúc" },
			Categories = new[] { "fx" },
			Buses = new[] { "fx" }
		},
		new CueTemplate
		{
			Key = "music",
			Title = "Chương trình âm nhạc",
			Description = "Mỗi bài trong kho là một tiết mục. Tự chạy lần lượt, có thể thêm beat và bài mới.",
			CueTitles = new[] { "Mở màn", "Tiết mục 1", "Tiết mục 2", "Tiết mục 3", "Tiết mục 4", "Tiết mục 5", "Tiết mục 6", "Kết thúc" },
			Buses = new[] { "music" },
			PerTrack = true,
			Limit = 20
		},
		new CueTemplate
		{
			Key = "beats",
			Title = "Chương trình beat",
			Description = "Khung cue cho beat / nhạc nền. Tạo trước rồi gán file beat của bạn vào từng cue.",
			CueTitles = new[] { "Beat mở màn", "Beat 1", "Beat 2", "Beat 3", "Beat 4", "Beat 5", "Beat 6", "Beat kết thúc" }
		},
		new CueTemplate
		{
			Key = "empty",
			Title = "Kịch bản trống",
			Description = "Tự thêm cue, tự chọn bài từ kho. Dùng khi chương trình không theo mẫu nào."
		}
	};

	public static CueTemplate Find(string key)
	{
		foreach (CueTemplate template in All)
		{
			if (template.Key == key)
			{
				return template;
			}
		}
		return All[All.Length - 1];
	}
}
