namespace SmartCar.ChatGpt;

public enum ExecStatus
{
    OK,
    INTERRUPTED,
    FAILED,
    IGNORED
}

public enum ExecReason
{
    NONE,
    OBSTACLE,
    USER_STOP,
    SAFETY,
    PARSE_ERROR,
    INTERNAL_ERROR
}

public class ExecResult
{
    public int BatchId { get; set; }
    public ExecStatus Status { get; set; }
    public ExecReason Reason { get; set; }
}
