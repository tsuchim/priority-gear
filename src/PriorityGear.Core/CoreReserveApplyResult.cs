namespace PriorityGear.Core;

public sealed record CoreReserveApplyResult(bool Succeeded, int RequestedReserve, string Message, string? ErrorCode = null)
{
    public static CoreReserveApplyResult Disabled() => new(true, 0, "Core Reserve disabled.");

    public static CoreReserveApplyResult Success(int requestedReserve, string message) => new(true, requestedReserve, message);

    public static CoreReserveApplyResult Failure(int requestedReserve, string message, string? errorCode = null) =>
        new(false, requestedReserve, message, errorCode);
}
