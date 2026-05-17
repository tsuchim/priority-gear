using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows.Forms;
using PriorityGear.Contracts;

namespace PriorityGear.Setup;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using SetupForm form = new(args);
        Application.Run(form);
        Environment.ExitCode = form.ExitCode;
    }

    private sealed class SetupForm : Form
    {
        private readonly string[] _args;
        private readonly TextBox _logBox = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both
        };

        public SetupForm(string[] args)
        {
            _args = args;
            Text = "PriorityGear Setup";
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Width = 900;
            Height = 650;
            Controls.Add(_logBox);
            Shown += async (_, _) => await RunAsync();
        }

        public int ExitCode { get; private set; } = 1;

        private async Task RunAsync()
        {
            string bootstrapLogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PriorityGear",
                "Logs");
            string logPath = Path.Combine(bootstrapLogDirectory, $"setup-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            SetupLog log = new(logPath);

            try
            {
                SetupInstallPlan plan = SetupInstallPlan.Create(ReadSetupVersion());
                if (_args.Any(arg => string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase)))
                {
                    await UninstallAsync(plan, log);
                    ExitCode = 0;
                    Finish("PriorityGear uninstall completed.", log);
                    return;
                }

                if (_args.Any(arg => string.Equals(arg, "--verify", StringComparison.OrdinalIgnoreCase)))
                {
                    log.Info("--verify checks the production install path only. Use PriorityGear.VerificationSetup.exe for the full developer verification harness.");
                }

                InstallerRunResult installResult = await InstallAsync(plan, log);
                SetupResult result = installResult.Succeeded
                    ? new SetupResult(true, installResult.Message)
                    : SetupResult.InstallFailed(installResult.Message);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(result.Summary);
                }

                ExitCode = 0;
                Finish(result.Summary, log);
            }
            catch (Exception ex)
            {
                log.Fail(ex.ToString());
                ExitCode = 1;
                Finish(SetupResult.InstallFailed(ex.Message).Summary, log);
            }
        }

        private static async Task<InstallerRunResult> InstallAsync(SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Environment");
            log.Info($"Version: {plan.Version} installer");
            log.Info($"Windows: {Environment.OSVersion.VersionString}");
            log.Info($"User: {WindowsIdentity.GetCurrent().Name}");
            log.Info($"Elevated: {IsElevated()}");
            log.Info($"Base install directory: {plan.BaseInstallDirectory}");
            log.Info($"Version install directory: {plan.VersionInstallDirectory}");
            log.Info($"ProgramData directory: {plan.ProgramDataDirectory}");
            log.Info($"Machine rules path preserved: {plan.MachineRulesPath}");
            log.Info($"Logs directory preserved: {plan.LogDirectory}");
            if (!IsElevated())
            {
                throw new InvalidOperationException("Administrator rights are required. Re-run by double-clicking the installer and approving UAC.");
            }

            SetupPlanSummary planSummary = SetupPlanner.CreateInstallOrUpdatePlan(plan);
            log.Section("Install/update plan");
            log.Info($"Service name: {planSummary.ServiceName}");
            log.Info($"Service account: {planSummary.ServiceAccount}");
            log.Info($"Service binary path: {planSummary.ServiceBinaryPath}");
            log.Info($"Preserve ProgramData: {planSummary.PreserveProgramData}");
            log.Info($"Preserve machine rules: {planSummary.PreserveMachineRules}");
            log.Info($"Preserve logs: {planSummary.PreserveLogs}");

            InstallerRunResult result = new InstallerStateMachine(
                plan,
                new ProductionInstallerExecutor(plan, log, SetupPayload.PayloadDirectory(AppContext.BaseDirectory)))
                .InstallOrUpdate();
            foreach (string warning in result.Warnings)
            {
                log.Info($"Warning: {warning}");
            }

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.Message);
            }

            log.Section("Final verdict");
            log.Info(result.Message);
            return result;
        }

        private static string ReadSetupVersion()
        {
            string versionPath = Path.Combine(AppContext.BaseDirectory, "setup-version.txt");
            if (!File.Exists(versionPath))
            {
                throw new FileNotFoundException("Installer is missing setup-version.txt.", versionPath);
            }

            string version = File.ReadAllText(versionPath).Trim();
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new InvalidOperationException("setup-version.txt is empty.");
            }

            return version;
        }

        private static async Task UninstallAsync(SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Uninstall");
            if (!IsElevated())
            {
                throw new InvalidOperationException("Administrator rights are required for uninstall.");
            }

            SetupPlanSummary planSummary = SetupPlanner.CreateUninstallPlan(plan);
            log.Info($"Service name: {planSummary.ServiceName}");
            log.Info($"Preserve ProgramData: {planSummary.PreserveProgramData}");
            log.Info($"Preserve machine rules: {planSummary.PreserveMachineRules}");
            log.Info($"Preserve logs: {planSummary.PreserveLogs}");

            if (ServiceExists(plan.ServiceName))
            {
                await StopNamedServiceAsync(plan.ServiceName, log);
                RunSc(ScCommand.DeleteService(plan.ServiceName), log);
            }
            else
            {
                log.Info("Service is not installed.");
            }

            if (Directory.Exists(plan.BaseInstallDirectory))
            {
                Directory.Delete(plan.BaseInstallDirectory, recursive: true);
                log.Info($"Removed installed program files: {plan.BaseInstallDirectory}");
            }

            log.Info($"Preserved data directory: {plan.ProgramDataDirectory}");
            log.Info("Delete ProgramData manually only if machine rules and logs are no longer needed.");
        }

        private static async Task StopExistingServiceBeforeInstallAsync(SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Existing service cleanup");
            if (!ServiceExists(plan.ServiceName))
            {
                log.Info("Existing service not found.");
                return;
            }

            await StopNamedServiceAsync(plan.ServiceName, log);
        }

        private static async Task StopNamedServiceAsync(string serviceName, SetupLog log)
        {
            using ServiceController controller = new(serviceName);
            controller.Refresh();
            if (controller.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            {
                controller.Stop();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30)));
                log.Info($"Service stopped: {serviceName}");
            }
            else
            {
                log.Info($"Service did not need stop: {serviceName}; state={controller.Status}");
            }
        }

        private static void InstallPayload(string payloadDirectory, SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Install files");
            Directory.CreateDirectory(plan.VersionInstallDirectory);
            foreach (string source in Directory.EnumerateFiles(payloadDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(payloadDirectory, source);
                string destination = Path.Combine(plan.VersionInstallDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, overwrite: true);
            }

            foreach (string requiredFile in SetupPayload.RequiredFiles)
            {
                string installedFile = Path.Combine(plan.VersionInstallDirectory, requiredFile);
                if (!File.Exists(installedFile))
                {
                    throw new FileNotFoundException($"Required installed file is missing: {installedFile}");
                }
            }

            log.Info("Payload installed to version directory.");
        }

        private static void InstallOrUpdateService(SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Install/update service");
            string? previousPath = TryGetServiceRegistryValue(plan.ServiceName, "ImagePath");
            log.Info($"Previous service binary path: {previousPath ?? "<not installed>"}");
            log.Info($"New service binary path: {plan.ServiceExePath}");
            RunSc(
                ServiceExists(plan.ServiceName)
                    ? ScCommand.ConfigService(plan.ServiceName, plan.ServiceExePath, plan.DisplayName)
                    : ScCommand.CreateService(plan.ServiceName, plan.ServiceExePath, plan.DisplayName),
                log);
        }

        private static async Task StartServiceAsync(SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Start service");
            using ServiceController controller = new(plan.ServiceName);
            controller.Refresh();
            if (controller.Status != ServiceControllerStatus.Running)
            {
                controller.Start();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)));
            }

            log.Info($"Service status: {controller.Status}");
        }

        private static void VerifyServiceConfiguration(SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Service configuration");
            string imagePath = TryGetServiceRegistryValue(plan.ServiceName, "ImagePath") ?? string.Empty;
            string account = TryGetServiceRegistryValue(plan.ServiceName, "ObjectName") ?? string.Empty;
            log.Info($"Service binary path: {imagePath}");
            log.Info($"Service account: {account}");
            if (!InstallerStateMachine.ServiceBinaryPathMatches(imagePath, plan.ServiceExePath))
            {
                throw new InvalidOperationException($"Service binary path is not the installed service path: {imagePath}");
            }

            if (!string.Equals(account, "LocalSystem", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Service account is not LocalSystem: {account}");
            }
        }

        private static async Task<ServiceResponse> VerifyStatusPipeAsync(SetupLog log)
        {
            log.Section("Status pipe");
            ServiceResponse last = new() { Succeeded = false, Message = "Status pipe was not attempted." };
            for (int attempt = 1; attempt <= 20; attempt++)
            {
                try
                {
                    last = await PipeClient.SendStatusAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    last = new ServiceResponse { Succeeded = false, Message = ex.Message };
                }

                log.Info($"Status pipe attempt {attempt}: Succeeded={last.Succeeded}; Message={last.Message}");
                if (last.Succeeded)
                {
                    return last;
                }

                await Task.Delay(500);
            }

            throw new InvalidOperationException($"Status pipe failed: {last.Message}");
        }

        private static void VerifyServiceStatus(ServiceResponse status, SetupLog log)
        {
            ServiceStatusDto dto = status.Status ?? throw new InvalidOperationException("Status pipe succeeded but did not return service status.");
            log.Info($"Service running: {dto.ServiceRunning}");
            log.Info($"Configured account: {dto.ConfiguredServiceAccount}");
            log.Info($"Process identity: {dto.ProcessIdentity}");
            log.Info($"SeDebugPrivilege: {dto.SeDebugPrivilege.Status}");
            if (!dto.ServiceRunning)
            {
                throw new InvalidOperationException("Service status reports that the service is not running.");
            }

            if (!string.Equals(dto.ConfiguredServiceAccount, "LocalSystem", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Service status account is not LocalSystem: {dto.ConfiguredServiceAccount}");
            }

            if (string.IsNullOrWhiteSpace(dto.ProcessIdentity))
            {
                throw new InvalidOperationException("Service status did not report process identity.");
            }
        }

        private static void CleanupOldVersions(SetupInstallPlan plan, SetupLog log)
        {
            log.Section("Old version cleanup");
            string versionsDirectory = Path.Combine(plan.BaseInstallDirectory, "versions");
            if (!Directory.Exists(versionsDirectory))
            {
                log.Info("No versions directory exists.");
                return;
            }

            DirectoryInfo current = new(plan.VersionInstallDirectory);
            foreach (DirectoryInfo oldVersion in new DirectoryInfo(versionsDirectory)
                .EnumerateDirectories()
                .Where(directory => !string.Equals(directory.FullName, current.FullName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(directory => directory.CreationTimeUtc)
                .Skip(3))
            {
                try
                {
                    oldVersion.Delete(recursive: true);
                    log.Info($"Deleted old version: {oldVersion.FullName}");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    log.Info($"Warning: old version cleanup failed: {oldVersion.FullName}; {ex.Message}");
                }
            }
        }

        private sealed class ProductionInstallerExecutor(SetupInstallPlan plan, SetupLog log, string payloadDirectory) : IInstallerExecutor
        {
            public void ValidatePayload()
            {
                IReadOnlyList<string> missingFiles = SetupPayload.MissingFiles(payloadDirectory);
                if (missingFiles.Count > 0)
                {
                    throw new FileNotFoundException($"Payload is incomplete: {string.Join(", ", missingFiles)}");
                }
            }

            public void StopExistingService()
            {
                StopExistingServiceBeforeInstallAsync(plan, log).GetAwaiter().GetResult();
            }

            public void CopyPayload()
            {
                InstallPayload(payloadDirectory, plan, log);
            }

            public void ConfigureService()
            {
                InstallOrUpdateService(plan, log);
            }

            public void StartService()
            {
                StartServiceAsync(plan, log).GetAwaiter().GetResult();
            }

            public InstallerStatus QueryStatus()
            {
                ServiceResponse response = VerifyStatusPipeAsync(log).GetAwaiter().GetResult();
                ServiceStatusDto dto = response.Status ?? throw new InvalidOperationException("Status pipe succeeded but did not return service status.");
                log.Info($"Service running: {dto.ServiceRunning}");
                log.Info($"Configured account: {dto.ConfiguredServiceAccount}");
                log.Info($"Process identity: {dto.ProcessIdentity}");
                log.Info($"SeDebugPrivilege: {dto.SeDebugPrivilege.Status}");
                return new InstallerStatus(
                    dto.ServiceRunning,
                    dto.ConfiguredServiceAccount,
                    dto.ProcessIdentity,
                    dto.ServiceBinaryPath);
            }

            public void CleanupOldVersions()
            {
                SetupForm.CleanupOldVersions(plan, log);
            }
        }

        private static bool ServiceExists(string serviceName)
        {
            return ServiceController.GetServices().Any(service => string.Equals(service.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
        }

        private static string? TryGetServiceRegistryValue(string serviceName, string valueName)
        {
            string keyPath = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
            return Convert.ToString(key?.GetValue(valueName));
        }

        private static void RunSc(ScCommand command, SetupLog log)
        {
            using Process process = Process.Start(CreateProcessStartInfo("sc.exe", command.Arguments))
                ?? throw new InvalidOperationException("Failed to start sc.exe.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            log.Info(command.DisplayText);
            if (!string.IsNullOrWhiteSpace(output))
            {
                log.Info(output.Trim());
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                log.Info(error.Trim());
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"{ScCommand.ClassifyFailure(process.ExitCode)} {error} {output}");
            }
        }

        private static ProcessStartInfo CreateProcessStartInfo(string fileName, IReadOnlyList<string> arguments)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return startInfo;
        }

        private static bool IsElevated()
        {
            WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Finish(string summary, SetupLog log)
        {
            log.Info($"Log: {log.Path}");
            log.Flush();
            _logBox.Text = log.ToString();
            MessageBox.Show(
                $"{summary}\r\n\r\nLog: {log.Path}",
                "PriorityGear Setup",
                MessageBoxButtons.OK,
                ExitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
    }
}
