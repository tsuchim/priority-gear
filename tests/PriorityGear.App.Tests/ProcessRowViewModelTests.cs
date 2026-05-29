using PriorityGear.App.Runtime;
using PriorityGear.App.ViewModels;
using PriorityGear.Core;

namespace PriorityGear.App.Tests;

public sealed class ProcessRowViewModelTests
{
    [Fact]
    public void From_DisplaysRuntimeDecisionAndLastResult()
    {
        PriorityRule rule = PriorityRule.ForExecutable("notepad.exe");
        ProcessSnapshot process = new(123, "notepad.exe", @"C:\Windows\notepad.exe", ProcessPriorityLevel.Normal, ProcessCapability.ControllableNow);
        PriorityDecision decision = new(rule, process, true, ProcessPriorityLevel.AboveNormal);
        ManagedProcessState state = ManagedProcessStateUpdater.FromDecision(
            decision,
            null,
            PriorityApplyResult.Success(ProcessPriorityLevel.AboveNormal),
            new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        MonitoringSnapshot snapshot = new(
            [process],
            new Dictionary<int, ManagedProcessState> { [123] = state },
            new Dictionary<int, PriorityDecision> { [123] = decision },
            new Dictionary<int, ProcessResourceSnapshot>(),
            true,
            DateTimeOffset.Now);

        ProcessRowViewModel row = ProcessRowViewModel.From(process, snapshot);

        Assert.Equal("notepad.exe", row.MatchedRule);
        Assert.Equal(ProcessPriorityLevel.AboveNormal, row.DesiredPriority);
        Assert.True(row.Active);
        Assert.Equal(ProcessPriorityLevel.AboveNormal, row.LastAppliedPriority);
        Assert.Equal("Applied", row.LastResult);
        Assert.Equal(ProcessStatus.Applied, row.Status);
    }

    [Fact]
    public void Filter_MatchesProcessNameCaseInsensitively()
    {
        ProcessRowViewModel row = Row("Notepad.exe");

        Assert.True(ProcessListFilter.Matches(row, "note", ProcessMetricFilter.All));
        Assert.True(ProcessListFilter.Matches(row, "PAD", ProcessMetricFilter.All));
        Assert.False(ProcessListFilter.Matches(row, "calc", ProcessMetricFilter.All));
    }

    [Fact]
    public void Filter_MetricUsageRequiresMeasuredPositiveValue()
    {
        ProcessSnapshot process = new(123, "notepad.exe", null, ProcessPriorityLevel.Normal, ProcessCapability.ControllableNow);
        MonitoringSnapshot snapshot = new(
            [process],
            new Dictionary<int, ManagedProcessState>(),
            new Dictionary<int, PriorityDecision>(),
            new Dictionary<int, ProcessResourceSnapshot>
            {
                [123] = new(
                    123,
                    ResourceMetric<double>.Available(3.2),
                    ResourceMetric<double>.Unavailable(ResourceMetricStatus.Unsupported, "unsupported"),
                    ResourceMetric<long>.Available(0))
            },
            false,
            DateTimeOffset.Now);
        ProcessRowViewModel row = ProcessRowViewModel.From(process, snapshot);

        Assert.True(ProcessListFilter.Matches(row, null, ProcessMetricFilter.Cpu));
        Assert.False(ProcessListFilter.Matches(row, null, ProcessMetricFilter.Gpu));
        Assert.False(ProcessListFilter.Matches(row, null, ProcessMetricFilter.Disk));
    }

    [Fact]
    public void PrioritySelectors_AreDescendingAndActiveStartsWithSameAsNormal()
    {
        Assert.Equal(
            [ProcessPriorityLevel.High, ProcessPriorityLevel.AboveNormal, ProcessPriorityLevel.Normal, ProcessPriorityLevel.BelowNormal, ProcessPriorityLevel.Idle],
            PrioritySelectorOptions.ConcretePriorities);
        Assert.Null(PrioritySelectorOptions.ActivePriorities[0].Priority);
        Assert.Equal("Same as normal", PrioritySelectorOptions.ActivePriorities[0].Label);
    }

    private static ProcessRowViewModel Row(string name)
    {
        ProcessSnapshot process = new(123, name, null, ProcessPriorityLevel.Normal, ProcessCapability.ControllableNow);
        MonitoringSnapshot snapshot = new(
            [process],
            new Dictionary<int, ManagedProcessState>(),
            new Dictionary<int, PriorityDecision>(),
            new Dictionary<int, ProcessResourceSnapshot>(),
            false,
            DateTimeOffset.Now);
        return ProcessRowViewModel.From(process, snapshot);
    }
}
