using Iot.Device.Camera;
using Iot.Device.Camera.Settings;
using Iot.Device.Common;
using Microsoft.Extensions.Logging;

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
		var builder = new CommandOptionsBuilder()
			.WithTimeout(1)
			.WithOutput(file)
			//.WithVflip()
			//.WithHflip()
			.WithPictureOptions(90, "jpg")
			.WithResolution(640, 480);
		var args = builder.GetArguments();

		using var proc = new ProcessRunner(_processSettings);
		Console.WriteLine("Using the following command line:");
		Console.WriteLine(proc.GetFullCommandLine(args));
		Console.WriteLine();

		using var stream = new MemoryStream();
		await proc.ExecuteAsync(args, stream);
		//var jpeg = stream.ToArray();
		var jpeg = await File.ReadAllBytesAsync(file);
		_logger.LogInformation("Camera picture taken");
		return jpeg;
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