namespace PriorityGear.Core;

public sealed record PriorityDecision(
    PriorityRule Rule,
    ProcessSnapshot Process,
    bool IsForegroundActive,
    ProcessPriorityLevel DesiredPriority)
{
    public bool ShouldApply(ManagedProcessState? state)
    {
        return state is null || state.LastAppliedPriority != DesiredPriority;
    }
}
