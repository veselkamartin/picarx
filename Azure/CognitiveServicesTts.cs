using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using SmartCar.ChatGpt;
using SmartCar.Media;

namespace SmartCar.Azure;

public class CognitiveServicesTts : ITextPlayer
{
	private readonly string _speachKey;
	private readonly ISoundPlayer _soundPlayer;
	private readonly ILogger<CognitiveServicesTts> _logger;

	public CognitiveServicesTts(
		string speachKey,
		ISoundPlayer soundPlayer,
		ILogger<CognitiveServicesTts> logger)
	{
		_speachKey = speachKey;
		_soundPlayer = soundPlayer;
		_logger = logger;
	}

	public async Task Play(string text)
	{
		string subscriptionRegion = "westeurope";

		var config = SpeechConfig.FromSubscription(_speachKey, subscriptionRegion);
		config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);

		config.SpeechSynthesisVoiceName = "cs-CZ-AntoninNeural";

		// Creates a speech synthesizer with a null output stream.
		// This means the audio output data will not be written to any stream.
		// You can just get the audio from the result.
		using var synthesizer = new SpeechSynthesizer(config, null!);
		using var result = await synthesizer.SpeakTextAsync(text);
		if (result.Reason == ResultReason.Canceled)
		{
			var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
			_logger.LogError("CANCELED: Reason={cancellationReason}", cancellation.Reason);
			if (cancellation.Reason == CancellationReason.Error)
			{
				throw new Exception($"Tts error, ErrorCode={cancellation.ErrorCode}, ErrorDetails=[{cancellation.ErrorDetails}]");
			}
		}
		//Transform byte array to short array
		var asShort = new short[result.AudioData.Length / 2];
		Buffer.BlockCopy(result.AudioData, 0, asShort, 0, result.AudioData.Length);
		await _soundPlayer.PlaySoundOnSpeaker(new SoundData(asShort, 24000));
	}
}