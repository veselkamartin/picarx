using System.Threading;
using System.Threading.Tasks;

namespace SmartCar.ChatGpt;

public interface ICommandExecutor
{
    Task StartBatchAsync(int batchId, CancellationToken ct);
    Task EnqueueCommandAsync(CommandSpec command, CancellationToken ct);
    Task FinishBatchAsync(CancellationToken ct);
    Task ImmediateStopAsync();
}
