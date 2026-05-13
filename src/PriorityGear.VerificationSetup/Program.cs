using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.Json;
using System.Windows.Forms;
using PriorityGear.Contracts;
using PriorityGear.Core;

namespace PriorityGear.VerificationSetup;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using VerificationForm form = new(args);
        Application.Run(form);
        Environment.ExitCode = form.ExitCode;
    }

    private sealed class VerificationForm : Form
    {
        private readonly string[] _args;
        private readonly TextBox _logBox = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both
        };

        public VerificationForm(string[] args)
        {
            _args = args;
            Text = "PriorityGear System Mode Verification";
            Width = 900;
            Height = 650;
            Controls.Add(_logBox);
            Shown += async (_, _) => await RunAsync();
        }

        public int ExitCode { get; private set; } = 1;

        private async Task RunAsync()
        {
            VerificationInstallPlan plan = VerificationInstallPlan.CreateDefault();
            string logPath = Path.Combine(
                plan.LogDirectory,
                $"system-mode-verification-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            VerificationLog log = new(logPath);

            try
            {
                if (_args.Any(arg => string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase)))
                {
                    await UninstallAsync(plan, log);
                    ExitCode = 0;
                    Finish("PriorityGear System Mode verification uninstall: PASSED", log);
                    return;
                }

                await VerifyAsync(plan, log);
                ExitCode = 0;
                Finish("PriorityGear System Mode verification: PASSED", log);
            }
            catch (Exception ex)
            {
                try
                {
                    await CleanupTestTargetServiceAsync(plan, log, throwOnFailure: false);
                }
                catch
                {
                }

                log.Fail(ex.ToString());
                ExitCode = 1;
                Finish($"PriorityGear System Mode verification: FAILED\r\nReason: {ex.Message}", log);
            }
        }

        private async Task VerifyAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Environment");
            log.Info($"Version: v0.2 verification setup");
            log.Info($"Windows: {Environment.OSVersion.VersionString}");
            log.Info($"User: {WindowsIdentity.GetCurrent().Name}");
            log.Info($"Elevated: {IsElevated()}");
            log.Info($"Base install directory: {plan.BaseInstallDirectory}");
            log.Info($"Version: {plan.Version}");
            log.Info($"Version install directory: {plan.VersionInstallDirectory}");
            log.Info($"New service binary path: {plan.ServiceExePath}");
            log.Info($"Service name: {plan.ServiceName}");
            if (!IsElevated())
            {
                throw new InvalidOperationException("Administrator rights are required. Re-run by double-clicking the setup and approving UAC.");
            }

            string setupDirectory = AppContext.BaseDirectory;
            string payloadDirectory = VerificationPayload.PayloadDirectory(setupDirectory);
            IReadOnlyList<string> missingFiles = VerificationPayload.MissingFiles(payloadDirectory);
            if (missingFiles.Count > 0)
            {
                throw new FileNotFoundException($"Payload is incomplete: {string.Join(", ", missingFiles)}");
            }

            await CleanupTestTargetServiceAsync(plan, log, throwOnFailure: false);
            await StopExistingServiceBeforeInstallAsync(plan, log);
            InstallPayload(payloadDirectory, plan, log);
            await InstallOrUpdateServiceAsync(plan, log);
            CleanupOldVersions(plan, log);
            await StartServiceAsync(plan, log);
            VerifyServiceConfiguration(plan, log);

            ServiceResponse status = await SendStatusAsync(log);
            if (!status.Succeeded)
            {
                AppendServiceDiagnostics(plan, log);
                throw new InvalidOperationException($"Status pipe failed: {status.Message}");
            }

            await VerifyPriorityMutationAsync(plan, log);
            await VerifyMachineRulesAsync(plan, log);
            await VerifyLocalSystemTestTargetServiceAsync(plan, log);
            await VerifyProbeAsync(log);
            await CleanupTestTargetServiceAsync(plan, log, throwOnFailure: true);

            log.Section("Final verdict");
            log.Info("Final verdict: passed");
        }

        private async Task UninstallAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Uninstall");
            if (!IsElevated())
            {
                throw new InvalidOperationException("Administrator rights are required for uninstall.");
            }

            if (ServiceExists(plan.ServiceName))
            {
                await StopServiceAsync(plan, log);
                RunProcess("sc.exe", $"delete \"{plan.ServiceName}\"", log);
            }

            await CleanupTestTargetServiceAsync(plan, log, throwOnFailure: false);

            log.Info("Uninstall completed.");
        }

        private static async Task StopExistingServiceBeforeInstallAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Existing service cleanup");
            if (!ServiceExists(plan.ServiceName))
            {
                log.Info("Existing service not found.");
                return;
            }

            using ServiceController controller = new(plan.ServiceName);
            controller.Refresh();
            int? processId = TryGetServiceProcessId(plan.ServiceName, log);
            log.Info("Existing service found.");
            log.Info($"Existing service state: {controller.Status}");
            log.Info($"Existing service PID: {processId?.ToString() ?? "<unknown>"}");

            if (controller.Status == ServiceControllerStatus.Stopped)
            {
                log.Info("Existing service is already stopped.");
                await WaitForServiceProcessExitAsync(processId, log);
                return;
            }

            if (controller.Status == ServiceControllerStatus.StopPending)
            {
                log.Info("Existing service is already stopping.");
            }
            else
            {
                log.Info("Stop requested.");
                controller.Stop();
            }

            controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            controller.Refresh();
            if (controller.Status != ServiceControllerStatus.Stopped)
            {
                throw new InvalidOperationException($"Existing service did not stop before payload update. State={controller.Status}; PID={processId?.ToString() ?? "<unknown>"}. Installed files were not overwritten.");
            }

            log.Info("Service stopped.");
            await WaitForServiceProcessExitAsync(processId, log);
        }

        private static void InstallPayload(string payloadDirectory, VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Install files");
            log.Info($"Base install directory: {plan.BaseInstallDirectory}");
            log.Info($"Version install directory: {plan.VersionInstallDirectory}");
            Directory.CreateDirectory(plan.VersionInstallDirectory);
            foreach (string source in Directory.EnumerateFiles(payloadDirectory, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(payloadDirectory, source);
                string destination = Path.Combine(plan.VersionInstallDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                try
                {
                    File.Copy(source, destination, overwrite: true);
                }
                catch (IOException ex)
                {
                    throw new IOException($"Failed to copy payload file. Existing service was expected to be stopped before copying. Locked or unavailable file: {destination}", ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException($"Failed to copy payload file due to access denial: {destination}", ex);
                }
            }

            foreach (string requiredFile in VerificationPayload.RequiredFiles)
            {
                string installedFile = Path.Combine(plan.VersionInstallDirectory, requiredFile);
                if (!File.Exists(installedFile))
                {
                    throw new FileNotFoundException($"Required installed file is missing: {installedFile}");
                }
            }

            log.Info("Payload installed to version directory.");
        }


        private static async Task InstallOrUpdateServiceAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Install/update service");
            string? previousPath = TryGetServiceImagePath(plan.ServiceName);
            log.Info($"Previous service binary path: {previousPath ?? "<not installed>"}");
            log.Info($"New service binary path: {plan.ServiceExePath}");
            if (ServiceExists(plan.ServiceName))
            {
                await StopServiceAsync(plan, log);
                RunProcess("sc.exe", $"config \"{plan.ServiceName}\" binPath= \"{plan.ServiceExePath}\" obj= LocalSystem start= demand DisplayName= \"{plan.DisplayName}\"", log);
            }
            else
            {
                RunProcess("sc.exe", $"create \"{plan.ServiceName}\" binPath= \"{plan.ServiceExePath}\" obj= LocalSystem start= demand DisplayName= \"{plan.DisplayName}\"", log);
            }

            log.Info("Service installed or updated.");
        }

        private static async Task StartServiceAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Start service");
            using ServiceController controller = new(plan.ServiceName);
            controller.Refresh();
            if (controller.Status != ServiceControllerStatus.Running)
            {
                controller.Start();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20)));
            }

            log.Info($"Service status: {controller.Status}");
        }

        private static void CleanupOldVersions(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Old version cleanup");
            string versionsDirectory = Path.Combine(plan.BaseInstallDirectory, "versions");
            log.Info($"Versions directory: {versionsDirectory}");
            if (!Directory.Exists(versionsDirectory))
            {
                log.Info("No versions directory exists.");
                return;
            }

            DirectoryInfo current = new(plan.VersionInstallDirectory);
            List<DirectoryInfo> oldVersions = new DirectoryInfo(versionsDirectory)
                .EnumerateDirectories()
                .Where(directory => !string.Equals(directory.FullName, current.FullName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(directory => directory.CreationTimeUtc)
                .Skip(3)
                .ToList();

            if (oldVersions.Count == 0)
            {
                log.Info("No old versions selected for cleanup.");
                return;
            }

            foreach (DirectoryInfo oldVersion in oldVersions)
            {
                try
                {
                    oldVersion.Delete(recursive: true);
                    log.Info($"Deleted old version: {oldVersion.FullName}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    log.Info($"Warning: old version cleanup access denied: {oldVersion.FullName}; {ex.Message}");
                }
                catch (IOException ex)
                {
                    log.Info($"Warning: old version cleanup I/O failure: {oldVersion.FullName}; {ex.Message}");
                }
            }
        }

        private static async Task StopServiceAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            await StopNamedServiceAsync(plan.ServiceName, log);
        }

        private static async Task StopNamedServiceAsync(string serviceName, VerificationLog log)
        {
            using ServiceController controller = new(serviceName);
            controller.Refresh();
            if (controller.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            {
                controller.Stop();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20)));
                log.Info($"Service stopped: {serviceName}");
            }
        }

        private static void VerifyServiceConfiguration(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Service configuration");
            string imagePath = TryGetServiceImagePath(plan.ServiceName) ?? string.Empty;
            string objectName = TryGetServiceObjectName(plan.ServiceName) ?? string.Empty;
            log.Info($"Service binary path: {imagePath}");
            log.Info($"Service account: {objectName}");

            if (!imagePath.Contains(plan.ServiceExePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Service binary path is not the verification install path: {imagePath}");
            }

            if (!string.Equals(objectName, "LocalSystem", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Service account is not LocalSystem: {objectName}");
            }
        }

        private static async Task<ServiceResponse> SendStatusAsync(VerificationLog log)
        {
            log.Section("Status pipe");
            ServiceResponse last = new() { Succeeded = false, Message = "Status pipe was not attempted." };
            for (int attempt = 1; attempt <= 20; attempt++)
            {
                last = await PipeClient.SendAsync(
                    ServiceContractConstants.StatusPipeName,
                    new ServiceRequest { Kind = ServiceCommandKind.GetServiceStatus },
                    CancellationToken.None);
                log.Info($"Status pipe attempt {attempt}: Succeeded={last.Succeeded}; Message={last.Message}");
                if (last.Succeeded)
                {
                    log.Info(JsonSerializer.Serialize(last, JsonOptions));
                    return last;
                }

                await Task.Delay(500);
            }

            log.Info(JsonSerializer.Serialize(last, JsonOptions));
            return last;
        }

        private static async Task<ServiceResponse> SendAdminAsync(ServiceRequest request, VerificationLog log, string title)
        {
            log.Section(title);
            ServiceResponse response = await PipeClient.SendAsync(
                ServiceContractConstants.AdminPipeName,
                request,
                CancellationToken.None);
            log.Info(JsonSerializer.Serialize(response, JsonOptions));
            return response;
        }

        private static async Task VerifyPriorityMutationAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Priority mutation using TestTarget");
            string targetExe = Path.Combine(plan.VersionInstallDirectory, "PriorityGear.TestTarget.exe");
            using Process target = Process.Start(new ProcessStartInfo
            {
                FileName = targetExe,
                Arguments = "--hold-seconds 120",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }) ?? throw new InvalidOperationException("Failed to start PriorityGear.TestTarget.");

            try
            {
                await Task.Delay(500);
                target.Refresh();
                log.Info($"TestTarget PID: {target.Id}");
                log.Info($"Original priority: {target.PriorityClass}");

                ServiceResponse apply = await SendAdminAsync(new ServiceRequest
                {
                    Kind = ServiceCommandKind.TestApplyPriority,
                    ProcessId = target.Id,
                    Priority = ProcessPriorityLevel.BelowNormal
                }, log, "Admin pipe priority apply");
                if (!apply.Succeeded)
                {
                    throw new InvalidOperationException($"Admin priority apply failed: {apply.Message}");
                }

                target.Refresh();
                log.Info($"Priority after apply: {target.PriorityClass}");
                if (target.PriorityClass != ProcessPriorityClass.BelowNormal)
                {
                    throw new InvalidOperationException($"Priority did not become BelowNormal: {target.PriorityClass}");
                }

                ServiceResponse restore = await SendAdminAsync(new ServiceRequest
                {
                    Kind = ServiceCommandKind.TestApplyPriority,
                    ProcessId = target.Id,
                    Priority = ProcessPriorityLevel.Normal
                }, log, "Admin pipe priority restore");
                if (!restore.Succeeded)
                {
                    throw new InvalidOperationException($"Priority restore failed: {restore.Message}");
                }

                target.Refresh();
                log.Info($"Priority after restore: {target.PriorityClass}");
                if (target.PriorityClass != ProcessPriorityClass.Normal)
                {
                    throw new InvalidOperationException($"Priority did not restore to Normal: {target.PriorityClass}");
                }
            }
            finally
            {
                if (!target.HasExited)
                {
                    target.Kill(entireProcessTree: true);
                    await target.WaitForExitAsync();
                }
            }
        }

        private static async Task VerifyMachineRulesAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Machine rule validation");
            string rulesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PriorityGear");
            Directory.CreateDirectory(rulesDirectory);
            string rulesPath = Path.Combine(rulesDirectory, "rules.machine.json");
            string? backupPath = null;
            if (File.Exists(rulesPath))
            {
                backupPath = rulesPath + ".verification-backup";
                File.Copy(rulesPath, backupPath, overwrite: true);
            }

            Guid enabledApproved = Guid.NewGuid();
            Guid disabled = Guid.NewGuid();
            Guid unapproved = Guid.NewGuid();
            Guid mismatched = Guid.NewGuid();
            Guid pathMismatch = Guid.NewGuid();

            List<MachinePriorityRule> rules =
            [
                new() { Id = enabledApproved, DisplayName = "Verification matching rule", Enabled = true, ApprovedByAdmin = true, ExecutableName = "PriorityGear.TestTarget.exe", BasePriority = ProcessPriorityLevel.BelowNormal },
                new() { Id = disabled, DisplayName = "Verification disabled rule", Enabled = false, ApprovedByAdmin = true, ExecutableName = "PriorityGear.TestTarget.exe", BasePriority = ProcessPriorityLevel.BelowNormal },
                new() { Id = unapproved, DisplayName = "Verification unapproved rule", Enabled = true, ApprovedByAdmin = false, ExecutableName = "PriorityGear.TestTarget.exe", BasePriority = ProcessPriorityLevel.BelowNormal },
                new() { Id = mismatched, DisplayName = "Verification mismatched executable", Enabled = true, ApprovedByAdmin = true, ExecutableName = "not-the-target.exe", BasePriority = ProcessPriorityLevel.BelowNormal },
                new() { Id = pathMismatch, DisplayName = "Verification path mismatch", Enabled = true, ApprovedByAdmin = true, ExecutableName = "PriorityGear.TestTarget.exe", FullPath = @"C:\PriorityGear\missing-target.exe", BasePriority = ProcessPriorityLevel.BelowNormal }
            ];

            File.WriteAllText(rulesPath, JsonSerializer.Serialize(rules, JsonOptions));

            string targetExe = Path.Combine(plan.VersionInstallDirectory, "PriorityGear.TestTarget.exe");
            using Process target = Process.Start(new ProcessStartInfo
            {
                FileName = targetExe,
                Arguments = "--hold-seconds 120",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }) ?? throw new InvalidOperationException("Failed to start PriorityGear.TestTarget for machine rule validation.");

            try
            {
                await Task.Delay(500);
                await ExpectRuleResult(enabledApproved, target.Id, shouldSucceed: true, "enabled approved matching rule", log);
                await SendAdminAsync(new ServiceRequest { Kind = ServiceCommandKind.TestApplyPriority, ProcessId = target.Id, Priority = ProcessPriorityLevel.Normal }, log, "Restore after matching rule");
                await ExpectRuleResult(disabled, target.Id, shouldSucceed: false, "disabled rule", log);
                await ExpectRuleResult(unapproved, target.Id, shouldSucceed: false, "unapproved rule", log);
                await ExpectRuleResult(mismatched, target.Id, shouldSucceed: false, "mismatched executable rule", log);
                await ExpectRuleResult(pathMismatch, target.Id, shouldSucceed: false, "path mismatch rule", log);
            }
            finally
            {
                if (!target.HasExited)
                {
                    target.Kill(entireProcessTree: true);
                    await target.WaitForExitAsync();
                }

                if (backupPath is not null)
                {
                    File.Copy(backupPath, rulesPath, overwrite: true);
                    File.Delete(backupPath);
                    log.Info("Original machine rules restored from backup.");
                }
                else
                {
                    File.Delete(rulesPath);
                    log.Info("Temporary machine rules removed.");
                }
            }
        }

        private static async Task ExpectRuleResult(Guid ruleId, int processId, bool shouldSucceed, string label, VerificationLog log)
        {
            ServiceResponse response = await SendAdminAsync(new ServiceRequest
            {
                Kind = ServiceCommandKind.ApplyApprovedMachineRule,
                RuleId = ruleId,
                ProcessId = processId
            }, log, $"Machine rule: {label}");
            if (response.Succeeded != shouldSucceed)
            {
                throw new InvalidOperationException($"Unexpected machine rule result for {label}: {response.Message}");
            }
        }

        private static async Task VerifyLocalSystemTestTargetServiceAsync(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("LocalSystem TestTarget service priority mutation");
            await CleanupTestTargetServiceAsync(plan, log, throwOnFailure: false);

            string testTargetExe = Path.Combine(plan.VersionInstallDirectory, "PriorityGear.TestTarget.exe");
            string binaryPath = $"\"{testTargetExe}\" --hold-seconds 120";
            log.Info($"TestTarget service name: {plan.TestTargetServiceName}");
            log.Info($"TestTarget service binary path: {binaryPath}");

            RunProcess("sc.exe", $"create \"{plan.TestTargetServiceName}\" binPath= \"{binaryPath}\" obj= LocalSystem start= demand DisplayName= \"{plan.TestTargetDisplayName}\"", log);
            try
            {
                using ServiceController controller = new(plan.TestTargetServiceName);
                controller.Start();
                await Task.Run(() => controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(20)));
                log.Info($"TestTarget service status: {controller.Status}");

                string account = TryGetServiceObjectName(plan.TestTargetServiceName) ?? string.Empty;
                log.Info($"TestTarget service account: {account}");
                if (!string.Equals(account, "LocalSystem", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"TestTarget service account is not LocalSystem: {account}");
                }

                int processId = await WaitForServiceProcessIdAsync(plan.TestTargetServiceName, log);
                using Process target = Process.GetProcessById(processId);
                target.Refresh();
                log.Info($"LocalSystem TestTarget PID: {target.Id}");
                log.Info($"LocalSystem TestTarget original priority: {target.PriorityClass}");

                ServiceResponse apply = await SendAdminAsync(new ServiceRequest
                {
                    Kind = ServiceCommandKind.TestApplyPriority,
                    ProcessId = target.Id,
                    Priority = ProcessPriorityLevel.BelowNormal
                }, log, "Admin pipe LocalSystem TestTarget priority apply");
                if (!apply.Succeeded)
                {
                    throw new InvalidOperationException($"LocalSystem TestTarget priority apply failed: {apply.Message}");
                }

                target.Refresh();
                log.Info($"LocalSystem TestTarget priority after apply: {target.PriorityClass}");
                if (target.PriorityClass != ProcessPriorityClass.BelowNormal)
                {
                    throw new InvalidOperationException($"LocalSystem TestTarget priority did not become BelowNormal: {target.PriorityClass}");
                }

                ServiceResponse restore = await SendAdminAsync(new ServiceRequest
                {
                    Kind = ServiceCommandKind.TestApplyPriority,
                    ProcessId = target.Id,
                    Priority = ProcessPriorityLevel.Normal
                }, log, "Admin pipe LocalSystem TestTarget priority restore");
                if (!restore.Succeeded)
                {
                    throw new InvalidOperationException($"LocalSystem TestTarget priority restore failed: {restore.Message}");
                }

                target.Refresh();
                log.Info($"LocalSystem TestTarget priority after restore: {target.PriorityClass}");
                if (target.PriorityClass != ProcessPriorityClass.Normal)
                {
                    throw new InvalidOperationException($"LocalSystem TestTarget priority did not restore to Normal: {target.PriorityClass}");
                }
            }
            finally
            {
                await CleanupTestTargetServiceAsync(plan, log, throwOnFailure: true);
            }
        }

        private static async Task CleanupTestTargetServiceAsync(VerificationInstallPlan plan, VerificationLog log, bool throwOnFailure)
        {
            try
            {
                log.Section("TestTarget service cleanup");
                if (!ServiceExists(plan.TestTargetServiceName))
                {
                    log.Info("Temporary TestTarget service not found.");
                    return;
                }

                log.Info("Temporary TestTarget service found.");
                await StopNamedServiceAsync(plan.TestTargetServiceName, log);
                RunProcess("sc.exe", $"delete \"{plan.TestTargetServiceName}\"", log);
                log.Info("Temporary TestTarget service deleted.");
            }
            catch (Exception ex)
            {
                log.Info($"Temporary TestTarget service cleanup failed: {ex.Message}");
                if (throwOnFailure)
                {
                    throw;
                }
            }
        }

        private static async Task<int> WaitForServiceProcessIdAsync(string serviceName, VerificationLog log)
        {
            for (int attempt = 1; attempt <= 20; attempt++)
            {
                int? processId = TryGetServiceProcessId(serviceName, log);
                if (processId is > 0)
                {
                    return processId.Value;
                }

                await Task.Delay(500);
            }

            throw new InvalidOperationException($"PID was not found for service {serviceName}.");
        }

        private static async Task VerifyProbeAsync(VerificationLog log)
        {
            log.Section("Denied/protected probe");
            ServiceResponse response = await SendAdminAsync(new ServiceRequest
            {
                Kind = ServiceCommandKind.ProbePriorityAccess,
                ProcessId = 4
            }, log, "Probe PID 4");
            log.Info(response.Succeeded
                ? "Probe found priority write access for PID 4. No mutation was attempted."
                : "Probe failed explicitly for PID 4. No mutation was attempted.");
        }

        private static bool ServiceExists(string serviceName)
        {
            return ServiceController.GetServices().Any(service => string.Equals(service.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
        }

        private static string? TryGetServiceImagePath(string serviceName)
        {
            return TryGetServiceRegistryValue(serviceName, "ImagePath");
        }

        private static string? TryGetServiceObjectName(string serviceName)
        {
            return TryGetServiceRegistryValue(serviceName, "ObjectName");
        }

        private static string? TryGetServiceRegistryValue(string serviceName, string valueName)
        {
            string keyPath = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
            return Convert.ToString(key?.GetValue(valueName));
        }

        private static int? TryGetServiceProcessId(string serviceName, VerificationLog log)
        {
            try
            {
                ProcessResult result = RunProcessCapture("sc.exe", $"queryex \"{serviceName}\"");
                if (result.ExitCode != 0)
                {
                    log.Info($"Could not query service PID. sc.exe exit={result.ExitCode}; {result.Error.Trim()}");
                    return null;
                }

                foreach (string line in result.Output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = line.Trim();
                    if (!trimmed.StartsWith("PID", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] parts = trimmed.Split(':', 2);
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int pid) && pid > 0)
                    {
                        return pid;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Info($"Could not query service PID: {ex.Message}");
            }

            return null;
        }

        private static async Task WaitForServiceProcessExitAsync(int? processId, VerificationLog log)
        {
            if (processId is null)
            {
                log.Info("Service process exit confirmation skipped because PID is unknown.");
                return;
            }

            try
            {
                using Process process = Process.GetProcessById(processId.Value);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
                log.Info("Service process exited.");
            }
            catch (ArgumentException)
            {
                log.Info("Service process already exited.");
            }
            catch (System.TimeoutException)
            {
                throw new InvalidOperationException($"Service reached Stopped state but process did not exit within timeout. PID={processId}. Installed files were not overwritten.");
            }
        }

        private static void AppendServiceDiagnostics(VerificationInstallPlan plan, VerificationLog log)
        {
            log.Section("Failure diagnostics");
            try
            {
                if (ServiceExists(plan.ServiceName))
                {
                    using ServiceController controller = new(plan.ServiceName);
                    controller.Refresh();
                    log.Info($"Service status on failure: {controller.Status}");
                }
            }
            catch (Exception ex)
            {
                log.Info($"Could not read service status on failure: {ex.Message}");
            }

            try
            {
                string keyPath = $@"SYSTEM\CurrentControlSet\Services\{plan.ServiceName}";
                using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);
                log.Info($"Service binary path on failure: {Convert.ToString(key?.GetValue("ImagePath")) ?? "<missing>"}");
                log.Info($"Service account on failure: {Convert.ToString(key?.GetValue("ObjectName")) ?? "<missing>"}");
            }
            catch (Exception ex)
            {
                log.Info($"Could not read service registry on failure: {ex.Message}");
            }

            string serviceLogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PriorityGear",
                "Logs",
                "service-current.log");
            log.Info($"Service log path: {serviceLogPath}");
            if (!File.Exists(serviceLogPath))
            {
                log.Info("Service log file does not exist.");
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(serviceLogPath);
                log.Info("Service log tail:");
                foreach (string line in lines.TakeLast(80))
                {
                    log.Info(line);
                }
            }
            catch (Exception ex)
            {
                log.Info($"Could not read service log tail: {ex.Message}");
            }
        }

        private static void RunProcess(string fileName, string arguments, VerificationLog log)
        {
            ProcessResult result = RunProcessCapture(fileName, arguments);
            log.Info($"{fileName} {arguments}");
            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                log.Info(result.Output.Trim());
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                log.Info(result.Error.Trim());
            }

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"{fileName} exited with code {result.ExitCode}: {result.Error} {result.Output}");
            }
        }

        private static ProcessResult RunProcessCapture(string fileName, string arguments)
        {
            using Process process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException($"Failed to start {fileName}.");

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            return new ProcessResult(process.ExitCode, output, error);
        }

        private static bool IsElevated()
        {
            WindowsPrincipal principal = new(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Finish(string summary, VerificationLog log)
        {
            log.Info($"Log: {log.Path}");
            log.Flush();
            _logBox.Text = log.ToString();
            MessageBox.Show(
                $"{summary}\r\n\r\nLog: {log.Path}",
                "PriorityGear System Mode Verification",
                ExitCode == 0 ? MessageBoxButtons.OK : MessageBoxButtons.OK,
                ExitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
