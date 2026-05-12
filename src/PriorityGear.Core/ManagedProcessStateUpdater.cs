namespace PriorityGear.Core;

public static class ManagedProcessStateUpdater
{
    public static ManagedProcessState FromDecision(
        PriorityDecision decision,
        ManagedProcessState? existing,
        PriorityApplyResult result,
        DateTimeOffset appliedAt)
    {
        ManagedProcessState state = existing ?? new ManagedProcessState
        {
            ProcessId = decision.Process.ProcessId,
            ExecutablePath = decision.Process.ExecutablePath,
            RuleId = decision.Rule.Id
        };

        state.CurrentDesiredPriority = decision.DesiredPriority;
        state.LastAttemptedPriority = decision.DesiredPriority;
        state.IsForegroundActive = decision.IsForegroundActive;
        state.LastApplyResult = result;
        state.LastApplyTime = appliedAt;
        state.LastError = result.Succeeded ? null : result.Message;

        if (result.Succeeded)
        {
            state.LastAppliedPriority = decision.DesiredPriority;
        }

        return state;
    }
}
