using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GSoundPlayer.Controls;
using GSoundPlayer.Library;

namespace GSoundPlayer;

public partial class App : Application
{
	private const string MutexName = "CTECSoundPlayer.OneInstance";

	private const string PipeName = "CTECSoundPlayer.OpenFile";

	private Mutex? _mutex;

	private CancellationTokenSource? _pipeStop;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		DispatcherUnhandledException += OnUnhandled;
		string text = e.Args.FirstOrDefault((string a) => !a.StartsWith("--", StringComparison.Ordinal));
		_mutex = new Mutex(initiallyOwned: true, "CTECSoundPlayer.OneInstance", out var createdNew);
		if (!createdNew && !e.Args.Contains("--shot") && !e.Args.Contains("--selftest"))
		{
			if (text != null)
			{
				SendToRunning(text);
			}
			Shutdown();
			return;
		}
		GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
		AppPaths.Ensure();
		Resources["AppFont"] = Ui.AppFont;
		MainWindow mainWindow;
		try
		{
			mainWindow = new MainWindow(e.Args, text);
		}
		catch (DllNotFoundException)
		{
			MessageBox.Show("Thiếu file gsaudio.dll bên cạnh phần mềm. Cài lại phần mềm giúp.", "CTECSoundPlayer", MessageBoxButton.OK, MessageBoxImage.Hand);
			Shutdown();
			return;
		}
		MainWindow = mainWindow;
		mainWindow.Show();
		_pipeStop = new CancellationTokenSource();
		ListenForFilesAsync(mainWindow, _pipeStop.Token);
	}

	private static void SendToRunning(string path)
	{
		try
		{
			using NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "CTECSoundPlayer.OpenFile", PipeDirection.Out);
			namedPipeClientStream.Connect(1500);
			using StreamWriter streamWriter = new StreamWriter(namedPipeClientStream);
			streamWriter.WriteLine(Path.GetFullPath(path));
		}
		catch (Exception ex) when ((ex is IOException || ex is TimeoutException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
	}

	private static async Task ListenForFilesAsync(MainWindow window, CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				using NamedPipeServerStream s = new NamedPipeServerStream("CTECSoundPlayer.OpenFile", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
				await s.WaitForConnectionAsync(ct);
				using StreamReader r = new StreamReader(s);
				string text = await r.ReadLineAsync(ct);
				if (!string.IsNullOrWhiteSpace(text))
				{
					window.OpenExternalFile(text.Trim());
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (IOException)
			{
				await Task.Delay(500, CancellationToken.None);
			}
		}
	}

	private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		try
		{
			File.AppendAllText(Path.Combine(AppPaths.Root, "loi.log"), $"{DateTime.Now:o}\n{e.Exception}\n\n");
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
		{
		}
		(MainWindow as MainWindow)?.ToastError("Có lỗi nhỏ ở giao diện, nhạc vẫn chạy. Lỗi đã được ghi lại");
		e.Handled = true;
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_pipeStop?.Cancel();
		_mutex?.Dispose();
		base.OnExit(e);
	}
}
