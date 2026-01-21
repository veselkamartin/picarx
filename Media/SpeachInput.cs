using Microsoft.Extensions.Logging;
using SmartCar.ChatGpt;

namespace SmartCar.Media;

public class SpeachInput : ISpeachInput
{
	private readonly OpenTkSoundRecorder _recorder;
	private readonly ISoundPlayer _player;
	private readonly ChatGptStt _stt;
	private readonly ILogger<SpeachInput> _logger;
	private readonly SoundData _listeningSound;
	private readonly SoundData _stopSound;
	private const int SilenceTreshold = -25; //silence threshold in dB
	private int _currentSampleRate;

	public SpeachInput(
		OpenTkSoundRecorder recorder,
		ISoundPlayer player,
		ChatGptStt stt,
		ILogger<SpeachInput> logger
		)
	{
		_recorder = recorder;
		_player = player;
		_stt = stt;
		_logger = logger;
		_listeningSound = SoundData.FillSine(TimeSpan.FromMilliseconds(500), frequency: 400, sampleRate: 44100, gain: 0.5f);
		_stopSound = SoundData.FillSine(TimeSpan.FromMilliseconds(100), frequency: 800, sampleRate: 44100, gain: 0.5f);
		_currentSampleRate = _recorder.SampleRate;
	}

	public async Task<string> Read(CancellationToken stoppingToken)
	{
		_currentSampleRate = _recorder.SampleRate;
		await _player.PlaySoundOnSpeaker(_listeningSound);
		SoundData recordedData;
		while (true)
		{
			recordedData = _recorder.Record(TimeSpan.FromSeconds(50), (audioData) =>
			{
				return stoppingToken.IsCancellationRequested || StopConditionInfo(audioData).StopDetected;
			});
			stoppingToken.ThrowIfCancellationRequested();
			var recordingInfo = StopConditionInfo(recordedData.Data);
			_logger.LogInformation("Recording min {min:0}, max {max:0}, sound length {soundLength:0.0}s", recordingInfo.MinLevel, recordingInfo.MaxLevel, recordingInfo.SoundLenght);
			if (recordingInfo.StopDetected)
			{
				break;
			}
			_logger.LogInformation("Opakuji poslech");
		}
		await _player.PlaySoundOnSpeaker(_stopSound);
		var recordedDataTrimmed = TrimSilence(recordedData);

		var text = await _stt.Transcribe(recordedDataTrimmed);
		_logger.LogInformation(text);
		//await _player.PlaySoundOnSpeaker(recordedDataTrimmed);

		//var gain = 1 / amplitude;
		//for (int i = 0; i < recordedData.Length; i++)
		//{
		//	recordedData[i]=(short)(recordedData[i] * gain);
		//}
		//await soundPlayer.PlaySoundOnSpeaker(recordedData, recorder.SampleRate);

		return text;
	}

	SoundData TrimSilence(SoundData data)
	{
		int? firstSound = null;
		int? lastSound = null;
		for (int i = 0; i < data.Data.Length; i++)
		{
			var isSound = GetAmplitude(data.Data[i]) > SilenceTreshold;
			if (isSound)
			{
				if (!firstSound.HasValue) firstSound = i;
				lastSound = i;
			}
		}
		if (!firstSound.HasValue || !lastSound.HasValue) return data;
		firstSound = Math.Max(firstSound.Value - (int)(data.SampleRate * 0.5), 0);
		lastSound = Math.Min(lastSound.Value + (int)(data.SampleRate * 0.5), data.Data.Length - 1);
		return new SoundData(
			data.Data
			.AsSpan()
			.Slice(firstSound.Value, lastSound.Value - firstSound.Value + 1)
			.ToArray(),
			data.SampleRate);
	}

	private static double GetAmplitude(Span<short> recordedData)
	{
		short min = 0, max = 0;
		foreach (var value in recordedData)
		{
			if (value < min) min = value;
			if (value > max) max = value;
		}
		short maxValue = min == 0 ? max : Math.Max(max, Math.Abs(min));
		return GetAmplitude(maxValue);
	}

	private static double GetAmplitude(short maxValue)
	{
		var amplitude = (float)Math.Abs(maxValue) / short.MaxValue;
		double dB = 20 * Math.Log10(amplitude);
		return dB;
	}

	private (bool StopDetected, double SoundLenght, double MinLevel, double MaxLevel) StopConditionInfo(Span<short> audioData)
	{
		if (_currentSampleRate == 0) throw new Exception("Sample rate not set");
		const double minimumEndingSilenceLenghtInS = 1; //1s
		int minimumEndingSilenceLenght = (int)(minimumEndingSilenceLenghtInS * _currentSampleRate);
		var maxSoundLevel = GetAmplitude(audioData); //recording maximum sound level in dB

		var firstNonSilentValue = audioData.IndexOfAnyExceptInRange((short)-2, (short)2);

		double minSoundLevel = 0;//minimal sound level in recording mesured in chunks of 0.2s
		var soundNotSilentSeconds = 0.0;
		if (firstNonSilentValue >= 0)
		{
			const double chunkLengthSec = 0.2;
			int chunkLenght = (int)(chunkLengthSec * _currentSampleRate);
			if (chunkLenght == 0) throw new Exception($"Chunk length zero, sample rate {_currentSampleRate}");
			//iterates through recording in chunks, ending chunk must also be correct lenght
			for (int chunkStart = firstNonSilentValue; chunkStart < audioData.Length - chunkLenght; chunkStart = chunkStart + chunkLenght)
			{
				var chunk = audioData.Slice(chunkStart, chunkLenght);
				var chunkLevel = GetAmplitude(chunk);
				if (chunkLevel < minSoundLevel) minSoundLevel = chunkLevel;
				if (chunkLevel > SilenceTreshold) soundNotSilentSeconds += chunkLengthSec;
			}
		}
		var containsAnySound = maxSoundLevel >= SilenceTreshold;
		bool endContainsSound = false;
		if (audioData.Length > minimumEndingSilenceLenght)
		{
			var endData = audioData.Slice(audioData.Length - minimumEndingSilenceLenght, minimumEndingSilenceLenght);
			var endSoundLevel = GetAmplitude(endData);
			endContainsSound = endSoundLevel >= SilenceTreshold;
		}

		var stopDetected =
			audioData.Length > minimumEndingSilenceLenght &&
			containsAnySound &&
			soundNotSilentSeconds >= 0.7 &&
			!endContainsSound;

		return (stopDetected, soundNotSilentSeconds, minSoundLevel, maxSoundLevel);
	}
}
