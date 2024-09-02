using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;
using SmartCar.Media;

namespace SmartCar.ChatGpt;

public class ChatGpt
{
	private readonly FileClient _fileClient;
	// Assistants is a beta API and subject to change; acknowledge its experimental status by suppressing the matching warning.
#pragma warning disable OPENAI001
	private readonly AssistantClient _assistantClient;
	private readonly ChatResponseParser _parser;
	private readonly ILogger<ChatGpt> _logger;
	private readonly Camera _camera;

	public ChatGpt(
		OpenAIClient client,
		ILogger<ChatGpt> logger,
		ChatResponseParser parser,
		Camera camera
		)
	{
		_fileClient = client.GetFileClient();
		_assistantClient = client.GetAssistantClient();

		_parser = parser;
		_logger = logger;
		_camera = camera;
	}

	public async Task StartAsync()
	{
		Assistant assistant = _assistantClient.CreateAssistant(
			model: "gpt-4o",
			new AssistantCreationOptions()
			{
				Name = $"Auto {DateTime.Now:yyyy-MM-dd HH:mm}",
				Instructions = ChatGptInstructions.Instructions
			});

		var picture1 = _camera.GetPictureAsJpeg();
		OpenAIFileInfo pictureUploaded1 = _fileClient.UploadFile(BinaryData.FromBytes(picture1), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}.jpg", FileUploadPurpose.Vision);

		AssistantThread thread = _assistantClient.CreateThread(new ThreadCreationOptions()
		{
			InitialMessages =
			{
				new ThreadInitializationMessage( MessageRole.User,
				[
					"Ahoj",
					MessageContent.FromImageFileId(pictureUploaded1.Id),
					//MessageContent.FromImageUrl(linkToPictureOfOrange),
				]),
			}
		});

		//With the assistant and thread prepared, use the CreateRunStreaming method to get an enumerable CollectionResult<StreamingUpdate>. You can then iterate over this collection with foreach.For async calling patterns, use CreateRunStreamingAsync and iterate over the AsyncCollectionResult<StreamingUpdate> with await foreach, instead.Note that streaming variants also exist for CreateThreadAndRunStreaming and SubmitToolOutputsToRunStreaming.
		await RunAsync(assistant, thread);

		bool waitForInput = true;
		while (true)
		{
			string message;
			if (waitForInput)
			{
				var input = await WaitForInput();
				if (string.IsNullOrEmpty(input))
				{
					break;
				}
				message = input;
			}
			else
			{
				message = "Pokračuj";
			}
			var picture = _camera.GetPictureAsJpeg();
			OpenAIFileInfo pictureUploaded = _fileClient.UploadFile(BinaryData.FromBytes(picture), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}.jpg", FileUploadPurpose.Vision);

			await _assistantClient.CreateMessageAsync(thread, MessageRole.User, [MessageContent.FromText(message), MessageContent.FromImageFileId(pictureUploaded.Id)]);
			waitForInput = !await RunAsync(assistant, thread);
		}
	}

	private Task<string?> WaitForInput()
	{
		Console.Write("Vstup: ");
		var input = Console.ReadLine();
		return Task.FromResult(input);
	}

	private async Task<bool> RunAsync(Assistant assistant, AssistantThread thread)
	{
		_logger.LogInformation("Thinking");

		var streamingUpdates = _assistantClient.CreateRunStreamingAsync(
					thread,
					assistant,
					new RunCreationOptions()
					{
						//AdditionalInstructions = "When possible, try to sneak in puns if you're asked to compare things.",
					});
		//Finally, to handle the StreamingUpdates as they arrive, you can use the UpdateKind property on the base StreamingUpdate and / or downcast to a specifically desired update type, like MessageContentUpdate for thread.message.delta events or RequiredActionUpdate for streaming tool calls.

		await foreach (StreamingUpdate streamingUpdate in streamingUpdates)
		{
			if (streamingUpdate is MessageContentUpdate contentUpdate)
			{
				await _parser.Add(contentUpdate.Text);
				//Console.Write(contentUpdate.Text);
				if (contentUpdate.ImageFileId is not null)
				{
					Console.WriteLine($"Image content file ID: {contentUpdate.ImageFileId}");
				}
			}
			else
			{
				_logger.LogDebug($"{streamingUpdate.UpdateKind}");
			}
		}
		return await _parser.Finish();
	}
}
