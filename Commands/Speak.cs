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
		public override string Name => "";
		private Task? _speakTask;

		public override async Task Execute(string[] parameters)
		{
			var text = string.Join(" ", parameters);
			await WaitForPreviousSpeak();
			_speakTask = textPlayer.Play(text);
		}
		public override async Task Finish()
		{
			await WaitForPreviousSpeak();
			await base.Finish();
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
