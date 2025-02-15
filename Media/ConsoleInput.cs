namespace SmartCar.Media;

public class ConsoleInput : ISpeachInput
{
	bool _fist = true;
	public Task<string> Read(CancellationToken stoppingToken)
	{
		if (_fist)
		{
			_fist = false;
			return Task.FromResult("Popojeď dva metry");
		}
		Console.Write("Vstup: ");
		var input = Console.ReadLine();
		return Task.FromResult(input);
	}
}