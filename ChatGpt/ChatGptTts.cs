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
	public async Task Play(string text)
	{
		var outStream = await _tts.GenerateSpeechFromTextAsync(text, GeneratedSpeechVoice.Alloy,
			new SpeechGenerationOptions()
			{
				ResponseFormat = GeneratedSpeechFormat.Wav,
				Speed = 1f
			});
		var streamData = outStream.Value;
		await _soundPlayer.PlaySoundOnSpeaker(streamData.ToArray());
	}
}