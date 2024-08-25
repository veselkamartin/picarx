namespace PicarX.Commands;

public interface ICommandProvider
{
	ICommand[] Commands { get; }
}
