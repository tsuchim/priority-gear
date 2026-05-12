using System.ComponentModel;
using System.Diagnostics;
using PriorityGear.Core;

namespace PriorityGear.Windows;

public sealed class ProcessPriorityService
{
    public IReadOnlyList<ProcessSnapshot> GetProcesses()
    {
        List<ProcessSnapshot> snapshots = [];

        foreach (Process process in Process.GetProcesses().OrderBy(static p => p.ProcessName))
        {
            using (process)
            {
                snapshots.Add(CreateSnapshot(process));
            }
        }

        return snapshots;
    }

    public PriorityApplyResult SetPriority(int processId, ProcessPriorityLevel priority)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            process.PriorityClass = WindowsPriorityMapper.ToWindows(priority);
            return PriorityApplyResult.Success(priority);
        }
        catch (Win32Exception ex)
        {
            return PriorityApplyResult.Failure(priority, ex.Message, ex.NativeErrorCode.ToString());
        }
        catch (UnauthorizedAccessException ex)
        {
            return PriorityApplyResult.Failure(priority, ex.Message, "UnauthorizedAccess");
        }
        catch (InvalidOperationException ex)
        {
            return PriorityApplyResult.Failure(priority, ex.Message, "InvalidOperation");
        }
        catch (ArgumentException ex)
        {
            return PriorityApplyResult.Failure(priority, ex.Message, "ProcessNotFound");
        }
    }

    private static ProcessSnapshot CreateSnapshot(Process process)
    {
        string executableName = process.ProcessName + ".exe";
        string executablePath = string.Empty;
        ProcessPriorityLevel? currentPriority = null;
        ProcessCapability capability = ProcessCapability.ControllableNow;

        try
        {
            executablePath = process.MainModule?.FileName ?? string.Empty;
            executableName = Path.GetFileName(executablePath);
        }
        catch (Win32Exception)
        {
            capability = ProcessCapability.AdministratorRequired;
        }
        catch (InvalidOperationException)
        {
            capability = ProcessCapability.UnknownError;
        }

        try
        {
            currentPriority = WindowsPriorityMapper.FromWindows(process.PriorityClass);
        }
        catch (Win32Exception)
        {
            capability = capability == ProcessCapability.ControllableNow
                ? ProcessCapability.ProtectedOrUnsupported
                : capability;
        }
        catch (InvalidOperationException)
        {
            capability = ProcessCapability.UnknownError;
        }

        return new ProcessSnapshot(process.Id, executableName, executablePath, currentPriority, capability);
    }
}
