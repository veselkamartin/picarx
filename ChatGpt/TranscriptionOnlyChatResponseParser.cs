using System.Text;

namespace SmartCar.ChatGpt;

/// <summary>
/// Parser for transcription-only mode that just logs all output without command parsing.
/// </summary>
/// <remarks>
/// This parser is used for testing transcription accuracy. It accumulates all text
/// from the model and logs it when Finish() is called, without any command extraction.
/// </remarks>
public class TranscriptionOnlyChatResponseParser : IChatResponseParser
{
	private readonly StringBuilder _builder = new();
	private readonly ILogger<TranscriptionOnlyChatResponseParser> _logger;

	public TranscriptionOnlyChatResponseParser(ILogger<TranscriptionOnlyChatResponseParser> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Adds streaming text to the buffer.
	/// </summary>
	/// <param name="text">Text chunk from model output stream</param>
	public Task Add(string text)
	{
		_builder.Append(text);
		_logger.LogDebug("Received text chunk: {Text}", text);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Logs the complete accumulated text as the transcription result.
	/// </summary>
	public Task Finish()
	{
		var fullText = _builder.ToString().Trim();
		_builder.Clear();

		if (!string.IsNullOrWhiteSpace(fullText))
		{
			_logger.LogInformation("=== TRANSCRIPTION RESULT ===");
			_logger.LogInformation("{Transcription}", fullText);
			_logger.LogInformation("============================");
		}

		return Task.CompletedTask;
	}
}
