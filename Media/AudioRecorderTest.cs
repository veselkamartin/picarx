using Microsoft.Extensions.Logging;

namespace SmartCar.Media;

public class AudioRecorderTest
{
	private readonly OpenTkSoundRecorder _soundRecorder;
	private readonly ILogger<AudioRecorderTest> _logger;
	private const int TargetSampleRate = 24000; // Same as ChatGptRealtimeNew

	public AudioRecorderTest(
		OpenTkSoundRecorder soundRecorder,
		ILogger<AudioRecorderTest> logger)
	{
		_soundRecorder = soundRecorder;
		_logger = logger;
	}

	public async Task RecordAndSaveAsync(string outputFilePath, CancellationToken cancellationToken = default)
	{
		const int recordDurationSeconds = 10;
		_logger.LogInformation("Starting audio recording test for {Duration}s to file: {FilePath}", 
			recordDurationSeconds, outputFilePath);

		var allSamples = new List<short>();
		var deltaRecorder = _soundRecorder.CreateDeltaRecorder();

		try
		{
			var startTime = DateTime.UtcNow;
			var endTime = startTime.AddSeconds(recordDurationSeconds);

			while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
			{
				var recordedData = deltaRecorder.ReadAvailableSamples();

				if (recordedData.Data.Length > 0)
				{
					_logger.LogDebug("Read {SampleCount} samples at {SampleRate}Hz", 
						recordedData.Data.Length, recordedData.SampleRate);

					// Resample to target sample rate if needed (same logic as ChatGptRealtimeNew)
					short[] samples = recordedData.Data;
					if (recordedData.SampleRate != TargetSampleRate)
					{
						samples = ResampleAudio(samples, recordedData.SampleRate, TargetSampleRate);
						_logger.LogDebug("Resampled from {FromRate}Hz to {ToRate}Hz, resulting in {SampleCount} samples",
							recordedData.SampleRate, TargetSampleRate, samples.Length);
					}

					allSamples.AddRange(samples);
				}

				await Task.Delay(100, cancellationToken); // Poll every 100ms
			}

			_logger.LogInformation("Recording completed. Total samples: {SampleCount}, Duration: {Duration}s",
				allSamples.Count, allSamples.Count / (double)TargetSampleRate);

			// Save to WAV file
			SaveToWavFile(outputFilePath, allSamples.ToArray(), TargetSampleRate);
			_logger.LogInformation("Audio saved to: {FilePath}", outputFilePath);
		}
		finally
		{
			deltaRecorder.Dispose();
		}
	}

	private void SaveToWavFile(string filePath, short[] audioData, int sampleRate)
	{
		using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		fileStream.AppendWaveData(audioData, sampleRate);
	}

	private static short[] ResampleAudio(short[] input, int inputRate, int outputRate)
	{
		if (inputRate == outputRate) return input;

		var ratio = (double)outputRate / inputRate;
		var outputLength = (int)(input.Length * ratio);
		var output = new short[outputLength];

		for (int i = 0; i < outputLength; i++)
		{
			var srcIndex = i / ratio;
			var srcIndexInt = (int)srcIndex;
			if (srcIndexInt >= input.Length - 1)
			{
				output[i] = input[input.Length - 1];
			}
			else
			{
				// Linear interpolation
				var frac = srcIndex - srcIndexInt;
				output[i] = (short)(input[srcIndexInt] * (1 - frac) + input[srcIndexInt + 1] * frac);
			}
		}

		return output;
	}
}
