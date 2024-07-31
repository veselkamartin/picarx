using Microsoft.Extensions.Logging;
using PicarX.PicarX;
using System.Text;

namespace PicarX.ChatGpt;

public class ChatResponseParser
{
	private readonly StringBuilder _builder = new();
	private readonly PicarX.Picarx _px;
	private readonly ITextPlayer _textPlayer;
	private readonly ILogger<ChatResponseParser> _logger;
	private Task? _speakTask;
	private bool _continue;

	public ChatResponseParser(PicarX.Picarx picarx, ITextPlayer textPlayer, ILogger<ChatResponseParser> logger)
	{
		_px = picarx;
		_textPlayer = textPlayer;
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
		await WaitForPreviousSpeak();
		var returnContinue = _continue;
		_continue = false;
		return returnContinue;
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
				switch (args[0])
				{
					case "FORWARD":
						await Forward(args[1]);
						break;
					case "BACK":
						await Back(args[1]);
						break;
					case "LEFT":
						await Left(args[1]);
						break;
					case "RIGHT":
						await Right(args[1]);
						break;
					case "CAMERA":
						Camera(args[1], args[2]);
						break;
					case "CONTINUE":
						_continue = true;
						break;
					default:
						throw new Exception($"Unknown command {v}");
				}
			}
			else
			{
				await Speak(v);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error parsing line");
		}
	}

	private async Task Speak(string v)
	{
		await WaitForPreviousSpeak();
		_speakTask = _textPlayer.Play(v);
	}
	private async Task WaitForPreviousSpeak()
	{
		if (_speakTask != null)
		{
			await _speakTask;
			_speakTask = null;
		}
	}

	private void Camera(string v1, string v2)
	{
		if (!int.TryParse(v1, out var pan)) return;
		if (!int.TryParse(v2, out var tilt)) return;

		_px.SetCamPanAngle(pan);
		_px.SetCamTiltAngle(tilt);
	}

	private async Task Right(string v)
	{
		if (!int.TryParse(v, out var angle)) return;
		await _px.Turn(angle);
	}

	private async Task Left(string v)
	{
		if (!int.TryParse(v, out var angle)) return;
		await _px.Turn(-angle);
	}

	private async Task Forward(string v)
	{
		if (!int.TryParse(v, out var distanceInCm)) return;

		await _px.DirectForward(distanceInCm);
	}

	private async Task Back(string v)
	{
		if (!int.TryParse(v, out var distanceInCm)) return;

		await _px.DirectBack(distanceInCm);
	}
}
