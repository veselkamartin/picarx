using OpenAI;
using OpenAI.Audio;
using SmartCar.Media;

namespace SmartCar.ChatGpt;

public class ChatGptStt
{
	private readonly AudioClient _stt;

	public ChatGptStt(
		OpenAIClient client
		)
	{
		_stt = client.GetAudioClient("whisper-1");
	}
	public async Task<string> Transcribe(SoundData audio)
	{
		using var stream = new MemoryStream();
		WavHelper.AppendWaveData(stream, audio.Data, audio.SampleRate);
		stream.Position = 0;

		var options = new AudioTranscriptionOptions() { ResponseFormat = AudioTranscriptionFormat.Verbose, Language = "cs", Prompt = "Jeď rovně 100 cm a pak zahni doprava." };

		AudioTranscription transcription = await _stt.TranscribeAudioAsync(stream, "audio.wav", options);
		return transcription.Text;
	}
}