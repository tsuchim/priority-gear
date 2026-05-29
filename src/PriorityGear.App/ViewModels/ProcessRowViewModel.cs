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

    public string CpuSnapshot { get; init; } = "Not sampled";

    public double? CpuSnapshotValue { get; init; }

    public string GpuSnapshot { get; init; } = "Not sampled";

    public double? GpuSnapshotValue { get; init; }

    public string DiskSnapshot { get; init; } = "Not sampled";

    public long? DiskSnapshotValue { get; init; }

    public bool HasCpuUsage => CpuSnapshotValue is > 0;

    public bool HasGpuUsage => GpuSnapshotValue is > 0;

    public bool HasDiskUsage => DiskSnapshotValue is > 0;

    public static ProcessRowViewModel From(ProcessSnapshot process, MonitoringSnapshot snapshot)
    {
        snapshot.Decisions.TryGetValue(process.ProcessId, out PriorityDecision? decision);
        snapshot.States.TryGetValue(process.ProcessId, out ManagedProcessState? state);
        snapshot.Resources.TryGetValue(process.ProcessId, out ProcessResourceSnapshot? resources);

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
            StatusMessage = state?.LastError ?? process.Inspection.Message ?? string.Empty,
            CpuSnapshot = FormatPercent(resources?.CpuPercent),
            CpuSnapshotValue = resources?.CpuPercent.Value,
            GpuSnapshot = FormatPercent(resources?.GpuPercent),
            GpuSnapshotValue = resources?.GpuPercent.Value,
            DiskSnapshot = FormatBytesPerSecond(resources?.DiskBytesPerSecond),
            DiskSnapshotValue = resources?.DiskBytesPerSecond.Value
        };
    }

    private static string FormatPercent(ResourceMetric<double>? metric)
    {
        if (metric is null)
        {
            return "Not sampled";
        }

        return metric.HasValue ? $"{metric.Value.GetValueOrDefault():0.0}%" : $"{metric.Status}: {metric.Message}";
    }

    private static string FormatBytesPerSecond(ResourceMetric<long>? metric)
    {
        if (metric is null)
        {
            return "Not sampled";
        }

        if (!metric.HasValue)
        {
            return $"{metric.Status}: {metric.Message}";
        }

        long value = metric.Value.GetValueOrDefault();
        return value switch
        {
            >= 1024L * 1024L => $"{value / 1024d / 1024d:0.0} MB/s",
            >= 1024L => $"{value / 1024d:0.0} KB/s",
            _ => $"{value} B/s"
        };
    }
}
