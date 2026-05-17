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
        _completedSteps.Clear();
        _warnings.Clear();

        try
        {
            RunRequired(InstallerStep.ValidatePayload, executor.ValidatePayload);
            RunRequired(InstallerStep.StopExistingService, executor.StopExistingService);
            RunRequired(InstallerStep.CopyPayload, executor.CopyPayload);
            RunRequired(InstallerStep.ConfigureService, executor.ConfigureService);
            RunRequired(InstallerStep.StartService, executor.StartService);

            InstallerStatus status = QueryStatus();
            if (!status.ServiceRunning)
            {
                return Fail("Service status reports that the service is not running.");
            }
            _completedSteps.Add(InstallerStep.VerifyStatusPipe);

            if (!string.Equals(status.ConfiguredServiceAccount, "LocalSystem", StringComparison.OrdinalIgnoreCase))
            {
                return Fail($"Service account is not LocalSystem: {status.ConfiguredServiceAccount}");
            }

            if (string.IsNullOrWhiteSpace(status.ProcessIdentity))
            {
                return Fail("Service status did not report process identity.");
            }

            if (!ServiceBinaryPathMatches(status.ServiceBinaryPath, plan.ServiceExePath))
            {
                return Fail($"Service binary path does not point at the new version directory: {status.ServiceBinaryPath}");
            }
            _completedSteps.Add(InstallerStep.VerifyServiceConfiguration);

            try
            {
                executor.CleanupOldVersions();
                _completedSteps.Add(InstallerStep.CleanupOldVersions);
            }
            catch (Exception ex)
            {
                _warnings.Add($"Old version cleanup failed: {ex.Message}");
            }

            return Success("Install/update completed.");
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private InstallerStatus QueryStatus()
    {
        return executor.QueryStatus();
    }

    private void RunRequired(InstallerStep step, Action action)
    {
        action();
        _completedSteps.Add(step);
    }

    private InstallerRunResult Fail(string message)
    {
        return new InstallerRunResult(false, message, _completedSteps.ToArray(), _warnings.ToArray());
    }

    private InstallerRunResult Success(string message)
    {
        return new InstallerRunResult(true, message, _completedSteps.ToArray(), _warnings.ToArray());
    }

    public static bool ServiceBinaryPathMatches(string configuredPath, string expectedPath)
    {
        string? configuredExe = ExtractExecutablePath(configuredPath);
        if (string.IsNullOrWhiteSpace(configuredExe))
        {
            return false;
        }

        return string.Equals(
            Path.GetFullPath(configuredExe),
            Path.GetFullPath(expectedPath),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractExecutablePath(string configuredPath)
    {
        string trimmed = configuredPath.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed[0] == '"')
        {
            int endQuote = trimmed.IndexOf('"', 1);
            return endQuote > 1 ? trimmed[1..endQuote] : null;
        }

        int exeIndex = trimmed.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        return exeIndex >= 0 ? trimmed[..(exeIndex + 4)] : trimmed.Split(' ', 2)[0];
    }
}
