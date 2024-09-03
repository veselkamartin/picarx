//using OpenAI;
//using OpenAI.Audio;
//using OpenAI.Chat;
//using SmartCar.Media;
//using System.ClientModel;

//namespace SmartCar.ChatGpt;

//public class ChatGptOld
//{
//	private readonly AudioClient _tts;
//	private readonly ChatClient _chat;
//	private readonly ISoundPlayer _soundPlayer;

//	public ChatGptOld(ApiKeyCredential openAiApiKey, ISoundPlayer soundPlayer)
//	{
//		var client = new OpenAIClient(openAiApiKey);
//		_tts = client.GetAudioClient("tts-1");
//		_chat = client.GetChatClient(model: "gpt-4o");

//		_soundPlayer = soundPlayer;
//	}

//	public async Task StartAsync()
//	{
//		await PlayText("Ahoj, já jsem robotické autíčko.");

//		var asyncChatUpdates = _chat.CompleteChatStreamingAsync(
//		[
//			new SystemChatMessage(ChatGptInstructions.Instructions),
//		]);

//		Console.WriteLine($"[ASSISTANT]:");
//		await foreach (StreamingChatCompletionUpdate chatUpdate in asyncChatUpdates)
//		{
//			foreach (ChatMessageContentPart contentPart in chatUpdate.ContentUpdate)
//			{
//				Console.Write(contentPart.Text);
//			}
//		}
//	}

//	private async Task PlayText(string text)
//	{
//		var outStream = await _tts.GenerateSpeechFromTextAsync(text, GeneratedSpeechVoice.Shimmer,
//			new SpeechGenerationOptions()
//			{
//				ResponseFormat = GeneratedSpeechFormat.Mp3,
//				Speed = 0.8f
//			});
//		var streamData = outStream.Value;
//		await _soundPlayer.PlaySoundOnSpeaker(streamData.ToArray());
//	}
//}
