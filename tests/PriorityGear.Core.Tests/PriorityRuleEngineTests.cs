using PriorityGear.Core;

namespace PriorityGear.Core.Tests;

public sealed class PriorityRuleEngineTests
{
    [Fact]
    public void Decide_UsesActivePriority_WhenActiveModeEnabledAndForeground()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        rule.BasePriority = ProcessPriorityLevel.BelowNormal;
        rule.ActivePriority = ProcessPriorityLevel.High;
        ProcessSnapshot process = Process(42, "sample.exe");

        PriorityDecision? decision = new PriorityRuleEngine().Decide(process, [rule], 42);

        Assert.NotNull(decision);
        Assert.True(decision.IsForegroundActive);
        Assert.Equal(ProcessPriorityLevel.High, decision.DesiredPriority);
    }

    [Fact]
    public void Decide_UsesBasePriority_WhenProcessIsNotForeground()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        rule.BasePriority = ProcessPriorityLevel.BelowNormal;
        rule.ActivePriority = ProcessPriorityLevel.High;
        ProcessSnapshot process = Process(42, "sample.exe");

        PriorityDecision? decision = new PriorityRuleEngine().Decide(process, [rule], 7);

        Assert.NotNull(decision);
        Assert.False(decision.IsForegroundActive);
        Assert.Equal(ProcessPriorityLevel.BelowNormal, decision.DesiredPriority);
    }

    [Fact]
    public void Decide_UsesBasePriority_WhenActiveModeDisabled()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        rule.BasePriority = ProcessPriorityLevel.Normal;
        rule.ActivePriority = ProcessPriorityLevel.High;
        rule.ActiveModeEnabled = false;
        ProcessSnapshot process = Process(42, "sample.exe");

        PriorityDecision? decision = new PriorityRuleEngine().Decide(process, [rule], 42);

        Assert.NotNull(decision);
        Assert.True(decision.IsForegroundActive);
        Assert.Equal(ProcessPriorityLevel.Normal, decision.DesiredPriority);
    }

    [Fact]
    public void ShouldApply_ReturnsFalse_WhenLastAppliedPriorityMatchesDesiredPriority()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        ProcessSnapshot process = Process(42, "sample.exe");
        PriorityDecision decision = new(rule, process, false, ProcessPriorityLevel.Normal);
        ManagedProcessState state = new()
        {
            ProcessId = 42,
            ExecutablePath = @"C:\Tools\sample.exe",
            RuleId = rule.Id,
            LastAppliedPriority = ProcessPriorityLevel.Normal
        };

        Assert.False(decision.ShouldApply(state));
    }

    private static ProcessSnapshot Process(int processId, string executableName)
    {
        return new ProcessSnapshot(
            processId,
            executableName,
            $@"C:\Tools\{executableName}",
            ProcessPriorityLevel.Normal,
            ProcessCapability.ControllableNow);
    }
}
