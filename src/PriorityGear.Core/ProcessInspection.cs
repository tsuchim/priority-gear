namespace PriorityGear.Core;

public sealed record ProcessInspection(
    bool ProcessIdentityAvailable,
    bool ExecutablePathAvailable,
    bool CurrentPriorityReadable,
    bool PriorityWriteLikelyPossible,
    ProcessStatus Status,
    string? Message = null)
{
    public static ProcessInspection Ready { get; } = new(true, true, true, true, ProcessStatus.Ready);
}
