using PriorityGear.App.Runtime;
using PriorityGear.Core;

namespace PriorityGear.App.Tests;

public sealed class MonitoringControllerTests
{
    [Fact]
    public void Tick_AppliesWhenStateChanges_AndSkipsUnchangedPriority()
    {
        FakeProcessSource processes = new([Process(10, "notepad.exe")]);
        FakePriorityApplier applier = new();
        FakeForegroundSource foreground = new(null);
        MonitoringController controller = CreateController(processes, applier, foreground);
        controller.SetRules([Rule("notepad.exe", ProcessPriorityLevel.BelowNormal, ProcessPriorityLevel.AboveNormal)]);

        controller.Start(Time(0));
        controller.Tick(Time(1));

        Assert.Single(applier.Calls);
        Assert.Equal(ProcessPriorityLevel.BelowNormal, applier.Calls[0].Priority);
    }

    [Fact]
    public void Tick_ForegroundChangeChangesDesiredPriority()
    {
        FakeProcessSource processes = new([Process(10, "notepad.exe")]);
        FakePriorityApplier applier = new();
        FakeForegroundSource foreground = new(null);
        MonitoringController controller = CreateController(processes, applier, foreground);
        controller.SetRules([Rule("notepad.exe", ProcessPriorityLevel.BelowNormal, ProcessPriorityLevel.AboveNormal)]);

        controller.Start(Time(0));
        foreground.ProcessId = 10;
        MonitoringSnapshot snapshot = controller.Tick(Time(1));

        Assert.Equal(2, applier.Calls.Count);
        Assert.Equal(ProcessPriorityLevel.AboveNormal, snapshot.Decisions[10].DesiredPriority);
        Assert.True(snapshot.Decisions[10].IsForegroundActive);
    }

    [Fact]
    public void Refresh_ProcessExitRemovesRuntimeState()
    {
        FakeProcessSource processes = new([Process(10, "notepad.exe")]);
        FakePriorityApplier applier = new();
        MonitoringController controller = CreateController(processes, applier, new FakeForegroundSource(null));
        controller.SetRules([Rule("notepad.exe", ProcessPriorityLevel.Normal, ProcessPriorityLevel.AboveNormal)]);

        controller.Start(Time(0));
        processes.Processes = [];
        MonitoringSnapshot snapshot = controller.Refresh(Time(20));

        Assert.Empty(snapshot.States);
    }

    [Fact]
    public void DeniedProcessProducesVisibleStatusAndNoApply()
    {
        ProcessSnapshot denied = Process(10, "notepad.exe") with
        {
            Inspection = new ProcessInspection(true, true, true, false, ProcessStatus.PriorityWriteDenied, "Denied")
        };
        FakePriorityApplier applier = new();
        MonitoringController controller = CreateController(new FakeProcessSource([denied]), applier, new FakeForegroundSource(null));
        controller.SetRules([Rule("notepad.exe", ProcessPriorityLevel.Normal, ProcessPriorityLevel.AboveNormal)]);

        MonitoringSnapshot snapshot = controller.Start(Time(0));

        Assert.Empty(applier.Calls);
        Assert.Equal(ProcessStatus.PriorityWriteDenied, snapshot.Processes[0].Inspection.Status);
    }

    [Fact]
    public void RepeatedIdenticalFailureIsThrottled()
    {
        FakePriorityApplier applier = new()
        {
            Result = PriorityApplyResult.Failure(ProcessPriorityLevel.Normal, "Denied", "Denied")
        };
        MonitoringController controller = CreateController(new FakeProcessSource([Process(10, "notepad.exe")]), applier, new FakeForegroundSource(null));
        controller.SetRules([Rule("notepad.exe", ProcessPriorityLevel.Normal, ProcessPriorityLevel.AboveNormal)]);
        List<MonitoringLogEntry> logs = [];
        controller.LogProduced += (_, entry) => logs.Add(entry);

        controller.Start(Time(0));
        controller.Tick(Time(1));
        controller.Tick(Time(2));

        Assert.Single(logs, static l => l.Category == "failure");
    }

    [Fact]
    public void RuleChangeIsPickedUpWithoutRestart()
    {
        FakePriorityApplier applier = new();
        MonitoringController controller = CreateController(new FakeProcessSource([Process(10, "notepad.exe")]), applier, new FakeForegroundSource(null));
        controller.SetRules([]);
        controller.Start(Time(0));

        controller.SetRules([Rule("notepad.exe", ProcessPriorityLevel.High, ProcessPriorityLevel.AboveNormal)]);
        controller.Refresh(Time(1));

        Assert.Single(applier.Calls);
        Assert.Equal(ProcessPriorityLevel.High, applier.Calls[0].Priority);
    }

    private static MonitoringController CreateController(
        FakeProcessSource processes,
        FakePriorityApplier applier,
        FakeForegroundSource foreground)
    {
        return new MonitoringController(
            processes,
            applier,
            foreground,
            new MonitoringOptions(TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10)));
    }

    private static DateTimeOffset Time(int seconds)
    {
        return new DateTimeOffset(2026, 1, 1, 0, 0, seconds, TimeSpan.Zero);
    }

    private static PriorityRule Rule(string name, ProcessPriorityLevel basePriority, ProcessPriorityLevel activePriority)
    {
        PriorityRule rule = PriorityRule.ForExecutable(name);
        rule.BasePriority = basePriority;
        rule.ActivePriority = activePriority;
        return rule;
    }

    private static ProcessSnapshot Process(int pid, string name)
    {
        return new ProcessSnapshot(pid, name, $@"C:\Temp\{name}", ProcessPriorityLevel.Normal, ProcessCapability.ControllableNow);
    }

    private sealed class FakeProcessSource(IReadOnlyList<ProcessSnapshot> processes) : IProcessSource
    {
        public IReadOnlyList<ProcessSnapshot> Processes { get; set; } = processes;

        public IReadOnlyList<ProcessSnapshot> GetProcesses()
        {
            return Processes;
        }
    }

    private sealed class FakeForegroundSource(int? processId) : IForegroundProcessSource
    {
        public int? ProcessId { get; set; } = processId;

        public int? GetForegroundProcessId()
        {
            return ProcessId;
        }
    }

    private sealed class FakePriorityApplier : IPriorityApplier
    {
        public List<(int ProcessId, ProcessPriorityLevel Priority)> Calls { get; } = [];

        public PriorityApplyResult Result { get; set; } = PriorityApplyResult.Success(ProcessPriorityLevel.Normal);

        public PriorityApplyResult SetPriority(int processId, ProcessPriorityLevel priority)
        {
            Calls.Add((processId, priority));
            return Result.Succeeded ? PriorityApplyResult.Success(priority) : PriorityApplyResult.Failure(priority, Result.Message, Result.ErrorCode);
        }
    }
}
