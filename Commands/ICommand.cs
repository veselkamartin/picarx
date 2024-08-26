namespace SmartCar.Commands;

public interface ICommand
{
	string Name { get; }
	Task Execute(string[] parameters);
	Task Finish();
}
