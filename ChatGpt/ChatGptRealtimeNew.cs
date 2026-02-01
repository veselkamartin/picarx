using OpenAI;
using OpenAI.Realtime;
using SmartCar.Media;
using SmartCar.PicarX;

namespace SmartCar.ChatGpt;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class ChatGptRealtimeNew : IChatClient, IModelClient, IDisposable
{
	private readonly RealtimeClient _realtimeClient;
	private readonly ChatResponseParser _parser;
	private readonly ILogger<ChatGptRealtimeNew> _logger;
	private readonly ICamera _camera;
	private readonly OpenTkSoundRecorder _soundRecorder;
	private readonly StateProvider _stateProvider;
	private RealtimeSession? _session;
	private CancellationTokenSource? _audioStreamCts;
	private Task? _audioStreamTask;
	private Task? _cameraStreamTask;
	private Task? _updateProcessorTask;
	private OpenTkSoundRecorder.DeltaRecorder? _deltaRecorder;
	private bool _isDisposed = false;

	private const int MaxReconnectAttempts = 5;
	private const int InitialReconnectDelayMs = 1000;
	private const int MaxReconnectDelayMs = 30000;
	private const double BackoffMultiplier = 2.0;

	public ChatGptRealtimeNew(
		OpenAIClient client,
		ILogger<ChatGptRealtimeNew> logger,
		ChatResponseParser parser,
		ICamera camera,
		OpenTkSoundRecorder soundRecorder,
		StateProvider stateProvider
	)
	{
		_realtimeClient = client.GetRealtimeClient();
		_parser = parser;
		_logger = logger;
		_camera = camera;
		_soundRecorder = soundRecorder;
		_stateProvider = stateProvider;
	}

	public async Task StartAsync(CancellationToken stoppingToken)
	{
		int reconnectAttempt = 0;
		int reconnectDelayMs = InitialReconnectDelayMs;

		while (!stoppingToken.IsCancellationRequested && reconnectAttempt < MaxReconnectAttempts)
		{
			try
			{
				if (reconnectAttempt > 0)
				{
					_logger.LogInformation("Reconnection attempt {Attempt}/{MaxAttempts}, waiting {DelayMs}ms",
						reconnectAttempt, MaxReconnectAttempts, reconnectDelayMs);
					await Task.Delay(reconnectDelayMs, stoppingToken);

					// Exponential backoff
					reconnectDelayMs = (int)Math.Min(reconnectDelayMs * BackoffMultiplier, MaxReconnectDelayMs);
				}

				// Start the session
				_session = await _realtimeClient.StartConversationSessionAsync("gpt-realtime", new(), stoppingToken);
				_logger.LogInformation("Realtime session started (attempt {Attempt})", reconnectAttempt + 1);

				// Configure session for text-only output with server VAD
				var sessionOptions = new ConversationSessionOptions
				{
					Instructions = ChatGptInstructions.Instructions,
					ContentModalities = RealtimeContentModalities.Text, // Text-only output (no audio generation)
					InputAudioFormat = RealtimeAudioFormat.Pcm16,
					// Server-side VAD for turn detection with defaults
					TurnDetectionOptions = TurnDetectionOptions.CreateServerVoiceActivityTurnDetectionOptions(),
					Temperature = 0.4f, // Lower temperature for more consistent command syntax
					MaxOutputTokens = 2048,
				};

				await _session.ConfigureConversationSessionAsync(sessionOptions, stoppingToken);
				_logger.LogInformation("Session configured: text-only output, server VAD, temperature={Temp}", sessionOptions.Temperature);

				// Reset reconnect state on successful connection
				reconnectAttempt = 0;
				reconnectDelayMs = InitialReconnectDelayMs;

				// Start background tasks
				_audioStreamCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
				_audioStreamTask = Task.Run(() => StreamAudioAsync(_audioStreamCts.Token), stoppingToken);
				_cameraStreamTask = Task.Run(() => StreamCameraAsync(_audioStreamCts.Token), stoppingToken);
				_updateProcessorTask = Task.Run(() => ProcessUpdatesAsync(_audioStreamCts.Token), stoppingToken);

				// Wait for all tasks - if any complete/fail, we'll reconnect
				var completedTask = await Task.WhenAny(
					_audioStreamTask,
					_cameraStreamTask,
					_updateProcessorTask,
					Task.Delay(Timeout.Infinite, stoppingToken)
				);

				// Check if task completed due to error (not cancellation)
				if (completedTask == _audioStreamTask || completedTask == _cameraStreamTask || completedTask == _updateProcessorTask)
				{
					try
					{
						await completedTask; // Propagate exception if any
						_logger.LogWarning("Background task completed unexpectedly, attempting reconnection");
					}
					catch (OperationCanceledException)
					{
						// Expected during shutdown
						break;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Background task failed, attempting reconnection");
					}

					// Cleanup before reconnecting
					await CleanupAsync();
					reconnectAttempt++;
					continue;
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogInformation("Realtime session cancelled");
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in realtime session (attempt {Attempt}/{MaxAttempts})",
					reconnectAttempt + 1, MaxReconnectAttempts);

				await CleanupAsync();
				reconnectAttempt++;

				if (reconnectAttempt >= MaxReconnectAttempts)
				{
					_logger.LogError("Max reconnection attempts reached, giving up");
					throw;
				}
			}
		}

		await CleanupAsync();
	}

	private async Task StreamAudioAsync(CancellationToken cancellationToken)
	{
		const int chunkSizeMs = 200; // 100ms chunks
		const int sampleRate = 24000; // OpenAI expects 24kHz for PCM16
		_deltaRecorder = _soundRecorder.CreateDeltaRecorder();

		_logger.LogInformation("Starting audio streaming (24kHz PCM16, {ChunkMs}ms chunks)", chunkSizeMs);

		var chunkData = new short[sampleRate * chunkSizeMs / 1000];
		var chunkSamplesRead = 0;
		try
		{
			while (!cancellationToken.IsCancellationRequested && _session != null)
			{
				// TODO: Implement echo cancellation by checking if TTS is playing
				// If _soundPlayer.IsPlaying, either gate/mute the microphone or reduce gain
				// to prevent the model from hearing its own speech output

				// Record a chunk
				var recordedData = _deltaRecorder.ReadAvailableSamples();

				if (recordedData.Data.Length == 0)
				{
					await Task.Delay(chunkSizeMs, cancellationToken);
					continue;
				}

				// Resample if needed (recorder might use different sample rate)
				short[] audioSamples = recordedData.Data;
				if (recordedData.SampleRate != sampleRate)
				{
					audioSamples = ResampleAudio(audioSamples, recordedData.SampleRate, sampleRate);
				}

				while(audioSamples.Length > 0 && !cancellationToken.IsCancellationRequested)
				{
					// Fill the rest of the chunk
					int samplesToCopy = Math.Min(chunkData.Length - chunkSamplesRead, audioSamples.Length);
					Array.Copy(audioSamples, 0, chunkData, chunkSamplesRead, samplesToCopy);
					chunkSamplesRead += samplesToCopy;

					if (chunkSamplesRead < chunkData.Length)
					{
						// Not enough data to fill chunk yet
						break;
					}
					// Send full chunk
					byte[] audioBytes = ConvertShortsToBytes(chunkData);
					await _session.SendInputAudioAsync(BinaryData.FromBytes(audioBytes), cancellationToken);
					// Prepare for next chunk
					chunkSamplesRead = 0;
					// Remove sent samples from audioSamples
					if (samplesToCopy < audioSamples.Length)
					{
						var remainingSamples = new short[audioSamples.Length - samplesToCopy];
						Array.Copy(audioSamples, samplesToCopy, remainingSamples, 0, remainingSamples.Length);
						audioSamples = remainingSamples;
					}
					else
					{
						audioSamples = Array.Empty<short>();
					}
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Audio streaming stopped");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in audio streaming");
		}
	}

	private async Task StreamCameraAsync(CancellationToken cancellationToken)
	{
		const int idleIntervalMs = 5000; // 5 seconds when idle

		_logger.LogInformation("Starting camera streaming (send only when idle, every {Interval}s)", idleIntervalMs / 1000);

		try
		{
			while (!cancellationToken.IsCancellationRequested && _session != null)
			{
				try
				{
					// Only send state when idle (not executing)
					if (!_stateProvider.IsExecuting)
					{
						// Get state
						var distance = await _stateProvider.GetDistance();
						var carState = $"[CAR_STATE]\nMODE: IDLE\nDIST_FRONT_CM: {distance}";

						// Get camera frame
						var jpegBytes = await _camera.GetPictureAsJpeg();

						// Send text message with car state (images not yet supported in beta API for user messages)
						// For now just send the state text
						await _session.AddItemAsync(
							RealtimeItem.CreateUserMessage([carState]),
							cancellationToken
						);

						_logger.LogDebug("Sent car state: {State}", carState);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error capturing/sending camera frame");
				}

				await Task.Delay(idleIntervalMs, cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Camera streaming stopped");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in camera streaming");
		}
	}

	private async Task ProcessUpdatesAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Starting update processor");

		try
		{
			if (_session == null) return;

			await foreach (var update in _session.ReceiveUpdatesAsync(cancellationToken))
			{
				try
				{
					await HandleUpdateAsync(update, cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error handling update: {UpdateType}", update.GetType().Name);
				}
			}
		}
		catch (OperationCanceledException)
		{
			_logger.LogInformation("Update processor stopped");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in update processor");
		}
	}

	private async Task HandleUpdateAsync(RealtimeUpdate update, CancellationToken cancellationToken)
	{
		// Handle different update types using pattern matching
		switch (update)
		{
			case ConversationSessionConfiguredUpdate configuredUpdate:
				_logger.LogInformation("Session configured");
				break;

			case OpenAI.Realtime.OutputStreamingStartedUpdate:
				_logger.LogDebug("Response streaming started");
				break;

			case OutputDeltaUpdate deltaUpdate:
				// Process text deltas
				if (!string.IsNullOrEmpty(deltaUpdate.Text))
				{
					await _parser.Add(deltaUpdate.Text);
				}
				// Audio should not be present (text-only mode)
				if (deltaUpdate.AudioBytes != null)
				{
					_logger.LogWarning("Received audio bytes in text-only mode");
				}
				break;

			case OutputPartFinishedUpdate finishedUpdate:
				_logger.LogInformation("Response finished");
				await _parser.Finish();
				break;

			case RealtimeErrorUpdate errorUpdate:
				_logger.LogError("Realtime error: {Error}", errorUpdate.GetType());
				break;

			case InputAudioSpeechStartedUpdate:
				_logger.LogDebug("Speech started (VAD)");
				break;

			case InputAudioSpeechFinishedUpdate:
				_logger.LogDebug("Speech finished (VAD)");
				break;

			default:
				_logger.LogDebug("Update: {Type}", update.GetType().Name);
				break;
		}
	}

	public async Task SendExecResultAsync(ExecResult result)
	{
		if (_session == null) return;

		var statusText = result.Status switch
		{
			ExecStatus.OK => "OK",
			ExecStatus.INTERRUPTED => "INTERRUPTED",
			ExecStatus.FAILED => "FAILED",
			ExecStatus.IGNORED => "IGNORED",
			_ => "UNKNOWN"
		};

		var reasonText = result.Reason switch
		{
			ExecReason.NONE => "",
			ExecReason.OBSTACLE => "OBSTACLE",
			ExecReason.USER_STOP => "USER_STOP",
			ExecReason.SAFETY => "SAFETY",
			ExecReason.PARSE_ERROR => "PARSE_ERROR",
			ExecReason.INTERNAL_ERROR => "INTERNAL_ERROR",
			_ => "UNKNOWN"
		};

		var execResultText = string.IsNullOrEmpty(reasonText)
			? $"[EXEC_RESULT id={result.BatchId}]\nSTATUS: {statusText}"
			: $"[EXEC_RESULT id={result.BatchId}]\nSTATUS: {statusText}\nREASON: {reasonText}";

		_logger.LogInformation("Sending exec result: {Result}", execResultText);

		try
		{
			// Use a timeout to prevent hanging if session is unresponsive
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
			await _session.AddItemAsync(
				RealtimeItem.CreateUserMessage([execResultText]),
				cts.Token
			);
		}
		catch (OperationCanceledException)
		{
			_logger.LogWarning("Timeout sending exec result for batch {BatchId}", result.BatchId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending exec result");
		}
	}

	private static byte[] ConvertShortsToBytes(short[] shorts)
	{
		var bytes = new byte[shorts.Length * 2];
		Buffer.BlockCopy(shorts, 0, bytes, 0, bytes.Length);
		return bytes;
	}

	private static short[] ResampleAudio(short[] input, int inputRate, int outputRate)
	{
		if (inputRate == outputRate) return input;

		var ratio = (double)outputRate / inputRate;
		var outputLength = (int)(input.Length * ratio);
		var output = new short[outputLength];

		for (int i = 0; i < outputLength; i++)
		{
			var srcIndex = i / ratio;
			var srcIndexInt = (int)srcIndex;
			if (srcIndexInt >= input.Length - 1)
			{
				output[i] = input[input.Length - 1];
			}
			else
			{
				// Linear interpolation
				var frac = srcIndex - srcIndexInt;
				output[i] = (short)(input[srcIndexInt] * (1 - frac) + input[srcIndexInt + 1] * frac);
			}
		}

		return output;
	}

	private async Task CleanupAsync()
	{
		try
		{
			_audioStreamCts?.Cancel();

			if (_audioStreamTask != null)
				await _audioStreamTask;
			if (_cameraStreamTask != null)
				await _cameraStreamTask;
			if (_updateProcessorTask != null)
				await _updateProcessorTask;

			_session?.Dispose();
			_audioStreamCts?.Dispose();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during cleanup");
		}
	}

	public void Dispose()
	{
		if (_isDisposed) return;
		_isDisposed = true;

		CleanupAsync().GetAwaiter().GetResult();
	}
}

#pragma warning restore OPENAI002
