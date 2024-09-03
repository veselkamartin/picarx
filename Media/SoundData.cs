namespace SmartCar.Media;

public record SoundData(short[] Data, int SampleRate)
{
	public static SoundData FillSine(TimeSpan lenght, float frequency, int sampleRate, float gain = 1f)
	{
		short[] buffer = new short[(int)(sampleRate * lenght.TotalSeconds)];
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = (short)(MathF.Sin(i * frequency * MathF.PI * 2 / sampleRate) * gain * short.MaxValue);
		}
		return new(buffer, sampleRate);
	}
}
