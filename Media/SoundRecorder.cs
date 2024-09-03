using OpenTK.Audio.OpenAL;

namespace SmartCar.Media;

public class SoundRecorder: IDisposable
{
	private bool _disposedValue;
	private readonly ALCaptureDevice _captureDevice;

	public SoundRecorder()
	{
		CheckALError("Start");

		Console.WriteLine("Available capture devices: ");
		var list = ALC.GetStringList(GetEnumerationStringList.CaptureDeviceSpecifier);
		foreach (var item in list)
		{
			Console.WriteLine("  " + item);
		}
		var captureDeviceName = list.FirstOrDefault(d => d.Contains("Jabra"));

		Console.WriteLine($"Opening for capture: {captureDeviceName}");
		_captureDevice = ALC.CaptureOpenDevice(captureDeviceName, SampleRate, ALFormat.Mono16, 1024);
	}

	public int SampleRate { get { return 44100; } }
	public SoundData Record(TimeSpan length)
	{
		ObjectDisposedException.ThrowIf(_disposedValue, this);

		//var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
		//Console.WriteLine($"Devices: {string.Join(", ", devices)}");


		// Record a second of data
		CheckALError("Before record");
		short[] recording = new short[(int)(SampleRate * length.TotalSeconds)];

		Console.WriteLine($"Recording...");
		ALC.CaptureStart(_captureDevice);

		int current = 0;
		while (current < recording.Length)
		{
			int samplesAvailable = ALC.GetInteger(_captureDevice, AlcGetInteger.CaptureSamples);
			if (samplesAvailable > 512)
			{
				int samplesToRead = Math.Min(samplesAvailable, recording.Length - current);
				ALC.CaptureSamples(_captureDevice, ref recording[current], samplesToRead);
				current += samplesToRead;
			}
			Thread.Yield();
		}

		ALC.CaptureStop(_captureDevice);

		CheckALError("After record");
		Console.WriteLine($"Recording stopped");
		return new(recording, SampleRate);
	}

	public static void CheckALError(string str)
	{
		ALError error = AL.GetError();
		if (error != ALError.NoError)
		{
			Console.WriteLine($"ALError at '{str}': {AL.GetErrorString(error)}");
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