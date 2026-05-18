using PriorityGear.Setup;

namespace PriorityGear.Setup.Tests;

public sealed class InstallerStateMachineTests
{
    private static readonly SetupInstallPlan Plan = SetupInstallPlan.Create("v0.3.0");

    [Fact]
    public void PayloadMissingFailsBeforeServiceConfiguration()
    {
        FakeInstallerExecutor executor = new() { ValidatePayloadFailure = "payload missing" };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("payload missing", result.Message);
        Assert.DoesNotContain(InstallerStep.ConfigureService, result.CompletedSteps);
    }

    [Fact]
    public void ServiceStartFailureIsNotSuccess()
    {
        FakeInstallerExecutor executor = new() { StartServiceFailure = "start timeout" };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("start timeout", result.Message);
        Assert.DoesNotContain(InstallerStep.VerifyStatusPipe, result.CompletedSteps);
    }

    [Fact]
    public void ServiceStopFailureIsNotSuccess()
    {
        FakeInstallerExecutor executor = new() { StopServiceFailure = "stop denied" };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("stop denied", result.Message);
        Assert.DoesNotContain(InstallerStep.CopyPayload, result.CompletedSteps);
    }

    [Fact]
    public void PartialCopyFailureIsNotSuccess()
    {
        FakeInstallerExecutor executor = new() { CopyPayloadFailure = "copy failed halfway" };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("copy failed halfway", result.Message);
        Assert.DoesNotContain(InstallerStep.ConfigureService, result.CompletedSteps);
    }

    [Fact]
    public void ServiceConfigureFailureIsNotSuccess()
    {
        FakeInstallerExecutor executor = new() { ConfigureServiceFailure = "create service failed" };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("create service failed", result.Message);
        Assert.DoesNotContain(InstallerStep.StartService, result.CompletedSteps);
    }

    [Fact]
    public void StatusPipeTimeoutIsNotSuccess()
    {
        FakeInstallerExecutor executor = new() { QueryStatusFailure = "status pipe timeout" };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("status pipe timeout", result.Message);
    }

    [Fact]
    public void NonLocalSystemAccountFails()
    {
        FakeInstallerExecutor executor = new()
        {
            Status = new InstallerStatus(true, "LocalService", "NT AUTHORITY\\LOCAL SERVICE", Plan.ServiceExePath)
        };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("LocalSystem", result.Message);
        Assert.DoesNotContain(InstallerStep.VerifyServiceConfiguration, result.CompletedSteps);
    }

    [Fact]
    public void StaleServiceBinaryPathFails()
    {
        FakeInstallerExecutor executor = new()
        {
            Status = new InstallerStatus(true, "LocalSystem", "NT AUTHORITY\\SYSTEM", @"C:\Program Files\PriorityGear\versions\v0.2.1\PriorityGear.Service.exe")
        };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("new version directory", result.Message);
    }

    [Fact]
    public void ServiceBinaryPathRequiresExactExecutablePath()
    {
        string hostilePath = $"C:\\Windows\\System32\\cmd.exe /c \"{Plan.ServiceExePath}\"";

        Assert.False(InstallerStateMachine.ServiceBinaryPathMatches(hostilePath, Plan.ServiceExePath));
        Assert.True(InstallerStateMachine.ServiceBinaryPathMatches($"\"{Plan.ServiceExePath}\" --service", Plan.ServiceExePath));
    }

    [Fact]
    public void OldVersionCleanupFailureIsWarningOnly()
    {
        FakeInstallerExecutor executor = new() { CleanupOldVersionsFailure = "locked old version" };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.True(result.Succeeded);
        Assert.Contains(result.Warnings, warning => warning.Contains("locked old version", StringComparison.Ordinal));
    }

    [Fact]
    public void SuccessfulInstallRequiresStatusIdentity()
    {
        FakeInstallerExecutor executor = new()
        {
            Status = new InstallerStatus(true, "LocalSystem", string.Empty, Plan.ServiceExePath)
        };

        InstallerRunResult result = new InstallerStateMachine(Plan, executor).InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("process identity", result.Message);
        Assert.DoesNotContain(InstallerStep.VerifyServiceConfiguration, result.CompletedSteps);
    }

    [Fact]
    public void ReturnedResultDoesNotExposeMutableStepLists()
    {
        InstallerStateMachine stateMachine = new(Plan, new FakeInstallerExecutor());
        InstallerRunResult first = stateMachine.InstallOrUpdate();

        _ = stateMachine.InstallOrUpdate();

        Assert.Equal(
            new[]
            {
                InstallerStep.ValidatePayload,
                InstallerStep.StopExistingService,
                InstallerStep.CopyPayload,
                InstallerStep.ConfigureService,
                InstallerStep.StartService,
                InstallerStep.VerifyStatusPipe,
                InstallerStep.VerifyServiceConfiguration,
                InstallerStep.CleanupOldVersions
            },
            first.CompletedSteps);
    }

    [Fact]
    public void SuccessEmitsStepStartAndCompleteInOrder()
    {
        InstallerStateMachine stateMachine = new(Plan, new FakeInstallerExecutor());
        List<InstallerProgress> progress = [];
        stateMachine.Progress += progress.Add;

        InstallerRunResult result = stateMachine.InstallOrUpdate();

        Assert.True(result.Succeeded);
        Assert.Equal(InstallerProgressKind.Starting, progress[0].Kind);
        Assert.Equal(InstallerStep.ValidatePayload, progress[0].Step);
        Assert.Contains(progress, p => p.Kind == InstallerProgressKind.Completed && p.Step == InstallerStep.CleanupOldVersions);
    }

    [Fact]
    public void FailureEmitsFailedStepAndStopsLaterCompletion()
    {
        InstallerStateMachine stateMachine = new(Plan, new FakeInstallerExecutor { ConfigureServiceFailure = "create failed" });
        List<InstallerProgress> progress = [];
        stateMachine.Progress += progress.Add;

        InstallerRunResult result = stateMachine.InstallOrUpdate();

        Assert.False(result.Succeeded);
        Assert.Contains("ConfigureService failed", result.Message);
        Assert.Contains(progress, p => p.Kind == InstallerProgressKind.Failed && p.Step == InstallerStep.ConfigureService);
        Assert.DoesNotContain(progress, p => p.Kind == InstallerProgressKind.Completed && p.Step == InstallerStep.StartService);
    }

    [Fact]
    public void OldVersionCleanupFailureEmitsWarningAndSuccess()
    {
        InstallerStateMachine stateMachine = new(Plan, new FakeInstallerExecutor { CleanupOldVersionsFailure = "locked" });
        List<InstallerProgress> progress = [];
        stateMachine.Progress += progress.Add;

        InstallerRunResult result = stateMachine.InstallOrUpdate();

        Assert.True(result.Succeeded);
        Assert.Contains(progress, p => p.Kind == InstallerProgressKind.Warning && p.Step == InstallerStep.CleanupOldVersions);
    }

    private sealed class FakeInstallerExecutor : IInstallerExecutor
    {
        public string? ValidatePayloadFailure { get; init; }

        public string? StopServiceFailure { get; init; }

        public string? CopyPayloadFailure { get; init; }

        public string? ConfigureServiceFailure { get; init; }

        public string? StartServiceFailure { get; init; }

        public string? QueryStatusFailure { get; init; }

        public string? CleanupOldVersionsFailure { get; init; }

        public InstallerStatus Status { get; init; } = new(true, "LocalSystem", "NT AUTHORITY\\SYSTEM", Plan.ServiceExePath);

        public void ValidatePayload() => ThrowIfSet(ValidatePayloadFailure);

        public void StopExistingService() => ThrowIfSet(StopServiceFailure);

        public void CopyPayload() => ThrowIfSet(CopyPayloadFailure);

        public void ConfigureService() => ThrowIfSet(ConfigureServiceFailure);

        public void StartService() => ThrowIfSet(StartServiceFailure);

        public InstallerStatus QueryStatus()
        {
            ThrowIfSet(QueryStatusFailure);
            return Status;
        }

        public void CleanupOldVersions() => ThrowIfSet(CleanupOldVersionsFailure);

        private static void ThrowIfSet(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
