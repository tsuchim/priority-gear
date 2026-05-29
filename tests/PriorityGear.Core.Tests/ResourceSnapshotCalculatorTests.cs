using PriorityGear.Core;

namespace PriorityGear.Core.Tests;

public sealed class ResourceSnapshotCalculatorTests
{
    [Fact]
    public void Calculate_UsesCpuTimeDeltaAcrossLogicalProcessors()
    {
        ProcessResourceSnapshot snapshot = ResourceSnapshotCalculator.Calculate(
            new ResourceSample(10, TimeSpan.FromSeconds(1), 100, ResourceMetricStatus.Available, null),
            new ResourceSample(10, TimeSpan.FromSeconds(4), 3100, ResourceMetricStatus.Available, null),
            TimeSpan.FromSeconds(3),
            logicalProcessorCount: 2);

        Assert.Equal(50d, snapshot.CpuPercent.Value);
        Assert.Equal(1000, snapshot.DiskBytesPerSecond.Value);
    }

    [Fact]
    public void Calculate_ProcessExitedDoesNotReportFakeZero()
    {
        ProcessResourceSnapshot snapshot = ResourceSnapshotCalculator.Calculate(
            new ResourceSample(10, TimeSpan.FromSeconds(1), 100, ResourceMetricStatus.Available, null),
            new ResourceSample(10, null, null, ResourceMetricStatus.ProcessExited, "exited"),
            TimeSpan.FromSeconds(3),
            logicalProcessorCount: 2);

        Assert.False(snapshot.CpuPercent.HasValue);
        Assert.Equal(ResourceMetricStatus.ProcessExited, snapshot.CpuPercent.Status);
        Assert.False(snapshot.DiskBytesPerSecond.HasValue);
    }

    [Fact]
    public void Calculate_MissingDiskCounterKeepsDiskUnavailable()
    {
        ProcessResourceSnapshot snapshot = ResourceSnapshotCalculator.Calculate(
            new ResourceSample(10, TimeSpan.FromSeconds(1), null, ResourceMetricStatus.Available, null),
            new ResourceSample(10, TimeSpan.FromSeconds(2), null, ResourceMetricStatus.Available, null),
            TimeSpan.FromSeconds(1),
            logicalProcessorCount: 1);

        Assert.True(snapshot.CpuPercent.HasValue);
        Assert.False(snapshot.DiskBytesPerSecond.HasValue);
        Assert.Equal(ResourceMetricStatus.Unavailable, snapshot.DiskBytesPerSecond.Status);
    }
}
