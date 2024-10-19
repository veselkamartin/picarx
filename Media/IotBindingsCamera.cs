using Iot.Device.Camera;
using Iot.Device.Camera.Settings;
using Iot.Device.Common;
using System.Diagnostics;

namespace SmartCar.Media;

public class IotBindingsCamera : IDisposable, ICamera
{
	private bool _disposedValue;
	private readonly ILogger<IotBindingsCamera> _logger;

	private readonly ProcessSettings _processSettings;
	private readonly ProcessSettings _processSettingsList;

	public IotBindingsCamera(ILogger<IotBindingsCamera> logger)
	{
		_logger = logger;

		_processSettings = ProcessSettingsFactory.CreateForLibcamerastillAndStderr();
		//_processSettings = ProcessSettingsFactory.CreateForRaspistill();
		_processSettingsList = ProcessSettingsFactory.CreateForLibcamerastill();
	}

	public async Task<byte[]> GetPictureAsJpeg()
	{
		ObjectDisposedException.ThrowIf(_disposedValue, this);

		//Console.WriteLine("List of available cameras:");
		//var cams = await List();
		//foreach (var cam in cams)
		//{
		//	Console.WriteLine(cam);
		//}

		var file = Path.GetTempFileName() + ".jpg";

		_logger.LogInformation("Camera taking picture");
		var sw = Stopwatch.StartNew();
		var builder = new CommandOptionsBuilder(false)
			.WithTimeout(1)
			.WithOutput(file)
			//.WithVflip()
			//.WithHflip()
			.WithPictureOptions(90, "jpg")
			.WithResolution(640, 480);
		var args = builder.GetArguments();

		using var proc = new ProcessRunner(_processSettings);
		//Console.WriteLine("Using the following command line:");
		//Console.WriteLine(proc.GetFullCommandLine(args));
		//Console.WriteLine();

		using var stream = new MemoryStream();
		await proc.ExecuteAsync(args, stream);
		//var jpeg = stream.ToArray();
		sw.Stop();
		_logger.LogInformation("Camera picture taken in {Elapsed}ms", sw.ElapsedMilliseconds);
		var jpeg = await File.ReadAllBytesAsync(file);
		File.Delete(file);
		return jpeg;
	}

	public async Task<TimelapseReader> CaptureTimelapse()
	{
		// The false argument avoids the app to output to stdio
		// Time lapse images will be directly saved on disk without
		// writing anything on the terminal
		// Alternatively, we can leave the default (true) and
		// use the '.Remove' method
		var dir = Path.GetTempPath();
		var builder = new CommandOptionsBuilder(false)
			// .Remove(CommandOptionsBuilder.Get(Command.Output))
			.WithOutput(dir + "timelapse_image_%05d.jpg")
			.WithTimeout(30000)
			.WithTimelapse(100)
			.WithVflip()
			.WithHflip()
			.WithResolution(640, 480);
		var args = builder.GetArguments();

		using var proc = new ProcessRunner(_processSettings);
		Console.WriteLine("Using the following command line:");
		Console.WriteLine(proc.GetFullCommandLine(args));
		Console.WriteLine();

		// The ContinuousRunAsync method offload the capture on a separate thread
		// the first await is tied the thread being run
		// the second await is tied to the capture
		var task = await proc.ContinuousRunAsync(args, default(Stream));
		return new TimelapseReader(dir, task, proc);
	}
	public class TimelapseReader : ITimelapseReader
	{
		private string _dir;
		private Task _task;
		private readonly ProcessRunner _proc;

		public TimelapseReader(string dir, Task task, ProcessRunner proc)
		{
			_dir = dir;
			_task = task;
			_proc = proc;
		}

		public async Task<byte[]> Read()
		{
			string[] files;
			var sw = Stopwatch.StartNew();
			do
			{
				files = Directory.GetFiles(_dir, "timelapse_image_*");
				if (files.Length == 0)
				{
					Console.WriteLine("No files in " + _dir);
					await Task.Delay(100);
				}
				//if (_task.IsCompleted)
				//{
				//	Console.WriteLine("Reading camera completed");
				//	throw new Exception("Reading camera completed");
				//}
				if (sw.ElapsedMilliseconds > 10000) throw new Exception("No image captured in 10s");
			} while (files.Length == 0);
			var lastFile = files.Last();
			Console.WriteLine("Image captured:" + lastFile);
			var jpeg = await File.ReadAllBytesAsync(lastFile);
			foreach (var imageFile in files)
			{
				File.Delete(imageFile);
			}
			return jpeg;
		}
		public void Stop()
		{
			_proc.Dispose();
		}
	}

	private async Task<IEnumerable<CameraInfo>> List()
	{
		var builder = new CommandOptionsBuilder()
			.WithListCameras();
		var args = builder.GetArguments();

		using var proc = new ProcessRunner(_processSettingsList);
		Console.WriteLine("Using the following command line:");
		Console.WriteLine(proc.GetFullCommandLine(args));
		Console.WriteLine();

		var text = await proc.ExecuteReadOutputAsStringAsync(args);
		Console.WriteLine($"Output being parsed:");
		Console.WriteLine(text);
		Console.WriteLine();

		var cameras = await CameraInfo.From(text);
		return cameras;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
			}
			_disposedValue = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
	}
}