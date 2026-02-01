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

	public int SampleRate { get { return 24000; } }
	public delegate bool StopCondition(Span<short> audioData);

	public async Task<SoundData> Record(TimeSpan length, CancellationToken ct)
	{
		return await Record(length, _ => false, ct);
	}

	public DeltaRecorder CreateDeltaRecorder()
	{
		ObjectDisposedException.ThrowIf(_disposedValue, this);
		return new DeltaRecorder(this);
	}

	public async Task<SoundData> Record(TimeSpan length, StopCondition stopCondition, CancellationToken ct)
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
		while (current < recording.Length && !stop && !ct.IsCancellationRequested)
		{
			int samplesAvailable = ALC.GetInteger(_captureDevice, AlcGetInteger.CaptureSamples);
			if (samplesAvailable > 512)
			{
				int samplesToRead = Math.Min(samplesAvailable, recording.Length - current);
				ALC.CaptureSamples(_captureDevice, ref recording[current], samplesToRead);
				current += samplesToRead;
				stop |= stopCondition(recording.AsSpan(0, current));
			}
			//Thread.Yield();
			await Task.Delay(10, ct);
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

	public class DeltaRecorder : IDisposable
	{
		private readonly OpenTkSoundRecorder _recorder;
		private bool _disposedValue;

		internal DeltaRecorder(OpenTkSoundRecorder recorder)
		{
			_recorder = recorder;
			recorder. _logger.LogInformation("Starting continuous recording");
			ALC.CaptureStart(recorder._captureDevice);
		}

		public SoundData ReadAvailableSamples()
		{
			ObjectDisposedException.ThrowIf(_disposedValue, this);
			ObjectDisposedException.ThrowIf(_recorder._disposedValue, _recorder);
			int samplesAvailable = ALC.GetInteger(_recorder. _captureDevice, AlcGetInteger.CaptureSamples);
			var buffer = new short[samplesAvailable];
			ALC.CaptureSamples(_recorder._captureDevice, ref buffer[0], samplesAvailable);
			_recorder.CheckALError("After record");
			return new(buffer, _recorder.SampleRate);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_recorder._logger.LogInformation("Stopping continuous recording");
					ALC.CaptureStop(_recorder._captureDevice);
				}
				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}

