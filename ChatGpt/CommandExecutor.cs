using SmartCar.Commands;
using SmartCar.PicarX;

namespace SmartCar.ChatGpt;

public class CommandExecutor
{
	private readonly ILogger<CommandExecutor> _logger;
	private readonly IEnumerable<ICommandProvider> _providers;
	private readonly Picarx _picarx;
	private readonly PicarX.StateProvider _stateProvider;

	private readonly object _sync = new();
	private int _executingBatchId = -1; // currently executing batch
	private int _incomingBatchId = -1; // batch being received while executing
	private CancellationTokenSource? _execCts;

	private readonly System.Collections.Concurrent.ConcurrentQueue<CommandSpec> _execQueue = new();
	private readonly List<ICommand> _startedCommands = new();
	private readonly SemaphoreSlim _queueSignal = new(0);
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
		// wake worker in case it is waiting for state change
		_queueSignal.Release();
	}

	public void StartBatch(int batchId)
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
				_state = ExecutorState.CollectingIncomingFirst;
			}
			// wake worker in case it is waiting for state change
			_queueSignal.Release();
		}
	}

	private async Task ProcessQueueLoop()
	{
		while (_workerRunning)
		{
			// wait for signal: new item or state change
			await _queueSignal.WaitAsync();
			var execResult = new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.OK, Reason = ExecReason.NONE };
			// drain queue
			while (execResult.Status == ExecStatus.OK && _execQueue.TryDequeue(out var spec))
			{
				// execute the command
				var handler = FindHandler(spec.Name);
				if (handler == null)
				{
					_logger.LogError("No handler for command {Name}", spec.Name);
					execResult.Status = ExecStatus.FAILED;
					execResult.Reason = ExecReason.PARSE_ERROR;
				}
				else
				{
					try
					{
						var cmdResult = await handler.Execute(spec.Args, _execCts?.Token ?? CancellationToken.None);
						execResult = MapCommandResultToExecResult(cmdResult);
					}
					catch (OperationCanceledException)
					{
						execResult.Status = ExecStatus.INTERRUPTED;
						execResult.Reason = ExecReason.USER_STOP;
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error executing command {Name}", spec.Name);
						execResult.Status = ExecStatus.FAILED;
						execResult.Reason = ExecReason.INTERNAL_ERROR;
					}
					if (execResult.Status != ExecStatus.OK)
					{
						// stop executing further commands for this batch
						_logger.LogInformation("Command {Name} returned {Result}, stopping batch", spec.Name, execResult.Status);
					}
					lock (_sync)
					{
						_startedCommands.Add(handler);
					}
				}
			}

			// after draining the queue, decide whether to finalize or wait
			ExecutorState stateCopy;
			lock (_sync)
			{
				stateCopy = _state;
			}
			if (execResult.Status != ExecStatus.OK || (_execQueue.IsEmpty && stateCopy != ExecutorState.Executing))
			{

				await FinalizeStartedCommands();
				if (execResult.Status != ExecStatus.OK)
				{
					// stop executing further commands for this batch
					_execCts?.Cancel();
					_execQueue.Clear();
				}

				lock (_sync)
				{
					_state = ExecutorState.Idle;
					_executingBatchId = -1;
				}
				await _modelClient.SendExecResultAsync(execResult);
			}
		}
	}

	public void EnqueueCommand(CommandSpec command)
	{
		_logger.LogInformation("EnqueueCommand name={Name} args={Args}", command.Name, string.Join(',', command.Args));
		lock (_sync)
		{
			if (_state == ExecutorState.Executing)
			{
				_execQueue.Enqueue(command);
			}
			else if (_state == ExecutorState.CollectingIncomingFirst)
			{
				if (command.Name == "STOP")
				{
					// preempt: stop motors and cancel current execution
					try
					{
						_picarx.Stop();
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Error stopping motors on STOP command");
					}
					_execCts?.Cancel();
					_execQueue.Clear();
					// do not call Finish on started commands per requirement; clear them
					lock (_sync)
					{
						_startedCommands.Clear();
					}
					_executingBatchId = _incomingBatchId;
					_incomingBatchId = -1;
					_execCts = new CancellationTokenSource();
					_state = ExecutorState.Executing;
					// Do not enqueue command, it is stop command
				}
				else
				{
					_state = ExecutorState.IncomingIgnoring;
				}
			}
			else if (_state == ExecutorState.IncomingIgnoring)
			{
				_logger.LogWarning("Ignoring: {Name}", command.Name);
			}
			else if (_state == ExecutorState.Idle)
			{
				// no active batch; ignore until StartBatchAsync is called
				_logger.LogWarning("Received command while idle: {Name}", command.Name);
			}
			else if (_state == ExecutorState.FinishCalled)
			{
				// finish called: still accept commands? normally should not accept; ignore
				_logger.LogInformation("Received command after finish requested, ignoring: {Name}", command.Name);
			}
			_queueSignal.Release();
		}
	}

	public void FinishBatch()
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
		// wake worker so it can finalize if appropriate
		_queueSignal.Release();
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
			CommandResult.OK => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.OK, Reason = ExecReason.NONE },
			CommandResult.OBSTACLE => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.INTERRUPTED, Reason = ExecReason.OBSTACLE },
			CommandResult.INTERRUPTED => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.INTERRUPTED, Reason = ExecReason.USER_STOP },
			CommandResult.FAILED => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.INTERNAL_ERROR },
			_ => new ExecResult { BatchId = _executingBatchId, Status = ExecStatus.FAILED, Reason = ExecReason.INTERNAL_ERROR }
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
}
