using Microsoft.Extensions.Logging;
using SmartCar.Commands;
using SmartCar.PicarX;
using System.Threading;

namespace SmartCar.ChatGpt;

public class CommandExecutor : ICommandExecutor
{
    private readonly ILogger<CommandExecutor> _logger;
    private readonly IEnumerable<ICommandProvider> _providers;
    private readonly Picarx _picarx;
    private readonly PicarX.StateProvider _stateProvider;

    private readonly object _sync = new();
    private int _executingBatchId = -1; // currently executing batch
    private int _incomingBatchId = -1; // batch being received while executing
    private int _incomingCommandsCount = 0;
    private CancellationTokenSource? _execCts;

    private enum ExecutorState
    {
        Idle,
        Executing,
        CollectingIncoming
    }

    private ExecutorState _state = ExecutorState.Idle;

    public CommandExecutor(ILogger<CommandExecutor> logger, IEnumerable<ICommandProvider> providers, Picarx picarx, PicarX.StateProvider stateProvider)
    {
        _logger = logger;
        _providers = providers;
        _picarx = picarx;
        _stateProvider = stateProvider;
    }

    public Task StartBatchAsync(int batchId, CancellationToken ct)
    {
        lock (_sync)
        {
            _logger.LogInformation("StartBatch {BatchId} (state={State} executing={Executing})", batchId, _state, _executingBatchId != -1);
            if (_state == ExecutorState.Idle)
            {
                _executingBatchId = batchId;
                _execCts = new CancellationTokenSource();
                _state = ExecutorState.Executing;
            }
            else
            {
                // collect incoming batch until we see first command
                _incomingBatchId = batchId;
                _incomingCommandsCount = 0;
                _state = ExecutorState.CollectingIncoming;
            }
        }
        return Task.CompletedTask;
    }

    public async Task<ExecResult> RunCommandAsync(CommandSpec command, CancellationToken ct)
    {
        _logger.LogInformation("RunCommand currentExec={Executing} name={Name} args={Args}", _executingBatchId, command.Name, string.Join(',', command.Args));

        // Determine which batch this command belongs to and act accordingly
        CancellationTokenSource? ctsToUse = null;
        bool shouldExecute = false;
        bool isIgnored = false;

        lock (_sync)
        {
            if (_state == ExecutorState.Executing)
            {
                // commands belong to currently executing batch
                shouldExecute = true;
                ctsToUse = _execCts;
            }
            else if (_state == ExecutorState.CollectingIncoming)
            {
                // commands belong to the incoming batch
                _incomingCommandsCount++;
                // If currently executing, only a batch that starts with STOP may preempt
                if (_executingBatchId != -1)
                {
                    if (_incomingCommandsCount == 1 && command.Name == "STOP")
                    {
                        // preempt current execution
                        _logger.LogInformation("Preempting current batch {Executing} with STOP from batch {Incoming}", _executingBatchId, _incomingBatchId);
                        try
                        {
                            _picarx.Stop();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error stopping picarx during preempt");
                        }
                        _execCts?.Cancel();
                        // move incoming to executing
                        _executingBatchId = _incomingBatchId;
                        _incomingBatchId = -1;
                        _incomingCommandsCount = 0;
                        _execCts = new CancellationTokenSource();
                        ctsToUse = _execCts;
                        shouldExecute = true;
                        _state = ExecutorState.Executing;
                    }
                    else
                    {
                        // ignore this batch's command
                        isIgnored = true;
                    }
                }
                else
                {
                    // accept incoming as executing (no current execution)
                    _executingBatchId = _incomingBatchId;
                    _incomingBatchId = -1;
                    _incomingCommandsCount = 0;
                    _execCts = new CancellationTokenSource();
                    ctsToUse = _execCts;
                    shouldExecute = true;
                    _state = ExecutorState.Executing;
                }
            }
            else
            {
                // no known state to accept this command
                isIgnored = true;
            }
        }

        if (isIgnored)
        {
            _logger.LogInformation("Ignoring command from batch (unknown) name={Name}", command.Name);
            return new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.IGNORED, Reason = ExecReason.NONE, DistFrontCm = (int)_picarx.GetDistance() };
        }

        if (!shouldExecute || ctsToUse == null)
        {
            return new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.IGNORED, Reason = ExecReason.NONE, DistFrontCm = (int)_picarx.GetDistance() };
        }

        // Execute the command using registered providers
        try
        {
            var handler = FindHandler(command.Name);
            if (handler == null)
            {
                _logger.LogError("No handler for command {Name}", command.Name);
                return new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.PARSE_ERROR, DistFrontCm = (int)_picarx.GetDistance() };
            }

            var cmdResult = await handler.Execute(command.Args, ctsToUse.Token);
            // map CommandResult to ExecResult
            var mapped = cmdResult switch
            {
                CommandResult.OK => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.OK, Reason = ExecReason.NONE, DistFrontCm = (int)_picarx.GetDistance() },
                CommandResult.PARSE_ERROR => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.PARSE_ERROR, DistFrontCm = (int)_picarx.GetDistance() },
                CommandResult.OBSTACLE => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.INTERRUPTED, Reason = ExecReason.OBSTACLE, DistFrontCm = (int)_picarx.GetDistance() },
                CommandResult.USER_STOP => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.INTERRUPTED, Reason = ExecReason.USER_STOP, DistFrontCm = (int)_picarx.GetDistance() },
                CommandResult.SAFETY => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.SAFETY, DistFrontCm = (int)_picarx.GetDistance() },
                CommandResult.INTERRUPTED => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.INTERRUPTED, Reason = ExecReason.USER_STOP, DistFrontCm = (int)_picarx.GetDistance() },
                CommandResult.FAILED => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.INTERNAL_ERROR, DistFrontCm = (int)_picarx.GetDistance() },
                _ => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.INTERNAL_ERROR, DistFrontCm = (int)_picarx.GetDistance() }
            };

            return mapped;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Command cancelled for executing batch {BatchId}", _executingBatchId);
            return new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.INTERRUPTED, Reason = ExecReason.USER_STOP, DistFrontCm = (int)_picarx.GetDistance() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command {Name}", command.Name);
            return new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.INTERNAL_ERROR, Detail = ex.Message, DistFrontCm = (int)_picarx.GetDistance() };
        }
    }

    public Task<ExecResult> FinishBatchAsync(CancellationToken ct)
    {
        int finishedBatch = -1;
        lock (_sync)
        {
            finishedBatch = _executingBatchId;
            _logger.LogInformation("FinishBatch called for {BatchId}", finishedBatch);
            _executingBatchId = -1;
            try
            {
                _execCts?.Dispose();
            }
            catch { }
            _execCts = null;
            _state = ExecutorState.Idle;
        }

        var result = new ExecResult
        {
            BatchId = finishedBatch,
            Status = ExecStatus.OK,
            Reason = ExecReason.NONE,
            DistFrontCm = (int)_picarx.GetDistance()
        };
        return Task.FromResult(result);
    }

    public Task ImmediateStopAsync()
    {
        lock (_sync)
        {
            _logger.LogInformation("ImmediateStop called");
            try
            {
                _picarx.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping picarx");
            }
            _execCts?.Cancel();
        }
        return Task.CompletedTask;
    }

    private ICommand? FindHandler(string name)
    {
        foreach (var prov in _providers)
        {
            var cmd = prov.Commands.SingleOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (cmd is not null) return cmd;
        }
        return null;
    }
}
