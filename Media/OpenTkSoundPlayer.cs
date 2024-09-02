using OpenTK.Audio.OpenAL;

namespace SmartCar.Media
{
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
		public Task PlaySoundOnSpeaker(byte[] data)
		{
			short[] sdata = new short[(int)Math.Ceiling((decimal)data.Length / 2)];
			Buffer.BlockCopy(data, 0, sdata, 0, data.Length);
			return PlaySoundOnSpeaker(sdata);
		}
		public Task PlaySoundOnSpeaker(short[] data)
		{
			CheckALError("Start");

			// Playback the recorded data
			CheckALError("Before data");
			AL.GenBuffer(out int alBuffer);
			// short[] sine = new short[44100 * 1];
			// FillSine(sine, 4400, 44100);
			// FillSine(recording, 440, 44100);
			AL.BufferData(alBuffer, ALFormat.Mono16, ref data[0], data.Length * 2, 44100);
			CheckALError("After data");

			AL.Listener(ALListenerf.Gain, 0.1f);

			AL.GenSource(out int alSource);
			AL.Source(alSource, ALSourcef.Gain, 1f);
			AL.Source(alSource, ALSourcei.Buffer, alBuffer);

			AL.SourcePlay(alSource);

			Console.WriteLine("Before Playing: " + AL.GetErrorString(AL.GetError()));

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
	}
}