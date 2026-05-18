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

public enum InstallerProgressKind
{
    Starting,
    Completed,
    Failed,
    Warning
}

public sealed record InstallerProgress(InstallerProgressKind Kind, InstallerStep Step, string Message);

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

    public event Action<InstallerProgress>? Progress;

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

            Emit(InstallerProgressKind.Starting, InstallerStep.VerifyStatusPipe, "Verifying service status pipe.");
            InstallerStatus status = QueryStatus();
            if (!status.ServiceRunning)
            {
                return Fail(InstallerStep.VerifyStatusPipe, "Service status reports that the service is not running.");
            }
            _completedSteps.Add(InstallerStep.VerifyStatusPipe);
            Emit(InstallerProgressKind.Completed, InstallerStep.VerifyStatusPipe, "Service status pipe responded.");

            Emit(InstallerProgressKind.Starting, InstallerStep.VerifyServiceConfiguration, "Validating service configuration.");
            if (!string.Equals(status.ConfiguredServiceAccount, "LocalSystem", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(InstallerStep.VerifyServiceConfiguration, $"Service account is not LocalSystem: {status.ConfiguredServiceAccount}");
            }

            if (string.IsNullOrWhiteSpace(status.ProcessIdentity))
            {
                return Fail(InstallerStep.VerifyServiceConfiguration, "Service status did not report process identity.");
            }

            if (!ServiceBinaryPathMatches(status.ServiceBinaryPath, plan.ServiceExePath))
            {
                return Fail(InstallerStep.VerifyServiceConfiguration, $"Service binary path does not point at the new version directory: {status.ServiceBinaryPath}");
            }
            _completedSteps.Add(InstallerStep.VerifyServiceConfiguration);
            Emit(InstallerProgressKind.Completed, InstallerStep.VerifyServiceConfiguration, "Service configuration is valid.");

            try
            {
                Emit(InstallerProgressKind.Starting, InstallerStep.CleanupOldVersions, "Cleaning old version directories.");
                executor.CleanupOldVersions();
                _completedSteps.Add(InstallerStep.CleanupOldVersions);
                Emit(InstallerProgressKind.Completed, InstallerStep.CleanupOldVersions, "Old version cleanup completed.");
            }
            catch (Exception ex)
            {
                string warning = $"Old version cleanup failed: {ex.Message}";
                _warnings.Add(warning);
                Emit(InstallerProgressKind.Warning, InstallerStep.CleanupOldVersions, warning);
            }

            return Success("Install/update completed.");
        }
        catch (InstallerStepException ex)
        {
            return Fail($"{ex.Step} failed: {ex.InnerException?.Message ?? ex.Message}");
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
        Emit(InstallerProgressKind.Starting, step, "Starting.");
        try
        {
            action();
            _completedSteps.Add(step);
            Emit(InstallerProgressKind.Completed, step, "Completed.");
        }
        catch (Exception ex)
        {
            Emit(InstallerProgressKind.Failed, step, ex.Message);
            throw new InstallerStepException(step, ex);
        }
    }

    private InstallerRunResult Fail(string message)
    {
        return new InstallerRunResult(false, message, _completedSteps.ToArray(), _warnings.ToArray());
    }

    private InstallerRunResult Fail(InstallerStep step, string message)
    {
        Emit(InstallerProgressKind.Failed, step, message);
        return Fail(message);
    }

    private InstallerRunResult Success(string message)
    {
        return new InstallerRunResult(true, message, _completedSteps.ToArray(), _warnings.ToArray());
    }

    private void Emit(InstallerProgressKind kind, InstallerStep step, string message)
    {
        Progress?.Invoke(new InstallerProgress(kind, step, message));
    }

    private sealed class InstallerStepException(InstallerStep step, Exception innerException)
        : Exception(innerException.Message, innerException)
    {
        public InstallerStep Step { get; } = step;
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
