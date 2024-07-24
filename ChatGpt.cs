using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicarX
{
	public class ChatGpt
	{
		private readonly AudioClient _tts;
		private readonly ChatClient _chat;

		public ChatGpt(ApiKeyCredential openAiApiKey)
		{
			var client = new OpenAIClient(openAiApiKey);
			_tts = client.GetAudioClient("tts-1");
			_chat = client.GetChatClient(model: "gpt-4o");
		}

		public async Task StartAsync()
		{
			await PlayText("Ahoj, já jsem robotické autíčko.");

			var asyncChatUpdates = _chat.CompleteChatStreamingAsync(
			[
				new UserChatMessage("Řekni 'Ahoj, já jsem robotické autíčko.'"),
			]);

			Console.WriteLine($"[ASSISTANT]:");
			await foreach (StreamingChatCompletionUpdate chatUpdate in asyncChatUpdates)
			{
				foreach (ChatMessageContentPart contentPart in chatUpdate.ContentUpdate)
				{
					Console.Write(contentPart.Text);
				}
			}
		}

		private async Task PlayText(string text)
		{
			var outStream = await _tts.GenerateSpeechFromTextAsync(text, GeneratedSpeechVoice.Shimmer,
				new SpeechGenerationOptions()
				{
					ResponseFormat = GeneratedSpeechFormat.Mp3,
					Speed = 0.8f
				});
			var stream = outStream.Value.ToStream();

			//using var provider = new Mp3FileReader(stream);
			//using var outputDevice = new NAudio.Wave. WaveOutEvent();
			//outputDevice.Init(provider);
			//outputDevice.Play();
			//while (outputDevice.PlaybackState == PlaybackState.Playing)
			//{
			//    await Task.Delay(100);
			//}
			var tempPath = Path.GetTempPath();
			var tempFile = Path.Join(tempPath, Guid.NewGuid().ToString() + ".mp3");
			await File.WriteAllBytesAsync(tempFile, outStream.Value.ToArray());
			var player = new NetCoreAudio.Player();
			await player.Play(tempFile);
			while (player.Playing)
			{
				await Task.Delay(100);
			}
		}
	}
}
