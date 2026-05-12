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
        string? executablePath = null;
        ProcessPriorityLevel? currentPriority = null;
        ProcessCapability capability = ProcessCapability.ControllableNow;
        bool pathAvailable = true;
        bool priorityReadable = true;
        bool priorityWritableLikely = true;
        ProcessStatus status = ProcessStatus.Ready;
        string? message = null;

        try
        {
            executablePath = process.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                executableName = Path.GetFileName(executablePath);
            }
            else
            {
                pathAvailable = false;
                status = ProcessStatus.PathUnavailable;
            }
        }
        catch (Win32Exception ex)
        {
            pathAvailable = false;
            status = ProcessStatus.PathUnavailable;
            message = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            pathAvailable = false;
            capability = ProcessCapability.UnknownError;
            status = ProcessStatus.ProcessExited;
            message = ex.Message;
        }

        try
        {
            currentPriority = WindowsPriorityMapper.FromWindows(process.PriorityClass);
        }
        catch (Win32Exception ex)
        {
            priorityReadable = false;
            priorityWritableLikely = false;
            capability = capability == ProcessCapability.ControllableNow
                ? ProcessCapability.ProtectedOrUnsupported
                : capability;
            status = ProcessStatus.CurrentPriorityUnavailable;
            message ??= ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            priorityReadable = false;
            priorityWritableLikely = false;
            capability = ProcessCapability.UnknownError;
            status = ProcessStatus.ProcessExited;
            message ??= ex.Message;
        }

        return new ProcessSnapshot(process.Id, executableName, executablePath, currentPriority, capability)
        {
            Inspection = new ProcessInspection(
                true,
                pathAvailable,
                priorityReadable,
                priorityWritableLikely,
                status,
                message)
        };
    }
}
