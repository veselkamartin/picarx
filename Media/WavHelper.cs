using System.Text;

namespace SmartCar.Media;

public static class WavHelper
{
	public static void AppendWaveData<T>(this T stream, short[] data, int sampleRate = 44100)
	   where T : Stream
	{
		// Copy to bytes
		var result = new byte[data.Length * sizeof(short)];
		Buffer.BlockCopy(data, 0, result, 0, result.Length);

		var writer = new BinaryWriter(stream);

		writer.Write(Encoding.ASCII.GetBytes("RIFF")); //RIFF marker. Marks the file as a riff file. Characters are each 1 byte long. 
		int dataLength = result.Length;
		writer.Write(dataLength + 36); //file-size (equals file-size - 8). Size of the overall file - 8 bytes, in bytes (32-bit integer). Typically, you'd fill this in after creation.
		writer.Write(Encoding.ASCII.GetBytes("WAVE")); //File Type Header. For our purposes, it always equals "WAVE".
		writer.Write(Encoding.ASCII.GetBytes("fmt ")); //Mark the format section. Format chunk marker. Includes trailing null. 
		writer.Write(16); //Length of format data.  Always 16. 
		writer.Write((short)1); //Type of format (1 is PCM, other number means compression) . 2 byte integer. Wave type PCM
		writer.Write((short)1); //Number of Channels - 1 byte integer
		writer.Write(sampleRate); //Sample Rate - 32 byte integer. Sample Rate = Number of Samples per second, or Hertz.
		writer.Write(sampleRate * 2 * 1); // sampleRate * bytesPerSample * number of channels, here 16000*2*1.
		writer.Write((short)(1 * 2)); //channels * bytesPerSample, here 1 * 2  // Bytes Per Sample: 1=8 bit Mono,  2 = 8 bit Stereo or 16 bit Mono, 4 = 16 bit Stereo
		writer.Write((short)16); //Bits per sample (BitsPerSample * Channels)
		writer.Write(Encoding.ASCII.GetBytes("data")); //"data" chunk header. Marks the beginning of the data section.    
		writer.Write(result.Length); //Size of the data section. data-size (equals file-size - 44). or NumSamples * NumChannels * bytesPerSample ??        


		// write to stream
		writer.Write(result, 0, result.Length);
	}

	public static void ReadWav(byte[] data, out short[] l, out short[]? r, out int sampleRate)
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
		//if (bytes == -1)
		bytes = (int)(fs.Length - fs.Position);
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
				l = asShort;
				r = null;
				break;
			case 2:
				// de-interleave
				int nSamps = nValues / 2;
				l = new short[nSamps];
				r = new short[nSamps];
				for (int s = 0, v = 0; s < nSamps; s++)
				{
					l[s] = asShort[v++];
					r[s] = asShort[v++];
				}
				break;
			default:
				throw new Exception($"Unsupported channel number {channels}");
		}

	}
}