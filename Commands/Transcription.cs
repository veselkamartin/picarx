using SmartCar.ChatGpt;

namespace SmartCar.Commands;

/// <summary>
/// Provides TRANSCRIPTION command for logging what the model heard from user voice input.
/// </summary>
public class Transcription : ICommandProvider
{
	public Transcription(ILogger<Transcription> logger)
	{
		Commands = [
			new TranscriptionCommand(logger),
		];
	}

	public ICommand[] Commands { get; }

	class TranscriptionCommand(ILogger<Transcription> logger) : CommandBase
	{
		public override string Name => "TRANSCRIPTION";

		public override Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
		{
			var text = string.Join(" ", parameters);
			logger.LogInformation("=== TRANSCRIPTION ===");
			logger.LogInformation("{Transcription}", text);
			logger.LogInformation("=====================");
			return Task.FromResult(CommandResult.OK);
		}
	}
}
