using Microsoft.Extensions.Logging;
using SmartCar.Commands;
using System.Text;

namespace SmartCar.ChatGpt;

public class ChatResponseParser
{
	private readonly StringBuilder _builder = new();
	private readonly ICommand[] _commands;
	private readonly ILogger<ChatResponseParser> _logger;
	private List<ICommand> _currentCommands = new List<ICommand>();
	private bool _continue;

	public ChatResponseParser(
		ICommandProvider[] commandProviders,
		ILogger<ChatResponseParser> logger
		)
	{
		var continueCommand = new ContinueCommand(this);
		_commands = commandProviders.SelectMany(cp => cp.Commands).Append(continueCommand).ToArray();
		_logger = logger;
	}

	public async Task Add(string text)
	{
		_builder.Append(text);
		if (text.Contains('\n'))
		{
			var lines = _builder.ToString().Split('\n');
			_builder.Clear();
			for (int i = 0; i < lines.Length - 1; i++)
			{
				await ProcessLine(lines[i]);
			}
			_builder.Append(lines[^1]);
		}
	}

	public async Task<bool> Finish()
	{
		var lines = _builder.ToString().Split('\n');
		_builder.Clear();
		foreach (var line in lines)
		{
			await ProcessLine(line);
		}
		foreach (var command in _currentCommands)
		{
			await command.Finish();
		}
		var returnContinue = _continue;
		_continue = false;
		return returnContinue;
	}

	class ContinueCommand(ChatResponseParser parser) : CommandBase
	{
		public override string Name => "CONTINUE";

		public override Task Execute(string[] parameters)
		{
			parser._continue = true;
			return Task.CompletedTask;
		}
	}

	private async Task ProcessLine(string v)
	{
		if (string.IsNullOrEmpty(v)) return;

		_logger.LogInformation(v);
		try
		{
			if (v.StartsWith('>'))
			{
				var args = v.Substring(1).Split(' ');
				string commandName = args[0];
				string[] commandArgs = args.Skip(1).ToArray();
				var command = _commands.SingleOrDefault(c => c.Name == commandName);
				if (command == null) throw new InvalidCommandException($"Unknown command {commandName}");
				await command.Execute(commandArgs);
				_currentCommands.Add(command);
			}
			else
			{
				var args = v.Substring(1).Split(' ');
				var command = _commands.SingleOrDefault(c => c.Name == "");
				if (command == null) throw new InvalidCommandException($"Speak command not registered");
				await command.Execute(args);
				_currentCommands.Add(command);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error parsing line");
		}
	}

}
