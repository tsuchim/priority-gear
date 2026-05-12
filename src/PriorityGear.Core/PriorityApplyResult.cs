namespace PriorityGear.Core;

public sealed record PriorityApplyResult(
    bool Succeeded,
    ProcessPriorityLevel RequestedPriority,
    string Message,
    string? ErrorCode = null)
{
    public static PriorityApplyResult Success(ProcessPriorityLevel priority)
    {
        return new PriorityApplyResult(true, priority, "Applied");
    }

    public static PriorityApplyResult Failure(ProcessPriorityLevel priority, string message, string? errorCode = null)
    {
        return new PriorityApplyResult(false, priority, message, errorCode);
    }
}
