using OpenAI;
using OpenAI.Audio;

namespace PicarX.ChatGpt;

public class ChatGptTts : ITextPlayer
{
	private readonly AudioClient _tts;
	private readonly SoundPlayer _soundPlayer;

	public ChatGptTts(
		OpenAIClient client
		)
	{
		_tts = client.GetAudioClient("tts-1");

		_soundPlayer = new SoundPlayer();
	}
	public async Task Play(string text)
	{
		var outStream = await _tts.GenerateSpeechFromTextAsync(text, GeneratedSpeechVoice.Shimmer,
			new SpeechGenerationOptions()
			{
				ResponseFormat = GeneratedSpeechFormat.Mp3,
				Speed = 0.8f
			});
		var streamData = outStream.Value;
		await _soundPlayer.PlaySoundOnSpeaker(streamData);
	}
}