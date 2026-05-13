using System.Diagnostics;
using System.Runtime.InteropServices;
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
        foreach (ServiceProcessRecord service in EnumerateServiceProcesses())
        {
            int pid = service.ProcessId;
            if (pid <= 0)
            {
                continue;
            }

            if (!byPid.TryGetValue(pid, out ServiceProcessInfoDto? info))
            {
                info = CreateProcessInfo(pid);
                byPid[pid] = info;
            }

            info.ServiceNames.Add(service.ServiceName);
            info.SharedServiceHost = info.ServiceNames.Count > 1;
            info.Owner ??= TryGetRegistryValue(service.ServiceName, "ObjectName");
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
            IReadOnlyList<ServiceProcessRecord> records = EnumerateServiceProcesses();
            ServiceProcessRecord? service = records
                .FirstOrDefault(candidate => string.Equals(candidate.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
            if (service is null || service.ProcessId <= 0)
            {
                return null;
            }

            ServiceProcessInfoDto info = CreateProcessInfo(service.ProcessId);
            info.ServiceNames.Add(service.ServiceName);
            info.Owner = TryGetRegistryValue(service.ServiceName, "ObjectName");
            info.SharedServiceHost = records.Count(candidate => candidate.ProcessId == service.ProcessId) > 1;
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
        ServiceProcessInfoDto? info = null;
        foreach (ServiceProcessRecord service in EnumerateServiceProcesses())
        {
            if (service.ProcessId != processId)
            {
                continue;
            }

            info ??= CreateProcessInfo(processId);
            info.ServiceNames.Add(service.ServiceName);
            info.Owner ??= TryGetRegistryValue(service.ServiceName, "ObjectName");
        }

        if (info is not null)
        {
            info.SharedServiceHost = info.ServiceNames.Count > 1;
        }

        return info;
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

    private static IReadOnlyList<ServiceProcessRecord> EnumerateServiceProcesses()
    {
        nint manager = OpenSCManager(null, null, ScManagerEnumerateService);
        if (manager == 0)
        {
            return [];
        }

        try
        {
            _ = EnumServicesStatusEx(
                manager,
                ScEnumProcessInfo,
                ServiceWin32,
                ServiceStateAll,
                nint.Zero,
                0,
                out int bytesNeeded,
                out _,
                out _,
                null);

            if (bytesNeeded <= 0)
            {
                return [];
            }

            nint buffer = Marshal.AllocHGlobal(bytesNeeded);
            try
            {
                if (!EnumServicesStatusEx(
                    manager,
                    ScEnumProcessInfo,
                    ServiceWin32,
                    ServiceStateAll,
                    buffer,
                    bytesNeeded,
                    out _,
                    out int servicesReturned,
                    out _,
                    null))
                {
                    return [];
                }

                List<ServiceProcessRecord> records = [];
                int size = Marshal.SizeOf<EnumServiceStatusProcess>();
                for (int index = 0; index < servicesReturned; index++)
                {
                    nint item = nint.Add(buffer, index * size);
                    EnumServiceStatusProcess status = Marshal.PtrToStructure<EnumServiceStatusProcess>(item);
                    string serviceName = Marshal.PtrToStringUni(status.ServiceName) ?? string.Empty;
                    string displayName = Marshal.PtrToStringUni(status.DisplayName) ?? string.Empty;
                    records.Add(new ServiceProcessRecord(serviceName, displayName, unchecked((int)status.Status.ProcessId)));
                }

                return records;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            CloseServiceHandle(manager);
        }
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

    private sealed record ServiceProcessRecord(string ServiceName, string DisplayName, int ProcessId);

    private const int ScManagerEnumerateService = 0x0004;
    private const int ScEnumProcessInfo = 0;
    private const int ServiceWin32 = 0x00000030;
    private const int ServiceStateAll = 0x00000003;

    [StructLayout(LayoutKind.Sequential)]
    private struct EnumServiceStatusProcess
    {
        public nint ServiceName;
        public nint DisplayName;
        public ServiceStatusProcess Status;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatusProcess
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
        public uint ProcessId;
        public uint ServiceFlags;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern nint OpenSCManager(string? machineName, string? databaseName, int desiredAccess);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool CloseServiceHandle(nint handle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumServicesStatusEx(
        nint serviceManager,
        int infoLevel,
        int serviceType,
        int serviceState,
        nint services,
        int bufferSize,
        out int bytesNeeded,
        out int servicesReturned,
        out int resumeHandle,
        string? groupName);
}
