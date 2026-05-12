namespace PriorityGear.Core;

public sealed class ManagedProcessState
{
    public required int ProcessId { get; init; }

    public required string ExecutablePath { get; init; }

    public required Guid RuleId { get; init; }

    public ProcessPriorityLevel CurrentDesiredPriority { get; set; }

    public ProcessPriorityLevel? LastAppliedPriority { get; set; }

    public PriorityApplyResult? LastApplyResult { get; set; }

    public DateTimeOffset? LastApplyTime { get; set; }

    public string? LastError { get; set; }

    public bool IsForegroundActive { get; set; }
}
