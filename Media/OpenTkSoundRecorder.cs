using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;

namespace SmartCar.Media;

public class OpenTkSoundRecorder : IDisposable
{
	private bool _disposedValue;
	private readonly ALCaptureDevice _captureDevice;
	private readonly ILogger<OpenTkSoundRecorder> _logger;

	public OpenTkSoundRecorder(ILogger<OpenTkSoundRecorder> logger)
	{
		_logger = logger;
		CheckALError("Start");

		_logger.LogInformation("Available capture devices: ");
		var list = ALC.GetStringList(GetEnumerationStringList.CaptureDeviceSpecifier);
		foreach (var item in list)
		{
			_logger.LogInformation("  " + item);
		}
		var captureDeviceName = list.FirstOrDefault(d => d.Contains("Jabra"));

		_logger.LogInformation($"Opening for capture: {captureDeviceName}");
		_captureDevice = ALC.CaptureOpenDevice(captureDeviceName, SampleRate, ALFormat.Mono16, 1024);
		CheckALError("Open");
	}

	public int SampleRate { get { return 44100; } }
	public delegate bool StopCondition(Span<short> audioData);

	public SoundData Record(TimeSpan length)
	{
		return Record(length, _ => false);
	}

	public SoundData Record(TimeSpan length, StopCondition stopCondition)
	{
		ObjectDisposedException.ThrowIf(_disposedValue, this);

		//var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
		//Console.WriteLine($"Devices: {string.Join(", ", devices)}");


		// Record a second of data
		CheckALError("Before record");
		short[] recording = new short[(int)(SampleRate * length.TotalSeconds)];

		_logger.LogInformation($"Recording...");
		ALC.CaptureStart(_captureDevice);

		int current = 0;
		bool stop = false;
		while (current < recording.Length && !stop)
		{
			int samplesAvailable = ALC.GetInteger(_captureDevice, AlcGetInteger.CaptureSamples);
			if (samplesAvailable > 512)
			{
				int samplesToRead = Math.Min(samplesAvailable, recording.Length - current);
				ALC.CaptureSamples(_captureDevice, ref recording[current], samplesToRead);
				current += samplesToRead;
				stop |= stopCondition(recording.AsSpan(0, current));
			}
			Thread.Yield();
		}

		ALC.CaptureStop(_captureDevice);

		CheckALError("After record");
		_logger.LogInformation($"Recording stopped");
		if (current < recording.Length)
		{
			recording = recording.AsSpan(0, current).ToArray();
		}
		return new(recording, SampleRate);
	}

	public void CheckALError(string str)
	{
		ALError error = AL.GetError();
		if (error != ALError.NoError)
		{
			_logger.LogError($"ALError at '{str}': {AL.GetErrorString(error)}");
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				//  dispose managed state (managed objects)
				ALC.CaptureCloseDevice(_captureDevice);
			}

			//  free unmanaged resources (unmanaged objects) and override finalizer
			//  set large fields to null
			_disposedValue = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~OpenTkSoundPlayer()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}