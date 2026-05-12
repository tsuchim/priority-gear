using PriorityGear.App.Runtime;
using PriorityGear.Core;

namespace PriorityGear.App.ViewModels;

public sealed class ProcessRowViewModel
{
    public required ProcessSnapshot Process { get; init; }

    public int ProcessId => Process.ProcessId;

    public string ExecutableName => Process.ExecutableName;

    public string Path => string.IsNullOrWhiteSpace(Process.ExecutablePath) ? "(unavailable)" : Process.ExecutablePath;

    public ProcessPriorityLevel? CurrentPriority => Process.CurrentPriority;

    public string MatchedRule { get; init; } = string.Empty;

    public ProcessPriorityLevel? DesiredPriority { get; init; }

    public bool Active { get; init; }

    public ProcessPriorityLevel? LastAppliedPriority { get; init; }

    public string LastResult { get; init; } = string.Empty;

    public string LastApplyTime { get; init; } = string.Empty;

    public ProcessStatus Status { get; init; }

    public string StatusMessage { get; init; } = string.Empty;

    public static ProcessRowViewModel From(ProcessSnapshot process, MonitoringSnapshot snapshot)
    {
        snapshot.Decisions.TryGetValue(process.ProcessId, out PriorityDecision? decision);
        snapshot.States.TryGetValue(process.ProcessId, out ManagedProcessState? state);

        ProcessStatus status = process.Inspection.Status;
        if (decision is not null)
        {
            status = state?.LastApplyResult?.Succeeded == true ? ProcessStatus.Applied : ProcessStatus.Matched;
        }

        if (state?.LastApplyResult?.Succeeded == false)
        {
            status = ProcessStatus.PriorityWriteDenied;
        }

        return new ProcessRowViewModel
        {
            Process = process,
            MatchedRule = decision?.Rule.DisplayName ?? string.Empty,
            DesiredPriority = decision?.DesiredPriority,
            Active = decision?.IsForegroundActive ?? false,
            LastAppliedPriority = state?.LastAppliedPriority,
            LastResult = state?.LastApplyResult?.Message ?? string.Empty,
            LastApplyTime = state?.LastApplyTime?.ToLocalTime().ToString("HH:mm:ss") ?? string.Empty,
            Status = status,
            StatusMessage = state?.LastError ?? process.Inspection.Message ?? string.Empty
        };
    }
}
