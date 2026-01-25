using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SmartCar.ChatGpt;

namespace SmartCar.ChatGpt;

public class ChatResponseParser
{
    private readonly StringBuilder _builder = new();
    private readonly ILogger<ChatResponseParser> _logger;
    private readonly ICommandExecutor _executor;
    private int _currentBatchId = -1;
    private bool _ignoreUntilHeader = false;
    private bool _continue;

    private static readonly Regex HeaderRegex = new(@"^\[COMMANDS\s+id=(\d+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ChatResponseParser(
        ICommandExecutor executor,
        ILogger<ChatResponseParser> logger
        )
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task Add(string text)
    {
        _builder.Append(text);
        if (text.Contains('\n'))
        {
            var lines = _builder.ToString().Split('\n');
            _builder.Clear();
            for (int i = 0; i < lines.Length - 1; i++)
            {
                await ProcessLine(lines[i]);
            }
            _builder.Append(lines[^1]);
        }
    }

    public async Task<bool> Finish()
    {
        var lines = _builder.ToString().Split('\n');
        _builder.Clear();
        foreach (var line in lines)
        {
            await ProcessLine(line);
        }

        if (_currentBatchId != -1)
        {
            try
            {
                await _executor.FinishBatchAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing batch {BatchId}", _currentBatchId);
            }
            finally
            {
                _currentBatchId = -1;
                _ignoreUntilHeader = false;
            }
        }

        var returnContinue = _continue;
        _continue = false;
        return returnContinue;
    }

    private async Task ProcessLine(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return;

        _logger.LogInformation(v);
        try
        {
            var trimmed = v.Trim();

            // Header
            var m = HeaderRegex.Match(trimmed);
            if (m.Success)
            {
                if (int.TryParse(m.Groups[1].Value, out var batchId))
                {
                    _currentBatchId = batchId;
                    _ignoreUntilHeader = false;
                    await _executor.StartBatchAsync(batchId, CancellationToken.None);
                }
                else
                {
                    _logger.LogError("Invalid batch id in header: {Header}", v);
                    _ignoreUntilHeader = true;
                }
                return;
            }

            if (_ignoreUntilHeader) return;

            // Command lines
            if (trimmed.StartsWith('>'))
            {
                if (_currentBatchId == -1)
                {
                    _logger.LogError("Command received without header: {Line}", v);
                    _ignoreUntilHeader = true;
                    return;
                }

                var parts = trimmed.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return;
                var name = parts[0].ToUpperInvariant();
                var args = parts.Skip(1).ToArray();

                if (name == "CONTINUE")
                {
                    _continue = true;
                    return;
                }

                var spec = new CommandSpec
                {
                    Name = name,
                    Args = args
                };

                try
                {
                    await _executor.EnqueueCommandAsync(spec, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enqueueing command {Name}", name);
                }

                return;
            }

            // Non-command text -> stop parsing until next header
            _ignoreUntilHeader = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing line");
        }
    }

}
