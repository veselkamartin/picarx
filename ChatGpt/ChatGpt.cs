﻿using OpenAI;
using OpenAI.Assistants;
using OpenAI.Files;
using SmartCar.Media;
using SmartCar.PicarX;

namespace SmartCar.ChatGpt;
public class ChatGpt
{
	private readonly FileClient _fileClient;
	// Assistants is a beta API and subject to change; acknowledge its experimental status by suppressing the matching warning.
#pragma warning disable OPENAI001
	private readonly AssistantClient _assistantClient;
	private readonly ChatResponseParser _parser;
	private readonly ILogger<ChatGpt> _logger;
	private readonly ICamera _camera;
	private readonly ISpeachInput _speachInput;
	private readonly StateProvider _stateProvider;

	public ChatGpt(
		OpenAIClient client,
		ILogger<ChatGpt> logger,
		ChatResponseParser parser,
		ICamera camera,
		ISpeachInput speachInput,
		StateProvider stateProvider
		)
	{
		_fileClient = client.GetFileClient();
		_assistantClient = client.GetAssistantClient();

		_parser = parser;
		_logger = logger;
		_camera = camera;
		_speachInput = speachInput;
		_stateProvider = stateProvider;
	}

	public async Task StartAsync(CancellationToken stoppingToken)
	{
		Assistant assistant = _assistantClient.CreateAssistant(
			model: "gpt-4o",//"gpt-4o-mini",
			new AssistantCreationOptions()
			{
				Name = $"Auto {DateTime.Now:yyyy-MM-dd HH:mm}",
				Instructions = ChatGptInstructions.Instructions2
			});

		var picture1 = await _camera.GetPictureAsJpeg();
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
		await RunAsync(assistant, thread, true, stoppingToken);

		bool waitForInput = true;
		while (!stoppingToken.IsCancellationRequested)
		{
			string message;
			if (waitForInput)
			{
				var input = await WaitForInput(stoppingToken);
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
			var picture = await _camera.GetPictureAsJpeg();
			OpenAIFileInfo pictureUploaded = _fileClient.UploadFile(BinaryData.FromBytes(picture), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}.jpg", FileUploadPurpose.Vision);
			var state = await _stateProvider.GetState();
			_logger.LogInformation("State: {message}", state);
			_logger.LogInformation("Input: {message}", message);
			await _assistantClient.CreateMessageAsync(thread, MessageRole.User,
				[
					MessageContent.FromText(/*">"+state + "\n"+*/ message),
					MessageContent.FromImageFileId(pictureUploaded.Id)
				]);
			waitForInput = !await RunAsync(assistant, thread, false, stoppingToken);
		}
	}

	private async Task<string?> WaitForInput(CancellationToken stoppingToken)
	{
		return await _speachInput.Read(stoppingToken);
	}

	private async Task<bool> RunAsync(Assistant assistant, AssistantThread thread, bool first, CancellationToken stoppingToken)
	{
		_logger.LogInformation("Thinking");

		var streamingUpdates = _assistantClient.CreateRunStreamingAsync(
					thread,
					assistant,
					new RunCreationOptions()
					{
						//MaxCompletionTokens = first ? 51: null,
						//AdditionalInstructions = "When possible, try to sneak in puns if you're asked to compare things.",
					});
		//Finally, to handle the StreamingUpdates as they arrive, you can use the UpdateKind property on the base StreamingUpdate and / or downcast to a specifically desired update type, like MessageContentUpdate for thread.message.delta events or RequiredActionUpdate for streaming tool calls.

		try
		{
			await foreach (StreamingUpdate streamingUpdate in streamingUpdates)
			{
				stoppingToken.ThrowIfCancellationRequested();
				if (streamingUpdate is MessageContentUpdate contentUpdate)
				{
					await _parser.Add(contentUpdate.Text);
					//Console.Write(contentUpdate.Text);
					if (contentUpdate.ImageFileId is not null)
					{
						_logger.LogInformation($"Image content file ID: {contentUpdate.ImageFileId}");
					}
				}
				else if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunQueued)
				{
					_logger.LogInformation("Queued");
				}
				else
				{
					_logger.LogDebug($"{streamingUpdate.UpdateKind} {streamingUpdate.GetType().Name}");
				}
			}
		}
		catch (OperationCanceledException) { throw; }
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in ChatGpt");
		}
		return await _parser.Finish();
	}
}
