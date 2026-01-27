namespace SmartCar.ChatGpt;

public interface IChatClient
{
	Task StartAsync(CancellationToken stoppingToken);
}
