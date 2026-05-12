using PriorityGear.Core;

namespace PriorityGear.Core.Tests;

public sealed class PriorityRuleEngineTests
{
    [Fact]
    public void Decide_IgnoresDisabledRules()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        rule.Enabled = false;

        PriorityDecision? decision = new PriorityRuleEngine().Decide(Process(42, "sample.exe"), [rule], null);

        Assert.Null(decision);
    }

    [Fact]
    public void Decide_IgnoresUnsupportedScopesInUserMode()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        rule.Scope = RuleScope.Machine;

        PriorityDecision? decision = new PriorityRuleEngine().Decide(Process(42, "sample.exe"), [rule], null);

        Assert.Null(decision);
    }

    [Fact]
    public void Decide_MatchesExecutableNameCaseInsensitive()
    {
        PriorityRule rule = PriorityRule.ForExecutable("SAMPLE.EXE");

        PriorityDecision? decision = new PriorityRuleEngine().Decide(Process(42, "sample.exe"), [rule], null);

        Assert.NotNull(decision);
    }

    [Fact]
    public void Decide_MatchesFullPath()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe", @"C:\Tools\sample.exe");

        PriorityDecision? decision = new PriorityRuleEngine().Decide(Process(42, "sample.exe"), [rule], null);

        Assert.NotNull(decision);
    }

    [Fact]
    public void Decide_ExecutableNameMatchWorksWhenFullPathIsMissing()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        ProcessSnapshot process = new(42, "sample.exe", null, ProcessPriorityLevel.Normal, ProcessCapability.ControllableNow);

        PriorityDecision? decision = new PriorityRuleEngine().Decide(process, [rule], null);

        Assert.NotNull(decision);
    }

    [Fact]
    public void Decide_MatchesPathSuffix()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        rule.Match.PathSuffix = @"Tools\sample.exe";

        PriorityDecision? decision = new PriorityRuleEngine().Decide(Process(42, "sample.exe"), [rule], null);

        Assert.NotNull(decision);
    }

    [Fact]
    public void Decide_FirstMatchingEnabledRuleWins()
    {
        PriorityRule first = PriorityRule.ForExecutable("sample.exe");
        first.BasePriority = ProcessPriorityLevel.Idle;
        PriorityRule second = PriorityRule.ForExecutable("sample.exe");
        second.BasePriority = ProcessPriorityLevel.High;

        PriorityDecision? decision = new PriorityRuleEngine().Decide(Process(42, "sample.exe"), [first, second], null);

        Assert.NotNull(decision);
        Assert.Equal(first.Id, decision.Rule.Id);
        Assert.Equal(ProcessPriorityLevel.Idle, decision.DesiredPriority);
    }

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

    [Fact]
    public void StateUpdater_DoesNotUpdateLastAppliedPriorityOnFailure()
    {
        PriorityRule rule = PriorityRule.ForExecutable("sample.exe");
        PriorityDecision decision = new(rule, Process(42, "sample.exe"), false, ProcessPriorityLevel.High);
        ManagedProcessState state = new()
        {
            ProcessId = 42,
            ExecutablePath = @"C:\Tools\sample.exe",
            RuleId = rule.Id,
            LastAppliedPriority = ProcessPriorityLevel.Normal
        };

        ManagedProcessState updated = ManagedProcessStateUpdater.FromDecision(
            decision,
            state,
            PriorityApplyResult.Failure(ProcessPriorityLevel.High, "Denied", "Denied"),
            DateTimeOffset.Now);

        Assert.Equal(ProcessPriorityLevel.High, updated.LastAttemptedPriority);
        Assert.Equal(ProcessPriorityLevel.Normal, updated.LastAppliedPriority);
        Assert.False(updated.LastApplyResult!.Succeeded);
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
