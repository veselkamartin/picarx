using OpenTK.Audio.OpenAL;

namespace SmartCar.Media;

public class SoundRecorder
{
	public int SampleRate { get { return 44100; } }
	public short[] Record()
	{
		Console.WriteLine("Hello!");
		//var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
		//Console.WriteLine($"Devices: {string.Join(", ", devices)}");


		CheckALError("Start");



		Console.WriteLine("Available capture devices: ");
		var list = ALC.GetStringList(GetEnumerationStringList.CaptureDeviceSpecifier);
		foreach (var item in list)
		{
			Console.WriteLine("  " + item);
		}
		var captureDeviceName = list.FirstOrDefault(d => d.Contains("Jabra"));


		Console.WriteLine($"Opening for capture: {captureDeviceName}");

		ALCaptureDevice captureDevice = ALC.CaptureOpenDevice(captureDeviceName, SampleRate, ALFormat.Mono16, 1024);
		// Record a second of data
		CheckALError("Before record");
		short[] recording = new short[44100 * 4];

		Console.WriteLine($"Recording...");
		ALC.CaptureStart(captureDevice);

		int current = 0;
		while (current < recording.Length)
		{
			int samplesAvailable = ALC.GetInteger(captureDevice, AlcGetInteger.CaptureSamples);
			if (samplesAvailable > 512)
			{
				int samplesToRead = Math.Min(samplesAvailable, recording.Length - current);
				ALC.CaptureSamples(captureDevice, ref recording[current], samplesToRead);
				current += samplesToRead;
			}
			Thread.Yield();
		}

		ALC.CaptureStop(captureDevice);

		CheckALError("After record");
		Console.WriteLine($"Recording stopped");
		return recording;

	}

	public static void CheckALError(string str)
	{
		ALError error = AL.GetError();
		if (error != ALError.NoError)
		{
			Console.WriteLine($"ALError at '{str}': {AL.GetErrorString(error)}");
		}
	}


}