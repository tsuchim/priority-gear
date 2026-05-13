using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using PriorityGear.Contracts;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class ServiceProcessDiscovery(Win32PriorityApplier priorityApplier)
{
    public const int DefaultResponseLimit = 100;

    public IReadOnlyList<ServiceProcessInfoDto> Discover()
    {
        Dictionary<int, ServiceProcessInfoDto> byPid = [];
        foreach (ServiceController service in ServiceController.GetServices())
        {
            using (service)
            {
                int? pid = TryGetServicePid(service.ServiceName);
                if (pid is null or <= 0)
                {
                    continue;
                }

                if (!byPid.TryGetValue(pid.Value, out ServiceProcessInfoDto? info))
                {
                    info = CreateProcessInfo(pid.Value);
                    byPid[pid.Value] = info;
                }

                info.ServiceNames.Add(service.ServiceName);
                info.SharedServiceHost = info.ServiceNames.Count > 1;
                info.Owner ??= TryGetRegistryValue(service.ServiceName, "ObjectName");
            }
        }

        foreach (ServiceProcessInfoDto info in byPid.Values)
        {
            info.SharedServiceHost = info.ServiceNames.Count > 1;
        }

        return byPid.Values.OrderBy(info => info.ProcessId).ToList();
    }

    public ServiceProcessDiscoveryStatusDto GetStatus()
    {
        IReadOnlyList<ServiceProcessInfoDto> discovered = Discover();
        return new ServiceProcessDiscoveryStatusDto
        {
            Available = true,
            RunningServiceCount = discovered.Sum(info => info.ServiceNames.Count),
            ServiceHostProcessCount = discovered.Count,
            SharedHostProcessCount = discovered.Count(info => info.SharedServiceHost),
            TotalDiscoveredGroupCount = discovered.Count,
            ReturnedGroupCount = Math.Min(discovered.Count, DefaultResponseLimit),
            Truncated = discovered.Count > DefaultResponseLimit,
            Limit = DefaultResponseLimit,
            Message = "Service process discovery available."
        };
    }

    public ServiceProcessInfoDto? FindByServiceName(string serviceName)
    {
        ServiceProcessInfoDto? direct = DiscoverOne(serviceName);
        if (direct is not null)
        {
            return direct;
        }

        return Discover().FirstOrDefault(info => info.ServiceNames.Any(name => string.Equals(name, serviceName, StringComparison.OrdinalIgnoreCase)));
    }

    public ServiceProcessInfoDto? DiscoverOne(string serviceName)
    {
        try
        {
            using ServiceController service = new(serviceName);
            int? pid = TryGetServicePid(service.ServiceName);
            if (pid is null or <= 0)
            {
                return null;
            }

            ServiceProcessInfoDto info = CreateProcessInfo(pid.Value);
            info.ServiceNames.Add(service.ServiceName);
            info.Owner = TryGetRegistryValue(service.ServiceName, "ObjectName");
            info.SharedServiceHost = Discover().Any(candidate =>
                candidate.ProcessId == pid.Value &&
                candidate.ServiceNames.Count > 1);
            return info;
        }
        catch
        {
            return null;
        }
    }

    public ServiceProcessInfoDto? DiscoverHostGroupByServiceName(string serviceName)
    {
        ServiceProcessInfoDto? direct = DiscoverOne(serviceName);
        if (direct is null)
        {
            return null;
        }

        return DiscoverHostGroupByProcessId(direct.ProcessId) ?? direct;
    }

    public ServiceProcessInfoDto? DiscoverHostGroupByProcessId(int processId)
    {
        return Discover().FirstOrDefault(info => info.ProcessId == processId);
    }

    private ServiceProcessInfoDto CreateProcessInfo(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            string name = process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? process.ProcessName
                : process.ProcessName + ".exe";
            return new ServiceProcessInfoDto
            {
                ProcessId = processId,
                ExecutableName = name,
                Path = TryGetPath(process),
                CurrentPriority = TryGetPriority(process),
                PriorityAccessStatus = priorityApplier.ProbeSetPriorityAccess(processId).Status.ToString()
            };
        }
        catch
        {
            return new ServiceProcessInfoDto { ProcessId = processId, PriorityAccessStatus = "ProcessUnavailable" };
        }
    }

    private static int? TryGetServicePid(string serviceName)
    {
        try
        {
            using Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                ArgumentList = { "queryex", serviceName },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Failed to start sc.exe.");
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            foreach (string line in output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries))
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
        catch
        {
        }

        return null;
    }

    private static string? TryGetRegistryValue(string serviceName, string valueName)
    {
        string keyPath = $@"SYSTEM\CurrentControlSet\Services\{serviceName}";
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath);
        return Convert.ToString(key?.GetValue(valueName));
    }

    private static string? TryGetPath(Process process)
    {
        try { return process.MainModule?.FileName; } catch { return null; }
    }

    private static string? TryGetPriority(Process process)
    {
        try { return process.PriorityClass.ToString(); } catch { return null; }
    }
}
