using SmartCar.SunFounderControler;

namespace SmartCar.ChatGpt;

public class ChatHost : BackgroundService
{
	private readonly IChatClient _chat;
	private readonly ControlerHandler _controlerHandler;
	private readonly ILogger<ChatHost> _logger;

	public ChatHost(IChatClient chat, ControlerHandler controlerHandler, ILogger<ChatHost> logger)
	{
		_chat = chat;
		_controlerHandler = controlerHandler;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			if (_controlerHandler.Connected)
			{
				await Task.Delay(1000);
			}
			else
			{
				var chatStop = new CancellationTokenSource();
				stoppingToken.Register(chatStop.Cancel);
				_controlerHandler.ConnectedChanged += () =>
				{
					if (_controlerHandler.Connected)
					{
						chatStop.Cancel();
					}
				};
				try
				{
					await _chat.StartAsync(chatStop.Token);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error in chat, retry");
				}
				await Task.Delay(1000);
			}
		}
	}
}
