using System.ComponentModel;
using System.Runtime.InteropServices;
using PriorityGear.Core;

namespace PriorityGear.Windows;

public sealed class Win32PriorityApplier
{
    private const uint ProcessSetInformation = 0x0200;

    public Win32PriorityResult SetPriority(int processId, ProcessPriorityLevel priority)
    {
        nint handle = NativeMethods.OpenProcess(ProcessSetInformation, false, (uint)processId);
        if (handle == 0)
        {
            int error = Marshal.GetLastWin32Error();
            return Failure(priority, error);
        }

        try
        {
            if (!NativeMethods.SetPriorityClass(handle, (uint)WindowsPriorityMapper.ToWindows(priority)))
            {
                int error = Marshal.GetLastWin32Error();
                return Failure(priority, error);
            }

            return new Win32PriorityResult(true, Win32PriorityStatus.Success, priority, null, "Priority applied.");
        }
        finally
        {
            _ = NativeMethods.CloseHandle(handle);
        }
    }

    public Win32PriorityResult ProbeSetPriorityAccess(int processId)
    {
        nint handle = NativeMethods.OpenProcess(ProcessSetInformation, false, (uint)processId);
        if (handle == 0)
        {
            int error = Marshal.GetLastWin32Error();
            return Failure(ProcessPriorityLevel.Normal, error);
        }

        _ = NativeMethods.CloseHandle(handle);
        return new Win32PriorityResult(true, Win32PriorityStatus.Success, ProcessPriorityLevel.Normal, null, "Priority write access is available.");
    }

    private static Win32PriorityResult Failure(ProcessPriorityLevel priority, int error)
    {
        return new Win32PriorityResult(false, Win32ErrorMapper.Map(error), priority, error, new Win32Exception(error).Message);
    }

    private static partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern nint OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetPriorityClass(nint processHandle, uint priorityClass);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(nint handle);
    }
}
