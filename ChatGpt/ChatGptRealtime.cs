//using OpenAI;
//using OpenAI.Assistants;
//using OpenAI.Files;
//using OpenAI.RealtimeConversation;
//using SmartCar.Media;
//using SmartCar.PicarX;
//using System.ClientModel.Primitives;

//namespace SmartCar.ChatGpt;
//public class ChatGptRealtime
//{
//	private readonly OpenAIFileClient _fileClient;
//#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
//	private readonly RealtimeConversationClient _assistantClient;
//	private readonly ChatResponseParser _parser;
//	private readonly ILogger<ChatGpt> _logger;
//	private readonly ICamera _camera;
//	private readonly ISpeachInput _speachInput;
//	private readonly StateProvider _stateProvider;

//	public ChatGptRealtime(
//		OpenAIClient client,
//		ILogger<ChatGpt> logger,
//		ChatResponseParser parser,
//		ICamera camera,
//		ISpeachInput speachInput,
//		StateProvider stateProvider
//		)
//	{
//		_fileClient = client.GetOpenAIFileClient();
//		_assistantClient = client.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-12-17");

//		_parser = parser;
//		_logger = logger;
//		_camera = camera;
//		_speachInput = speachInput;
//		_stateProvider = stateProvider;
//	}

//	public async Task StartAsync(CancellationToken stoppingToken)
//	{
//		var session= await _assistantClient.StartConversationSessionAsync(stoppingToken);
//		ConversationSessionOptions sessionOptions = new()
//		{
//			Instructions = ChatGptInstructions.Instructions2,
//			TurnDetectionOptions = ConversationTurnDetectionOptions.CreateDisabledTurnDetectionOptions(),
//			OutputAudioFormat = ConversationAudioFormat.Pcm16,
//			MaxOutputTokens = 2048,
//		};

//		await session.ConfigureSessionAsync(sessionOptions, stoppingToken);
//		ConversationResponseOptions responseOverrideOptions = new()
//		{
//			ContentModalities = ConversationContentModalities.Text,
//		};

//		var picture1 = await _camera.GetPictureAsJpeg();
//		var pictureUploaded1 = await _fileClient.UploadFileAsync(BinaryData.FromBytes(picture1), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}.jpg", FileUploadPurpose.Vision);

//		//AssistantThread thread = _assistantClient.CreateThread(new ThreadCreationOptions()
//		//{
//		//	InitialMessages =
//		//	{
//		//		new ThreadInitializationMessage( MessageRole.User,
//		//		[
//		//			"Ahoj",
//		//			//MessageContent.FromImageUrl(linkToPictureOfOrange),
//		//		]),
//		//	}
//		//});


//		await session.AddItemAsync(
//			ConversationItem.CreateUserMessage(["Ahoj",ConversationContentPart.
//					MessageContent.FromImageFileId(pictureUploaded1.Value.Id)
			
//			]),
//			stoppingToken);
//		await session.StartResponseAsync(responseOverrideOptions, stoppingToken);

//		List<ConversationUpdate> receivedUpdates = [];

//		await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync(stoppingToken))
//		{
//			receivedUpdates.Add(update);

//			if (update is ConversationErrorUpdate errorUpdate)
//			{
//				Assert.That(errorUpdate.Kind, Is.EqualTo(ConversationUpdateKind.Error));
//				Assert.Fail($"Error: {ModelReaderWriter.Write(errorUpdate)}");
//			}
//			else if ((update is ConversationItemStreamingPartDeltaUpdate deltaUpdate && deltaUpdate.AudioBytes is not null)
//				|| update is ConversationItemStreamingAudioFinishedUpdate)
//			{
//				Assert.Fail($"Audio content streaming unexpected after configuring response-level text-only modalities");
//			}
//			else if (update is ConversationSessionConfiguredUpdate sessionConfiguredUpdate)
//			{
//				Assert.That(sessionConfiguredUpdate.OutputAudioFormat == sessionOptions.OutputAudioFormat);
//				Assert.That(sessionConfiguredUpdate.TurnDetectionOptions.Kind, Is.EqualTo(ConversationTurnDetectionKind.Disabled));
//				Assert.That(sessionConfiguredUpdate.MaxOutputTokens.NumericValue, Is.EqualTo(sessionOptions.MaxOutputTokens.NumericValue));
//			}
//			else if (update is ConversationResponseFinishedUpdate turnFinishedUpdate)
//			{
//				break;
//			}
//		}

//		List<T> GetReceivedUpdates<T>() where T : ConversationUpdate
//			=> receivedUpdates.Select(update => update as T)
//				.Where(update => update is not null)
//				.ToList();
//		//model: "gpt-4o",//"gpt-4o-mini",
//		//new AssistantCreationOptions()
//		//{
//		//	Name = $"Auto {DateTime.Now:yyyy-MM-dd HH:mm}",
//		//	Instructions = ChatGptInstructions.Instructions2
//		//});


//		//With the assistant and thread prepared, use the CreateRunStreaming method to get an enumerable CollectionResult<StreamingUpdate>. You can then iterate over this collection with foreach.For async calling patterns, use CreateRunStreamingAsync and iterate over the AsyncCollectionResult<StreamingUpdate> with await foreach, instead.Note that streaming variants also exist for CreateThreadAndRunStreaming and SubmitToolOutputsToRunStreaming.
//		await RunAsync(assistant, thread, true, stoppingToken);

//		bool waitForInput = true;
//		while (!stoppingToken.IsCancellationRequested)
//		{
//			string message;
//			if (waitForInput)
//			{
//				var input = await WaitForInput(stoppingToken);
//				if (string.IsNullOrEmpty(input))
//				{
//					break;
//				}
//				message = input;
//			}
//			else
//			{
//				message = "Pokračuj";
//			}
//			var picture = await _camera.GetPictureAsJpeg();
//			var pictureUploaded = await _fileClient.UploadFileAsync(BinaryData.FromBytes(picture), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}.jpg", FileUploadPurpose.Vision);
//			var state = await _stateProvider.GetState();
//			_logger.LogInformation("State: {message}", state);
//			_logger.LogInformation("Input: {message}", message);
//			await _assistantClient.CreateMessageAsync(thread.Id, MessageRole.User,
//				[
//					MessageContent.FromText(/*">"+state + "\n"+*/ message),
//					MessageContent.FromImageFileId(pictureUploaded.Value.Id)
//				]);
//			waitForInput = !await RunAsync(assistant, thread, false, stoppingToken);
//		}
//	}

//	private async Task<string?> WaitForInput(CancellationToken stoppingToken)
//	{
//		return await _speachInput.Read(stoppingToken);
//	}

//	private async Task<bool> RunAsync(Assistant assistant, AssistantThread thread, bool first, CancellationToken stoppingToken)
//	{
//		_logger.LogInformation("Thinking");

//		var streamingUpdates = _assistantClient.CreateRunStreamingAsync(
//					thread.Id,
//					assistant.Id,
//					new RunCreationOptions()
//					{
//						//MaxCompletionTokens = first ? 51: null,
//						//AdditionalInstructions = "When possible, try to sneak in puns if you're asked to compare things.",
//					});
//		//Finally, to handle the StreamingUpdates as they arrive, you can use the UpdateKind property on the base StreamingUpdate and / or downcast to a specifically desired update type, like MessageContentUpdate for thread.message.delta events or RequiredActionUpdate for streaming tool calls.

//		try
//		{
//			await foreach (StreamingUpdate streamingUpdate in streamingUpdates)
//			{
//				stoppingToken.ThrowIfCancellationRequested();
//				if (streamingUpdate is MessageContentUpdate contentUpdate)
//				{
//					await _parser.Add(contentUpdate.Text);
//					//Console.Write(contentUpdate.Text);
//					if (contentUpdate.ImageFileId is not null)
//					{
//						_logger.LogInformation($"Image content file ID: {contentUpdate.ImageFileId}");
//					}
//				}
//				else if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunQueued)
//				{
//					_logger.LogInformation("Queued");
//				}
//				else
//				{
//					_logger.LogDebug($"{streamingUpdate.UpdateKind} {streamingUpdate.GetType().Name}");
//				}
//			}
//		}
//		catch (OperationCanceledException) { throw; }
//		catch (Exception ex)
//		{
//			_logger.LogError(ex, "Error in ChatGpt");
//		}
//		return await _parser.Finish();
//	}
//}
