using System.Threading;
using SmartCar.ChatGpt;

namespace SmartCar.Commands;

public interface ICommand
{
    string Name { get; }
    Task<CommandResult> Execute(string[] parameters, CancellationToken ct);
    Task Finish(CancellationToken ct);
}
