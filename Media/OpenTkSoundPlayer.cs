using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;

namespace SmartCar.Media;

public class OpenTkSoundPlayer : ISoundPlayer, IDisposable
{
	private bool _disposedValue;
	private ALDevice _device;
	private int _alSource;
	private readonly ALContext _context;
	private readonly ILogger<OpenTkSoundPlayer> _logger;

	public OpenTkSoundPlayer(ILogger<OpenTkSoundPlayer> logger)
	{
		_logger = logger;
		//var devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
		//Console.WriteLine($"Devices: {string.Join(", ", devices)}");
		CheckALError("Start");
		_logger.LogInformation("Listing all devices...");
		var allDevices = ALC.EnumerateAll.GetStringList(GetEnumerateAllContextStringList.AllDevicesSpecifier);
		foreach (var item in allDevices)
		{
			Console.WriteLine("  " + item);
		}

		// Get the default device, then go though all devices and select the AL soft device if it exists.
		string deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
		_logger.LogInformation($"Default device: {deviceName}");
		foreach (var d in allDevices)
		{
			if (d.Contains("Jabra"))
			{
				deviceName = d;
			}
		}
		_logger.LogInformation($"Opening: {deviceName}");

		_device = ALC.OpenDevice(deviceName);
		_context = ALC.CreateContext(_device, (int[])null!);
		ALC.MakeContextCurrent(_context);

		var currentGain = AL.GetListener(ALListenerf.Gain);
		_logger.LogInformation($"Current gain: {currentGain}");

		AL.Listener(ALListenerf.Gain, 2f);
		currentGain = AL.GetListener(ALListenerf.Gain);
		_logger.LogInformation($"Current gain: {currentGain}");

		AL.GenSource(out _alSource);
		AL.Source(_alSource, ALSourcef.Gain, 1f);
		var currentSourceGain = AL.GetSource(_alSource, ALSourcef.Gain);
		_logger.LogInformation($"Current source gain: {currentSourceGain}");
	}
	public async Task PlayWavOnSpeaker(byte[] data)
	{
		//short[] sdata = new short[(int)Math.Ceiling((decimal)data.Length / 2)];
		//Buffer.BlockCopy(data, 0, sdata, 0, data.Length);
		//return PlaySoundOnSpeaker(sdata);
		WavHelper.ReadWav(data, out var L, out var R, out var sampleRate);
		await PlaySoundOnSpeaker(new SoundData(L, sampleRate));
	}

	public async Task PlaySoundOnSpeaker(SoundData data)
	{
		ObjectDisposedException.ThrowIf(_disposedValue, this);

		CheckALError("Before data");
		AL.GenBuffer(out int alBuffer);
		AL.BufferData(alBuffer, ALFormat.Mono16, ref data.Data[0], data.Data.Length * 2, data.SampleRate);
		CheckALError("After data");

		AL.Source(_alSource, ALSourcei.Buffer, alBuffer);

		AL.SourcePlay(_alSource);

		CheckALError("Before Playing");

		while ((ALSourceState)AL.GetSource(_alSource, ALGetSourcei.SourceState) == ALSourceState.Playing)
		{
			//if (AL.SourceLatency.IsExtensionPresent())
			//{
			//	AL.SourceLatency.GetSource(alSource, SourceLatencyVector2d.SecOffsetLatency, out var values);
			//	AL.SourceLatency.GetSource(alSource, SourceLatencyVector2i.SampleOffsetLatency, out var values1, out var values2, out var values3);
			//	Console.WriteLine("Source latency: " + values);
			//	Console.WriteLine($"Source latency 2: {Convert.ToString(values1, 2)}, {values2}; {values3}");
			//	CheckALError(" ");
			//}
			//if (ALC.DeviceClock.IsExtensionPresent(_device))
			//{
			//	long[] clockLatency = new long[2];
			//	ALC.DeviceClock.GetInteger(_device, GetInteger64.DeviceClock, 1, clockLatency);
			//	Console.WriteLine("Clock: " + clockLatency[0] + ", Latency: " + clockLatency[1]);
			//	CheckALError(" ");
			//}

			await Task.Delay(50);
		}

		AL.SourceStop(_alSource);
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
				_logger.LogDebug("Disposing sound player");
				AL.DeleteSource(_alSource);
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

}