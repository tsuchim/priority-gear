namespace PriorityGear.Setup;

public enum InstallerStep
{
    ValidatePayload,
    StopExistingService,
    CopyPayload,
    ConfigureService,
    StartService,
    VerifyServiceConfiguration,
    VerifyStatusPipe,
    CleanupOldVersions
}

public sealed record InstallerStatus(
    bool ServiceRunning,
    string ConfiguredServiceAccount,
    string ProcessIdentity,
    string ServiceBinaryPath);

public sealed record InstallerRunResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<InstallerStep> CompletedSteps,
    IReadOnlyList<string> Warnings);

public interface IInstallerExecutor
{
    void ValidatePayload();

    void StopExistingService();

    void CopyPayload();

    void ConfigureService();

    void StartService();

    InstallerStatus QueryStatus();

    void CleanupOldVersions();
}

public sealed class InstallerStateMachine(SetupInstallPlan plan, IInstallerExecutor executor)
{
    private readonly List<InstallerStep> _completedSteps = [];
    private readonly List<string> _warnings = [];

    public InstallerRunResult InstallOrUpdate()
    {
        try
        {
            RunRequired(InstallerStep.ValidatePayload, executor.ValidatePayload);
            RunRequired(InstallerStep.StopExistingService, executor.StopExistingService);
            RunRequired(InstallerStep.CopyPayload, executor.CopyPayload);
            RunRequired(InstallerStep.ConfigureService, executor.ConfigureService);
            RunRequired(InstallerStep.StartService, executor.StartService);

            InstallerStatus status = RunStatusCheck();
            if (!status.ServiceRunning)
            {
                return Fail("Service status reports that the service is not running.");
            }

            if (!string.Equals(status.ConfiguredServiceAccount, "LocalSystem", StringComparison.OrdinalIgnoreCase))
            {
                return Fail($"Service account is not LocalSystem: {status.ConfiguredServiceAccount}");
            }

            if (string.IsNullOrWhiteSpace(status.ProcessIdentity))
            {
                return Fail("Service status did not report process identity.");
            }

            if (!status.ServiceBinaryPath.Contains(plan.ServiceExePath, StringComparison.OrdinalIgnoreCase))
            {
                return Fail($"Service binary path does not point at the new version directory: {status.ServiceBinaryPath}");
            }

            try
            {
                executor.CleanupOldVersions();
                _completedSteps.Add(InstallerStep.CleanupOldVersions);
            }
            catch (Exception ex)
            {
                _warnings.Add($"Old version cleanup failed: {ex.Message}");
            }

            return new InstallerRunResult(true, "Install/update completed.", _completedSteps, _warnings);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private InstallerStatus RunStatusCheck()
    {
        InstallerStatus status = executor.QueryStatus();
        _completedSteps.Add(InstallerStep.VerifyStatusPipe);
        _completedSteps.Add(InstallerStep.VerifyServiceConfiguration);
        return status;
    }

    private void RunRequired(InstallerStep step, Action action)
    {
        action();
        _completedSteps.Add(step);
    }

    private InstallerRunResult Fail(string message)
    {
        return new InstallerRunResult(false, message, _completedSteps, _warnings);
    }
}
