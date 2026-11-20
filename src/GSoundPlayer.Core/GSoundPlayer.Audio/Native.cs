using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GSoundPlayer.Audio;

internal static class Native
{
	private const string Dll = "gsaudio";

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern int gs_version();

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern nint gs_engine_create();

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern nint gs_engine_create_offline(int sampleRate);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_engine_destroy(nint e);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static void gs_engine_render(nint e, [Out] float[] output, int frames)
	{
		fixed (float* ptr = &ArrayMarshaller<float, float>.ManagedToUnmanagedIn.GetPinnableReference(output))
		{
			void* _output_native = ptr;
			__PInvoke(e, (float*)_output_native, frames);
		}
		[DllImport("gsaudio", EntryPoint = "gs_engine_render", ExactSpelling = true)]
		unsafe static extern void __PInvoke(nint __e_native, float* __output_native, int __frames_native);
	}

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern int gs_devices_refresh(nint e);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static int gs_device_name(nint e, int index, [Out] byte[] buf, int cap)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(buf))
		{
			void* _buf_native = ptr;
			result = __PInvoke(e, index, (byte*)_buf_native, cap);
		}
		return result;
		[DllImport("gsaudio", EntryPoint = "gs_device_name", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __e_native, int __index_native, byte* __buf_native, int __cap_native);
	}

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern int gs_device_is_default(nint e, int index);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static int gs_output_open(nint e, string? name, int exclusive)
	{
		byte* ptr = default;
		int num = 0;
		scoped Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn = default;
		try
		{
			Span<byte> buffer = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize];
			managedToUnmanagedIn.FromManaged(name, buffer);
			ptr = managedToUnmanagedIn.ToUnmanaged();
			return __PInvoke(e, ptr, exclusive);
		}
		finally
		{
			managedToUnmanagedIn.Free();
		}
		[DllImport("gsaudio", EntryPoint = "gs_output_open", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __e_native, byte* __name_native, int __exclusive_native);
	}

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_output_close(nint e);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static void gs_output_get_status(nint e, out OutputStatus s)
	{
		s = default;
		fixed (OutputStatus* _s_native = &s)
		{
			__PInvoke(e, _s_native);
		}
		[DllImport("gsaudio", EntryPoint = "gs_output_get_status", ExactSpelling = true)]
		unsafe static extern void __PInvoke(nint __e_native, OutputStatus* __s_native);
	}

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static int gs_output_name(nint e, [Out] byte[] buf, int cap)
	{
		int result;
		fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(buf))
		{
			void* _buf_native = ptr;
			result = __PInvoke(e, (byte*)_buf_native, cap);
		}
		return result;
		[DllImport("gsaudio", EntryPoint = "gs_output_name", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __e_native, byte* __buf_native, int __cap_native);
	}

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_set_master_db(nint e, double db);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_set_duck(nint e, int on);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static void gs_get_meters(nint e, out float l, out float r)
	{
		l = 0f;
		r = 0f;
		fixed (float* _r_native = &r)
		{
			fixed (float* _l_native = &l)
			{
				__PInvoke(e, _l_native, _r_native);
			}
		}
		[DllImport("gsaudio", EntryPoint = "gs_get_meters", ExactSpelling = true)]
		unsafe static extern void __PInvoke(nint __e_native, float* __l_native, float* __r_native);
	}

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static int gs_voice_create(nint e, string path, in VoiceParams p)
	{
		byte* ptr = default;
		int num = 0;
		scoped Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn = default;
		try
		{
			Span<byte> buffer = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize];
			managedToUnmanagedIn.FromManaged(path, buffer);
			fixed (VoiceParams* _p_native = &p)
			{
				ptr = managedToUnmanagedIn.ToUnmanaged();
				return __PInvoke(e, ptr, _p_native);
			}
		}
		finally
		{
			managedToUnmanagedIn.Free();
		}
		[DllImport("gsaudio", EntryPoint = "gs_voice_create", ExactSpelling = true)]
		unsafe static extern int __PInvoke(nint __e_native, byte* __path_native, VoiceParams* __p_native);
	}

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_play(nint e, int v, double fadeIn);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_pause(nint e, int v);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_resume(nint e, int v, double fadeIn);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_fade_out(nint e, int v, double seconds, int curve);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_cancel_fade(nint e, int v, double seconds);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_stop(nint e, int v);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_release(nint e, int v);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_seek(nint e, int v, double seconds);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_set_loop(nint e, int v, int on);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_set_region(nint e, int v, double inS, double outS);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_set_fades(nint e, int v, double fadeIn, double fadeOut);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_set_gain_db(nint e, int v, double db);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static void gs_voice_get_info(nint e, int v, out VoiceInfo info)
	{
		info = default;
		fixed (VoiceInfo* _info_native = &info)
		{
			__PInvoke(e, v, _info_native);
		}
		[DllImport("gsaudio", EntryPoint = "gs_voice_get_info", ExactSpelling = true)]
		unsafe static extern void __PInvoke(nint __e_native, int __v_native, VoiceInfo* __info_native);
	}

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_follow(nint e, int v, int next, double atS, double xfadeS);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_voice_unfollow(nint e, int v);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static nint gs_sample_load(nint e, string path, double maxSeconds, out double durationS)
	{
		durationS = 0.0;
		byte* ptr = default;
		nint num = 0;
		scoped Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn = default;
		try
		{
			Span<byte> buffer = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize];
			managedToUnmanagedIn.FromManaged(path, buffer);
			fixed (double* _durationS_native = &durationS)
			{
				ptr = managedToUnmanagedIn.ToUnmanaged();
				return __PInvoke(e, ptr, maxSeconds, _durationS_native);
			}
		}
		finally
		{
			managedToUnmanagedIn.Free();
		}
		[DllImport("gsaudio", EntryPoint = "gs_sample_load", ExactSpelling = true)]
		unsafe static extern nint __PInvoke(nint __e_native, byte* __path_native, double __maxSeconds_native, double* __durationS_native);
	}

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern void gs_sample_free(nint s);

	[DllImport("gsaudio", ExactSpelling = true)]
	public static extern int gs_sample_play(nint e, nint s, double gainDb);

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static int gs_analyze_file(string path, out AnalysisResult r, [Out] float[]? wave, int points, nint cancel)
	{
		r = default;
		int num = 0;
		scoped Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn = default;
		try
		{
			Span<byte> buffer = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize];
			managedToUnmanagedIn.FromManaged(path, buffer);
			fixed (float* ptr = &ArrayMarshaller<float, float>.ManagedToUnmanagedIn.GetPinnableReference(wave))
			{
				void* _wave_native = ptr;
				fixed (AnalysisResult* _r_native = &r)
				{
					return __PInvoke(managedToUnmanagedIn.ToUnmanaged(), _r_native, (float*)_wave_native, points, cancel);
				}
			}
		}
		finally
		{
			managedToUnmanagedIn.Free();
		}
		[DllImport("gsaudio", EntryPoint = "gs_analyze_file", ExactSpelling = true)]
		unsafe static extern int __PInvoke(byte* __path_native, AnalysisResult* __r_native, float* __wave_native, int __points_native, nint __cancel_native);
	}

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static int gs_read_title(string path, [Out] byte[] buf, int cap)
	{
		int num = 0;
		scoped Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn = default;
		try
		{
			Span<byte> buffer = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize];
			managedToUnmanagedIn.FromManaged(path, buffer);
			fixed (byte* ptr = &ArrayMarshaller<byte, byte>.ManagedToUnmanagedIn.GetPinnableReference(buf))
			{
				void* _buf_native = ptr;
				return __PInvoke(managedToUnmanagedIn.ToUnmanaged(), (byte*)_buf_native, cap);
			}
		}
		finally
		{
			managedToUnmanagedIn.Free();
		}
		[DllImport("gsaudio", EntryPoint = "gs_read_title", ExactSpelling = true)]
		unsafe static extern int __PInvoke(byte* __path_native, byte* __buf_native, int __cap_native);
	}

	[GeneratedCode("Microsoft.Interop.LibraryImportGenerator", "10.0.15.2326")]
	[SkipLocalsInit]
	public unsafe static int gs_extract_audio(string src, string dst)
	{
		byte* ptr = default;
		int num = 0;
		scoped Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn = default;
		scoped Utf8StringMarshaller.ManagedToUnmanagedIn managedToUnmanagedIn2 = default;
		try
		{
			string managed = dst;
			Span<byte> buffer = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize];
			managedToUnmanagedIn.FromManaged(managed, buffer);
			managed = src;
			buffer = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.BufferSize];
			managedToUnmanagedIn2.FromManaged(managed, buffer);
			ptr = managedToUnmanagedIn.ToUnmanaged();
			return __PInvoke(managedToUnmanagedIn2.ToUnmanaged(), ptr);
		}
		finally
		{
			managedToUnmanagedIn.Free();
			managedToUnmanagedIn2.Free();
		}
		[DllImport("gsaudio", EntryPoint = "gs_extract_audio", ExactSpelling = true)]
		unsafe static extern int __PInvoke(byte* __src_native, byte* __dst_native);
	}
}
