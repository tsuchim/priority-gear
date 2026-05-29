namespace PriorityGear.App.ViewModels;

public enum ProcessMetricFilter
{
    All,
    Cpu,
    Gpu,
    Disk
}

public static class ProcessListFilter
{
    public static bool Matches(ProcessRowViewModel row, string? processNameFilter, ProcessMetricFilter metricFilter)
    {
        if (!string.IsNullOrWhiteSpace(processNameFilter) &&
            !row.ExecutableName.Contains(processNameFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return metricFilter switch
        {
            ProcessMetricFilter.Cpu => row.HasCpuUsage,
            ProcessMetricFilter.Gpu => row.HasGpuUsage,
            ProcessMetricFilter.Disk => row.HasDiskUsage,
            _ => true
        };
    }
}
