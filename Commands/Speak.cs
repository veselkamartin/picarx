using System.Threading;
using SmartCar.ChatGpt;

namespace SmartCar.Commands;

public class Speak : ICommandProvider
{
	public Speak(ITextPlayer textPlayer)
	{
		Commands = [
			new SpeakCommand(textPlayer),
		];
	}

	public ICommand[] Commands { get; }

	class SpeakCommand(ITextPlayer textPlayer) : CommandBase
	{
		public override string Name => "SAY";
		private Task? _speakTask;

        public override async Task<CommandResult> Execute(string[] parameters, CancellationToken ct)
        {
            var text = string.Join(" ", parameters);
            await WaitForPreviousSpeak();
            _speakTask = textPlayer.Play(text);
            return CommandResult.OK;
        }
		public override async Task Finish(CancellationToken ct)
		{
			await WaitForPreviousSpeak();
			await base.Finish(ct);
		}
		private async Task WaitForPreviousSpeak()
		{
			if (_speakTask != null)
			{
				await _speakTask;
				_speakTask = null;
			}
		}
	}
}
