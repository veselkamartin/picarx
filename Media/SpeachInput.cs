using Microsoft.Extensions.Logging;
using SmartCar.ChatGpt;

namespace SmartCar.Media;

public class SpeachInput
{
	private readonly SoundRecorder _recorder;
	private readonly ISoundPlayer _player;
	private readonly ChatGptStt _stt;
	private readonly ILogger<SpeachInput> _logger;
	private readonly SoundData _listeningSound;
	private readonly SoundData _stopSound;

	public SpeachInput(
		SoundRecorder recorder,
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
	}

	public async Task<string> Read()
	{
		while (true)
		{
			await _player.PlaySoundOnSpeaker(_listeningSound);
			var recordedData = _recorder.Record(TimeSpan.FromSeconds(5));
			await _player.PlaySoundOnSpeaker(_stopSound);

			var max = recordedData.Data.Max();
			var min = recordedData.Data.Min();
			short maxValue = Math.Max(max, Math.Abs(min));
			var amplitude = (float)maxValue / short.MaxValue;
			double dB = 20 * Math.Log10(Math.Abs(amplitude));
			if (dB < -25)
			{
				_logger.LogInformation("Hlasitost {db} dB, opakuji poslech", dB);
				continue;
			}
			else
			{
				_logger.LogInformation("Hlasitost {db} dB, ok", dB);
			}
			var text = await _stt.Transcribe(recordedData);
			_logger.LogInformation(text);

			//await soundPlayer.PlaySoundOnSpeaker(sine, sampleRate);
			//await soundPlayer.PlaySoundOnSpeaker(recordedData, recorder.SampleRate);
			//await soundPlayer.PlaySoundOnSpeaker(sine, sampleRate);
			//var gain = 1 / amplitude;
			//for (int i = 0; i < recordedData.Length; i++)
			//{
			//	recordedData[i]=(short)(recordedData[i] * gain);
			//}
			//await soundPlayer.PlaySoundOnSpeaker(recordedData, recorder.SampleRate);

			return text;
		}
	}
}