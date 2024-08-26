namespace SmartCar.Commands;

public interface ICommandProvider
{
	ICommand[] Commands { get; }
}
