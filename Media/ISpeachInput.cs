
namespace SmartCar.Media
{
	public interface ISpeachInput
	{
		Task<string> Read(CancellationToken stoppingToken);
	}
}