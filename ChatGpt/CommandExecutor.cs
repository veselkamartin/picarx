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

    private readonly System.Collections.Concurrent.ConcurrentQueue<CommandSpec> _execQueue = new();
    private readonly List<ICommand> _startedCommands = new();
    private readonly System.Threading.SemaphoreSlim _queueSignal = new(0);
    private readonly IModelClient _modelClient;
    private Task? _workerTask;
    private bool _workerRunning = false;

    private enum ExecutorState
    {
        Idle,
        Executing,
        FinishCalled,
        CollectingIncomingFirst,
        IncomingIgnoring
    }

    private ExecutorState _state = ExecutorState.Idle;

    public CommandExecutor(ILogger<CommandExecutor> logger, IEnumerable<ICommandProvider> providers, Picarx picarx, PicarX.StateProvider stateProvider, IModelClient modelClient)
    {
        _logger = logger;
        _providers = providers;
        _picarx = picarx;
        _stateProvider = stateProvider;
        _modelClient = modelClient;
        StartWorker();
    }

    private void StartWorker()
    {
        lock (_sync)
        {
            if (_workerRunning) return;
            _workerRunning = true;
            _workerTask = Task.Run(ProcessQueueLoop);
        }
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
                _state = ExecutorState.CollectingIncomingFirst;
            }
        }
        return Task.CompletedTask;
    }

    private async Task ProcessQueueLoop()
    {
        while (_workerRunning)
        {
            await _queueSignal.WaitAsync();
            CommandSpec? spec = null;
            while (_execQueue.TryDequeue(out var item))
            {
                spec = item;
                // determine action based on state and incoming rules
                bool shouldEnqueue = false;
                lock (_sync)
                {
                    if (_state == ExecutorState.Executing)
                    {
                        shouldEnqueue = true;
                    }
                    else if (_state == ExecutorState.CollectingIncomingFirst)
                    {
                        // this is the first command of incoming batch
                        _incomingCommandsCount++;
                        if (spec.Name == "STOP")
                        {
                            // preempt
                            try { _picarx.Stop(); } catch { }
                            _execCts?.Cancel();
                            _executingBatchId = _incomingBatchId;
                            _incomingBatchId = -1;
                            _incomingCommandsCount = 0;
                            _execCts = new CancellationTokenSource();
                            _state = ExecutorState.Executing;
                            shouldEnqueue = true;
                        }
                        else
                        {
                            // start ignoring incoming
                            _state = ExecutorState.IncomingIgnoring;
                        }
                    }
                    else if (_state == ExecutorState.IncomingIgnoring)
                    {
                        shouldEnqueue = false;
                    }
                }

                if (!shouldEnqueue)
                {
                    // drop the command
                    continue;
                }

                // execute the command
                var handler = FindHandler(spec.Name);
                if (handler == null)
                {
                    _logger.LogError("No handler for command {Name}", spec.Name);
                    await PushExecResult(ExecStatus.FAILED, ExecReason.PARSE_ERROR);
                    lock (_sync) { _state = ExecutorState.Idle; _executingBatchId = -1; }
                    break;
                }

                lock (_sync) { _startedCommands.Add(handler); }

                CommandResult cmdResult = CommandResult.OK;
                try
                {
                    cmdResult = await handler.Execute(spec.Args, _execCts?.Token ?? CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    cmdResult = CommandResult.INTERRUPTED;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing command {Name}", spec.Name);
                    cmdResult = CommandResult.FAILED;
                }

                if (cmdResult != CommandResult.OK)
                {
                    // stop executing further commands for this batch
                    _logger.LogInformation("Command {Name} returned {Result}, stopping batch", spec.Name, cmdResult);
                    await FinalizeStartedCommands();
                    var mapped = MapCommandResultToExecResult(cmdResult);
                    await _modelClient.SendExecResultAsync(mapped);
                    lock (_sync)
                    {
                        _state = ExecutorState.Idle;
                        _executingBatchId = -1;
                    }
                    break;
                }

                // continue to next command
                // if FinishBatch was called and queue empty, finalize
                lock (_sync)
                {
                    if (_state == ExecutorState.CollectingIncomingFirst || _state == ExecutorState.Executing)
                    {
                        // nothing
                    }
                }
            }
            // if loop drained and FinishCalled -> finalize
            bool shouldFinalize = false;
            lock (_sync)
            {
                if ((_state == ExecutorState.FinishCalled || _state == ExecutorState.Executing) && _execQueue.IsEmpty)
                {
                    shouldFinalize = true;
                }
            }
            if (shouldFinalize)
            {
                await FinalizeStartedCommands();
                var res = new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.OK, Reason = ExecReason.NONE, DistFrontCm = (int)_picarx.GetDistance() };
                await _modelClient.SendExecResultAsync(res);
                lock (_sync)
                {
                    _state = ExecutorState.Idle;
                    _executingBatchId = -1;
                }
            }
        }
    }

    public Task EnqueueCommandAsync(CommandSpec command, CancellationToken ct)
    {
        _logger.LogInformation("EnqueueCommand name={Name} args={Args}", command.Name, string.Join(',', command.Args));
        lock (_sync)
        {
            if (_state == ExecutorState.Executing)
            {
                _execQueue.Enqueue(command);
                _queueSignal.Release();
                return Task.CompletedTask;
            }
            else if (_state == ExecutorState.CollectingIncomingFirst)
            {
                _incomingCommandsCount++;
                if (_incomingCommandsCount == 1)
                {
                    if (command.Name == "STOP")
                    {
                        // preempt: stop motors and cancel current execution
                        try { _picarx.Stop(); } catch { }
                        _execCts?.Cancel();
                        // do not call Finish on started commands per requirement; clear them
                        lock (_sync) { _startedCommands.Clear(); }
                        _executingBatchId = _incomingBatchId;
                        _incomingBatchId = -1;
                        _incomingCommandsCount = 0;
                        _execCts = new CancellationTokenSource();
                        _state = ExecutorState.Executing;
                        _execQueue.Enqueue(command);
                        _queueSignal.Release();
                        return Task.CompletedTask;
                    }
                    else
                    {
                        _state = ExecutorState.IncomingIgnoring;
                        return Task.CompletedTask;
                    }
                }
                else
                {
                    // subsequent incoming while in CollectingIncomingFirst
                    if (_state == ExecutorState.IncomingIgnoring)
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            else if (_state == ExecutorState.IncomingIgnoring)
            {
                return Task.CompletedTask;
            }
            else if (_state == ExecutorState.Idle)
            {
                // no active batch; ignore until StartBatchAsync is called
                _logger.LogWarning("Received command while idle: {Name}", command.Name);
                return Task.CompletedTask;
            }
            else if (_state == ExecutorState.FinishCalled)
            {
                // finish called: still accept commands? normally should not accept; ignore
                _logger.LogInformation("Received command after finish requested, ignoring: {Name}", command.Name);
                return Task.CompletedTask;
            }
        }
        return Task.CompletedTask;
    }

    public Task FinishBatchAsync(CancellationToken ct)
    {
        lock (_sync)
        {
            _logger.LogInformation("FinishBatch called for executing batch {BatchId}", _executingBatchId);
            if (_state == ExecutorState.Executing)
            {
                _state = ExecutorState.FinishCalled;
            }
            // if Idle or other state, nothing to do
        }
        return Task.CompletedTask;
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
            // clear queue
            while (_execQueue.TryDequeue(out _)) { }
            _queueSignal.Release();
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

    private ExecResult MapCommandResultToExecResult(CommandResult r)
    {
        return r switch
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
    }

    private async Task FinalizeStartedCommands()
    {
        List<ICommand> toFinish;
        lock (_sync)
        {
            toFinish = new List<ICommand>(_startedCommands);
            _startedCommands.Clear();
        }
        foreach (var cmd in toFinish)
        {
            try
            {
                await cmd.Finish(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing command");
            }
        }
    }

    private async Task PushExecResult(ExecStatus status, ExecReason reason)
    {
        var res = new ExecResult { BatchId = _executingBatchId, Status = status, Reason = reason, DistFrontCm = (int)_picarx.GetDistance() };
        await _modelClient.SendExecResultAsync(res);
    }
}
