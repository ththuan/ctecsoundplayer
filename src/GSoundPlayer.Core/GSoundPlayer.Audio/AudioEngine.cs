using System;
using System.Collections.Generic;
using System.Text;

namespace GSoundPlayer.Audio;

public sealed class AudioEngine : IDisposable
{
	private nint _h;

	public const int ErrNoAudio = -9;

	public OutputStatus Status
	{
		get
		{
			Native.gs_output_get_status(_h, out var s);
			return s;
		}
	}

	public string OutputName
	{
		get
		{
			byte[] array = new byte[512];
			int val = Native.gs_output_name(_h, array, array.Length);
			return Encoding.UTF8.GetString(array, 0, Math.Min(val, array.Length - 1));
		}
	}

	private AudioEngine(nint h)
	{
		_h = h;
	}

	public static AudioEngine Create()
	{
		nint num = Native.gs_engine_create();
		if (num == 0)
		{
			throw new InvalidOperationException("Không khởi tạo được âm thanh");
		}
		return new AudioEngine(num);
	}

	public static AudioEngine CreateOffline(int sampleRate)
	{
		nint num = Native.gs_engine_create_offline(sampleRate);
		if (num == 0)
		{
			throw new InvalidOperationException("Không khởi tạo được âm thanh");
		}
		return new AudioEngine(num);
	}

	public void Render(float[] output, int frames)
	{
		Native.gs_engine_render(_h, output, frames);
	}

	public IReadOnlyList<(string Name, bool IsDefault)> ListDevices()
	{
		int num = Native.gs_devices_refresh(_h);
		List<(string, bool)> list = new List<(string, bool)>(num);
		byte[] array = new byte[512];
		for (int i = 0; i < num; i++)
		{
			int val = Native.gs_device_name(_h, i, array, array.Length);
			list.Add((Encoding.UTF8.GetString(array, 0, Math.Min(val, array.Length - 1)), Native.gs_device_is_default(_h, i) != 0));
		}
		return list;
	}

	public int OpenOutput(string? deviceName, bool exclusive)
	{
		return Native.gs_output_open(_h, deviceName, exclusive ? 1 : 0);
	}

	public void CloseOutput()
	{
		Native.gs_output_close(_h);
	}

	public void SetMasterDb(double db)
	{
		Native.gs_set_master_db(_h, db);
	}

	public void SetDuck(bool on)
	{
		Native.gs_set_duck(_h, on ? 1 : 0);
	}

	public (float L, float R) ReadMeters()
	{
		Native.gs_get_meters(_h, out var l, out var r);
		return (L: l, R: r);
	}

	public int CreateVoice(string path, in VoiceParams p)
	{
		return Native.gs_voice_create(_h, path, in p);
	}

	public void Play(int v, double fadeIn)
	{
		Native.gs_voice_play(_h, v, fadeIn);
	}

	public void Pause(int v)
	{
		Native.gs_voice_pause(_h, v);
	}

	public void Resume(int v, double fadeIn)
	{
		Native.gs_voice_resume(_h, v, fadeIn);
	}

	public void FadeOut(int v, double seconds, FadeCurve curve)
	{
		Native.gs_voice_fade_out(_h, v, seconds, (int)curve);
	}

	public void CancelFade(int v, double seconds)
	{
		Native.gs_voice_cancel_fade(_h, v, seconds);
	}

	public void Stop(int v)
	{
		Native.gs_voice_stop(_h, v);
	}

	public void Release(int v)
	{
		Native.gs_voice_release(_h, v);
	}

	public void Seek(int v, double seconds)
	{
		Native.gs_voice_seek(_h, v, seconds);
	}

	public void SetLoop(int v, bool on)
	{
		Native.gs_voice_set_loop(_h, v, on ? 1 : 0);
	}

	public void SetRegion(int v, double inS, double outS)
	{
		Native.gs_voice_set_region(_h, v, inS, outS);
	}

	public void SetFades(int v, double fadeIn, double fadeOut)
	{
		Native.gs_voice_set_fades(_h, v, fadeIn, fadeOut);
	}

	public void SetGainDb(int v, double db)
	{
		Native.gs_voice_set_gain_db(_h, v, db);
	}

	public VoiceInfo GetInfo(int v)
	{
		Native.gs_voice_get_info(_h, v, out var info);
		return info;
	}

	public void Follow(int v, int next, double atS, double xfadeS)
	{
		Native.gs_voice_follow(_h, v, next, atS, xfadeS);
	}

	public void Unfollow(int v)
	{
		Native.gs_voice_unfollow(_h, v);
	}

	public nint LoadSample(string path, double maxSeconds, out double duration)
	{
		return Native.gs_sample_load(_h, path, maxSeconds, out duration);
	}

	public void FreeSample(nint s)
	{
		Native.gs_sample_free(s);
	}

	public int PlaySample(nint s, double gainDb)
	{
		return Native.gs_sample_play(_h, s, gainDb);
	}

	public static int Analyze(string path, out AnalysisResult r, float[]? wave)
	{
		return Native.gs_analyze_file(path, out r, wave, wave?.Length ?? 0, 0);
	}

	public static int ExtractAudio(string videoPath, string m4aPath)
	{
		return Native.gs_extract_audio(videoPath, m4aPath);
	}

	public static string ReadTitle(string path)
	{
		byte[] array = new byte[1024];
		int num = Native.gs_read_title(path, array, array.Length);
		if (num <= 0)
		{
			return "";
		}
		return Encoding.UTF8.GetString(array, 0, Math.Min(num, array.Length - 1)).Trim();
	}

	public void Dispose()
	{
		if (_h != 0)
		{
			Native.gs_engine_destroy(_h);
			_h = 0;
		}
	}
}
