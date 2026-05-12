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
}
