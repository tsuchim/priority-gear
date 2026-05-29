namespace PriorityGear.Core;

public enum ResourceMetricStatus
{
    Available,
    Unavailable,
    AccessDenied,
    ProcessExited,
    Unsupported,
    Error
}

public sealed record ResourceMetric<T>(ResourceMetricStatus Status, T? Value, string? Message)
    where T : struct
{
    public bool HasValue => Status == ResourceMetricStatus.Available && Value.HasValue;

    public static ResourceMetric<T> Available(T value) => new(ResourceMetricStatus.Available, value, null);

    public static ResourceMetric<T> Unavailable(ResourceMetricStatus status, string message) => new(status, null, message);
}

public sealed record ProcessResourceSnapshot(
    int ProcessId,
    ResourceMetric<double> CpuPercent,
    ResourceMetric<double> GpuPercent,
    ResourceMetric<long> DiskBytesPerSecond);

public sealed record ResourceSample(
    int ProcessId,
    TimeSpan? TotalProcessorTime,
    long? DiskBytes,
    ResourceMetricStatus Status,
    string? Message);

public static class ResourceSnapshotCalculator
{
    public static ProcessResourceSnapshot Calculate(
        ResourceSample start,
        ResourceSample end,
        TimeSpan interval,
        int logicalProcessorCount,
        ResourceMetric<double>? gpuPercent = null)
    {
        if (end.Status != ResourceMetricStatus.Available)
        {
            return new ProcessResourceSnapshot(
                end.ProcessId,
                ResourceMetric<double>.Unavailable(end.Status, end.Message ?? "CPU unavailable."),
                gpuPercent ?? ResourceMetric<double>.Unavailable(ResourceMetricStatus.Unsupported, "GPU attribution is unsupported."),
                ResourceMetric<long>.Unavailable(end.Status, end.Message ?? "Disk I/O unavailable."));
        }

        if (start.Status != ResourceMetricStatus.Available)
        {
            return new ProcessResourceSnapshot(
                end.ProcessId,
                ResourceMetric<double>.Unavailable(start.Status, start.Message ?? "Start sample unavailable."),
                gpuPercent ?? ResourceMetric<double>.Unavailable(ResourceMetricStatus.Unsupported, "GPU attribution is unsupported."),
                ResourceMetric<long>.Unavailable(start.Status, start.Message ?? "Start sample unavailable."));
        }

        double seconds = interval.TotalSeconds;
        if (seconds <= 0 || logicalProcessorCount <= 0)
        {
            return new ProcessResourceSnapshot(
                end.ProcessId,
                ResourceMetric<double>.Unavailable(ResourceMetricStatus.Error, "Invalid sampling interval or processor count."),
                gpuPercent ?? ResourceMetric<double>.Unavailable(ResourceMetricStatus.Unsupported, "GPU attribution is unsupported."),
                ResourceMetric<long>.Unavailable(ResourceMetricStatus.Error, "Invalid sampling interval."));
        }

        ResourceMetric<double> cpu = start.TotalProcessorTime.HasValue && end.TotalProcessorTime.HasValue
            ? ResourceMetric<double>.Available(Math.Max(0, (end.TotalProcessorTime.Value - start.TotalProcessorTime.Value).TotalSeconds / seconds / logicalProcessorCount * 100d))
            : ResourceMetric<double>.Unavailable(ResourceMetricStatus.Unavailable, "CPU time was unavailable.");

        ResourceMetric<long> disk = start.DiskBytes.HasValue && end.DiskBytes.HasValue
            ? ResourceMetric<long>.Available(Math.Max(0, (long)Math.Round((end.DiskBytes.Value - start.DiskBytes.Value) / seconds)))
            : ResourceMetric<long>.Unavailable(ResourceMetricStatus.Unavailable, "Disk I/O counters were unavailable.");

        return new ProcessResourceSnapshot(
            end.ProcessId,
            cpu,
            gpuPercent ?? ResourceMetric<double>.Unavailable(ResourceMetricStatus.Unsupported, "GPU attribution is unsupported."),
            disk);
    }
}
