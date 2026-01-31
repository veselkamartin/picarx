using SmartCar.Commands;
using SmartCar.PicarX;

namespace SmartCar.ChatGpt;

/// <summary>
/// Executes command batches asynchronously using a background worker and queue.
/// </summary>
/// <remarks>
/// <para><b>Execution Model:</b></para>
/// <para>
/// Commands are enqueued into a concurrent queue and executed sequentially by a single background worker.
/// This design allows the parser to continue receiving and parsing model output without blocking on command execution.
/// </para>
/// 
/// <para><b>Batch Lifecycle:</b></para>
/// <list type="number">
/// <item><description><see cref="StartBatch(int)"/> - Begins a new batch with given ID</description></item>
/// <item><description><see cref="EnqueueCommand(CommandSpec)"/> - Adds commands to the execution queue (non-blocking)</description></item>
/// <item><description><see cref="FinishBatch()"/> - Signals end of batch (non-blocking)</description></item>
/// <item><description>Worker drains queue, calls <see cref="ICommand.Finish(CancellationToken)"/> on started commands, then sends <see cref="ExecResult"/> to model via <see cref="IModelClient"/></description></item>
/// </list>
/// 
/// <para><b>State Machine:</b></para>
/// <list type="bullet">
/// <item><description><b>Idle</b> - No active batch</description></item>
/// <item><description><b>Executing</b> - Batch is running, commands are being executed</description></item>
/// <item><description><b>FinishCalled</b> - Parser signaled end of batch, worker will finalize after queue drains</description></item>
/// <item><description><b>CollectingIncomingFirst</b> - A new batch started while executing; waiting for first command to decide preemption</description></item>
/// <item><description><b>IncomingIgnoring</b> - Incoming batch's first command was not STOP; ignoring all subsequent incoming commands</description></item>
/// </list>
/// 
/// <para><b>STOP-First Preemption:</b></para>
/// <para>
/// If a new batch starts while executing and its first command is "STOP":
/// - Motors are stopped immediately via <see cref="Picarx.Stop()"/>
/// - Current execution is cancelled
/// - Started commands are NOT finalized (per safety requirement)
/// - The incoming batch is promoted to executing
/// - Subsequent commands from the new batch are enqueued and executed
/// </para>
/// <para>
/// If the first incoming command is not STOP, all commands from that batch are ignored.
/// </para>
/// 
/// <para><b>Error Handling:</b></para>
/// <para>
/// If any command returns a non-OK <see cref="CommandResult"/>, execution stops immediately:
/// - Remaining commands in queue are not executed
/// - Started commands are finalized
/// - An <see cref="ExecResult"/> with failure/interruption status is sent to model
/// </para>
/// 
/// <para><b>Worker Behavior:</b></para>
/// <para>
/// The background worker continuously:
/// 1. Waits for queue signal (new command or state change)
/// 2. Drains all queued commands sequentially
/// 3. After queue empty, checks if state != Executing (or error occurred)
/// 4. If finalize condition met: awaits <see cref="ICommand.Finish(CancellationToken)"/> on all started commands, sends batch result to model
/// 5. Returns to waiting
/// </para>
/// </remarks>
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

	/// <summary>
	/// Begins a new command batch.
	/// </summary>
	/// <param name="batchId">Unique identifier for the batch from model header</param>
	/// <remarks>
	/// <para>If state is Idle, this batch becomes the executing batch.</para>
	/// <para>If state is Executing or FinishCalled, this batch becomes the "incoming" batch and waits for first command to decide preemption.</para>
	/// </remarks>
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
				_stateProvider.IsExecuting = true;
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
					_stateProvider.IsExecuting = false;
				}
				await _modelClient.SendExecResultAsync(execResult);
			}
		}
	}

	/// <summary>
	/// Enqueues a command for execution. This method does not block.
	/// </summary>
	/// <param name="command">The command specification to execute</param>
	/// <remarks>
	/// <para><b>Behavior by state:</b></para>
	/// <list type="bullet">
	/// <item><description><b>Executing</b>: Command is enqueued for the current batch</description></item>
	/// <item><description><b>CollectingIncomingFirst</b>: If command is "STOP", preempts current execution; otherwise starts ignoring incoming batch</description></item>
	/// <item><description><b>IncomingIgnoring</b>: Command is dropped</description></item>
	/// <item><description><b>Idle/FinishCalled</b>: Command is dropped with warning</description></item>
	/// </list>
	/// <para>The method wakes the background worker after enqueueing.</para>
	/// </remarks>
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
					_stateProvider.IsExecuting = true;
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

	/// <summary>
	/// Signals the end of the current batch. This method does not block.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Sets state to <see cref="ExecutorState.FinishCalled"/> if currently Executing.
	/// The background worker will finalize and send <see cref="ExecResult"/> when the queue is drained.
	/// </para>
	/// </remarks>
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
