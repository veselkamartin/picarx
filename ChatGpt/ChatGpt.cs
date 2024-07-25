using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Files;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicarX.ChatGpt;

public class ChatGpt : ITextPlayer
{
	private readonly AudioClient _tts;
	private readonly FileClient _fileClient;
	// Assistants is a beta API and subject to change; acknowledge its experimental status by suppressing the matching warning.
#pragma warning disable OPENAI001
	private readonly AssistantClient _assistantClient;
	private readonly SoundPlayer _soundPlayer;
	private readonly ChatResponseParser _parser;

	public ChatGpt(ApiKeyCredential openAiApiKey,
		ILogger<ChatResponseParser> parserLogger,
		PicarX.Picarx px)
	{
		var client = new OpenAIClient(openAiApiKey);
		_tts = client.GetAudioClient("tts-1");
		_fileClient = client.GetFileClient();
		_assistantClient = client.GetAssistantClient();

		_soundPlayer = new SoundPlayer();
		_parser = new ChatResponseParser(px, this, parserLogger);
	}

	public async Task StartAsync()
	{
		//This example shows how to use the v2 Assistants API to provide image data to an assistant and then stream the run's response.


		//For this example, we will use both image data from a local file as well as an image located at a URL.For the local data, we upload the file with the Vision upload purpose, which would also allow it to be downloaded and retrieved later.

		//OpenAIFileInfo pictureOfAppleFile = _fileClient.UploadFile(
		//	"picture-of-apple.jpg",
		//	FileUploadPurpose.Vision);
		Uri linkToPictureOfOrange = new("https://platform.openai.com/fictitious-files/picture-of-orange.png");
		//Next, create a new assistant with a vision - capable model like gpt - 4o and a thread with the image information referenced:

		Assistant assistant = _assistantClient.CreateAssistant(
			model: "gpt-4o",
			new AssistantCreationOptions()
			{
				Name = $"Auto {DateTime.Now:yyyy-MM-dd HH:mm}",
				Instructions = ChatGptInstructions.Instructions
			});

		AssistantThread thread = _assistantClient.CreateThread(new ThreadCreationOptions()
		{
			InitialMessages =
			{
				new ThreadInitializationMessage( MessageRole.User,
				[
					"Ahoj",
					//MessageContent.FromImageFileId(pictureOfAppleFile.Id),
					//MessageContent.FromImageUrl(linkToPictureOfOrange),
				]),
			}
		});
		//_assistantClient.CreateMessageAsync(thread.Id,new OpenAI.Assistants.ThreadMessage() )




		//With the assistant and thread prepared, use the CreateRunStreaming method to get an enumerable CollectionResult<StreamingUpdate>. You can then iterate over this collection with foreach.For async calling patterns, use CreateRunStreamingAsync and iterate over the AsyncCollectionResult<StreamingUpdate> with await foreach, instead.Note that streaming variants also exist for CreateThreadAndRunStreaming and SubmitToolOutputsToRunStreaming.

		await RunAsync(assistant, thread);

		while (true)
		{
			var input = await WaitForInput();
			if (string.IsNullOrEmpty(input))
			{
				break;
			}

			await _assistantClient.CreateMessageAsync(thread, MessageRole.User, [MessageContent.FromText(input)]);
			await RunAsync(assistant, thread);
		}
	}

	private Task<string?> WaitForInput()
	{
		Console.Write("Vstup: ");
		var input = Console.ReadLine();
		return Task.FromResult(input);
	}

	private async Task RunAsync(Assistant assistant, AssistantThread thread)
	{
		var streamingUpdates = _assistantClient.CreateRunStreamingAsync(
					thread,
					assistant,
					new RunCreationOptions()
					{
						AdditionalInstructions = "When possible, try to sneak in puns if you're asked to compare things.",
					});
		//Finally, to handle the StreamingUpdates as they arrive, you can use the UpdateKind property on the base StreamingUpdate and / or downcast to a specifically desired update type, like MessageContentUpdate for thread.message.delta events or RequiredActionUpdate for streaming tool calls.

		await foreach (StreamingUpdate streamingUpdate in streamingUpdates)
		{
			if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
			{
				//Console.WriteLine($"--- Run started! ---");
			}
			else if (streamingUpdate is MessageContentUpdate contentUpdate)
			{
				await _parser.Add(contentUpdate.Text);
				//Console.Write(contentUpdate.Text);
				if (contentUpdate.ImageFileId is not null)
				{
					Console.WriteLine($"[Image content file ID: {contentUpdate.ImageFileId}");
				}
			}
		}
		await _parser.Finish();
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
