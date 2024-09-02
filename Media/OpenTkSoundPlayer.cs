using OpenTK.Audio.OpenAL;

namespace SmartCar.Media;

public class OpenTkSoundPlayer : ISoundPlayer, IDisposable
{
	private bool _disposedValue;
	private ALDevice _device;
	private readonly ALContext _context;

	public OpenTkSoundPlayer()
	{
		Console.WriteLine("Hello!");
		//var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
		//Console.WriteLine($"Devices: {string.Join(", ", devices)}");
		Console.WriteLine("Listing all devices...");
		var allDevices = ALC.EnumerateAll.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier);
		foreach (var item in allDevices)
		{
			Console.WriteLine("  " + item);
		}

		// Get the default device, then go though all devices and select the AL soft device if it exists.
		string deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
		Console.WriteLine($"Default device: {deviceName}");
		foreach (var d in allDevices)
		{
			if (d.Contains("Jabra"))
			{
				deviceName = d;
			}
		}
		Console.WriteLine($"Opening: {deviceName}");

		_device = ALC.OpenDevice(deviceName);
		_context = ALC.CreateContext(_device, (int[])null!);
		ALC.MakeContextCurrent(_context);
	}
	public async Task PlaySoundOnSpeaker(byte[] data)
	{
		//short[] sdata = new short[(int)Math.Ceiling((decimal)data.Length / 2)];
		//Buffer.BlockCopy(data, 0, sdata, 0, data.Length);
		//return PlaySoundOnSpeaker(sdata);
		ReadWav(data, out var L, out var R, out var sampleRate);
		await PlaySoundOnSpeaker(L, sampleRate);

	}
	static void ReadWav(byte[] data, out short[] L, out short[]? R, out int sampleRate)
	{


		using var fs = new MemoryStream(data);
		var reader = new BinaryReader(fs);

		// chunk 0
		int chunkID = reader.ReadInt32();
		int fileSize = reader.ReadInt32();
		int riffType = reader.ReadInt32();


		// chunk 1
		int fmtID = reader.ReadInt32();
		int fmtSize = reader.ReadInt32(); // bytes for this chunk (expect 16 or 18)

		// 16 bytes coming...
		int fmtCode = reader.ReadInt16();
		int channels = reader.ReadInt16();
		sampleRate = reader.ReadInt32();
		var byteRate = reader.ReadInt32();
		int fmtBlockAlign = reader.ReadInt16();
		int bitDepth = reader.ReadInt16();

		if (fmtSize == 18)
		{
			// Read any extra values
			int fmtExtraSize = reader.ReadInt16();
			reader.ReadBytes(fmtExtraSize);
		}

		// chunk 2
		int dataID = reader.ReadInt32();
		int bytes = reader.ReadInt32();
		if (bytes == -1) bytes = (int)(fs.Length - fs.Position);
		// DATA!
		byte[] byteArray = reader.ReadBytes(bytes);

		int bytesForSamp = bitDepth / 8;
		int nValues = bytes / bytesForSamp;


		short[]? asShort = null;
		switch (bitDepth)
		{
			case 64:
				double[] asDouble = new double[nValues];
				Buffer.BlockCopy(byteArray, 0, asDouble, 0, bytes);
				asShort = Array.ConvertAll(asDouble, e => (short)(e * (short.MaxValue + 1)));
				break;
			case 32:
				var asFloat = new float[nValues];
				Buffer.BlockCopy(byteArray, 0, asFloat, 0, bytes);
				asShort = Array.ConvertAll(asFloat, e => (short)(e * (short.MaxValue + 1)));
				break;
			case 16:
				asShort = new short[nValues];
				Buffer.BlockCopy(byteArray, 0, asShort, 0, bytes);
				break;
			default:
				throw new Exception($"Unsupported bit depth {bitDepth}");
		}

		switch (channels)
		{
			case 1:
				L = asShort;
				R = null;
				break;
			case 2:
				// de-interleave
				int nSamps = nValues / 2;
				L = new short[nSamps];
				R = new short[nSamps];
				for (int s = 0, v = 0; s < nSamps; s++)
				{
					L[s] = asShort[v++];
					R[s] = asShort[v++];
				}
				break;
			default:
				throw new Exception($"Unsupported channel number {channels}");
		}

	}
	public Task PlaySoundOnSpeaker(short[] data, int sampleRate)
	{
		CheckALError("Start");

		// Playback the recorded data
		CheckALError("Before data");
		AL.GenBuffer(out int alBuffer);
		// short[] sine = new short[44100 * 1];
		// FillSine(sine, 4400, 44100);
		// FillSine(recording, 440, 44100);
		AL.BufferData(alBuffer, ALFormat.Mono16, ref data[0], data.Length * 2, sampleRate);
		CheckALError("After data");

		var currentGain = AL.GetListener(ALListenerf.Gain);
		Console.WriteLine($"Current gain: {currentGain}");

		AL.Listener(ALListenerf.Gain, 2f);
		currentGain = AL.GetListener(ALListenerf.Gain);
		Console.WriteLine($"Current gain: {currentGain}");

		AL.GenSource(out int alSource);
		AL.Source(alSource, ALSourcef.Gain, 1f);
		AL.Source(alSource, ALSourcei.Buffer, alBuffer);
		var currentSourceGain = AL.GetSource(alSource, ALSourcef.Gain);
		Console.WriteLine($"Current source gain: {currentSourceGain}");

		AL.SourcePlay(alSource);

		CheckALError("Before Playing");

		while ((ALSourceState)AL.GetSource(alSource, ALGetSourcei.SourceState) == ALSourceState.Playing)
		{
			if (AL.SourceLatency.IsExtensionPresent())
			{
				AL.SourceLatency.GetSource(alSource, SourceLatencyVector2d.SecOffsetLatency, out var values);
				AL.SourceLatency.GetSource(alSource, SourceLatencyVector2i.SampleOffsetLatency, out var values1, out var values2, out var values3);
				Console.WriteLine("Source latency: " + values);
				Console.WriteLine($"Source latency 2: {Convert.ToString(values1, 2)}, {values2}; {values3}");
				CheckALError(" ");
			}
			if (ALC.DeviceClock.IsExtensionPresent(_device))
			{
				long[] clockLatency = new long[2];
				ALC.DeviceClock.GetInteger(_device, GetInteger64.DeviceClock, 1, clockLatency);
				Console.WriteLine("Clock: " + clockLatency[0] + ", Latency: " + clockLatency[1]);
				CheckALError(" ");
			}

			Thread.Sleep(50);
		}

		AL.SourceStop(alSource);


		return Task.CompletedTask;
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
				Console.WriteLine("Goodbye!");

				ALC.MakeContextCurrent(ALContext.Null);
				ALC.DestroyContext(_context);
				ALC.CloseDevice(_device);
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

	internal async Task PlaySoundOnSpeaker(short[] recordedData, object sampleRate)
	{
		throw new NotImplementedException();
	}
}