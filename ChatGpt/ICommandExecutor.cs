using System.Threading;
using System.Threading.Tasks;

namespace SmartCar.ChatGpt;

public interface ICommandExecutor
{
    Task StartBatchAsync(int batchId, CancellationToken ct);
    Task<ExecResult> RunCommandAsync(CommandSpec command, CancellationToken ct);
    Task<ExecResult> FinishBatchAsync(CancellationToken ct);
    Task ImmediateStopAsync();
}
