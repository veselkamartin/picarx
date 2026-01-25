using System.Threading;
using SmartCar.ChatGpt;

namespace SmartCar.Commands;

public abstract class CommandBase : ICommand
{
    public abstract string Name { get; }

    public abstract Task<CommandResult> Execute(string[] parameters, CancellationToken ct);
    public virtual Task Finish(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
    protected void ParseParams<T1>(string[] parameters, out T1 p1)
    {
        CheckParamsCount(parameters, 1);
        p1 = ParseParam<T1>(parameters[0], 0);
    }
    protected void ParseParams<T1, T2>(string[] parameters, out T1 p1, out T2 p2)
    {
        CheckParamsCount(parameters, 2);
        p1 = ParseParam<T1>(parameters[0], 0);
        p2 = ParseParam<T2>(parameters[1], 1);
    }
    private T ParseParam<T>(string parameter, int paramIndex)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)parameter;
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)Convert.ChangeType(parameter, typeof(T));
        }
        throw new InvalidCommandException($"Command {Name} parameter {paramIndex + 1} unknown type");
    }

    private void CheckParamsCount(string[] parameters, int count)
    {
        if (parameters.Length < count) throw new InvalidCommandException($"Command {Name} invalid parameters count {parameters.Length}, expected {count}");
    }
}
