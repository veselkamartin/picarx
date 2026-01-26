using OpenAI;
using OpenAI.Audio;
using SmartCar.Media;

namespace SmartCar.ChatGpt;

public class ChatGptTts : ITextPlayer
{
	private readonly AudioClient _tts;
	private readonly ISoundPlayer _soundPlayer;

	public ChatGptTts(
		OpenAIClient client,
		ISoundPlayer soundPlayer
		)
	{
		_tts = client.GetAudioClient("tts-1");
		_soundPlayer = soundPlayer;
	}
	public async Task Play(string text, CancellationToken ct)
	{
		var outStream = await _tts.GenerateSpeechAsync(text, GeneratedSpeechVoice.Onyx,
			new SpeechGenerationOptions()
			{
				ResponseFormat = GeneratedSpeechFormat.Wav
			}, ct);
		var streamData = outStream.Value;
		await _soundPlayer.PlayWavOnSpeaker(streamData.ToArray(), ct);
	}
}