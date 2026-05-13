using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PriorityGear.Windows;

public sealed class PrivilegeService
{
    private const string SeDebugPrivilege = "SeDebugPrivilege";
    private const uint TokenAdjustPrivileges = 0x0020;
    private const uint TokenQuery = 0x0008;
    private const uint SePrivilegeEnabled = 0x00000002;

    public PrivilegeEnableResult EnableSeDebugPrivilege()
    {
        if (!NativeMethods.OpenProcessToken(Process.GetCurrentProcess().Handle, TokenAdjustPrivileges | TokenQuery, out nint token))
        {
            int error = Marshal.GetLastWin32Error();
            return new PrivilegeEnableResult(true, false, Win32ErrorMapper.Map(error), error, new Win32Exception(error).Message);
        }

        try
        {
            if (!NativeMethods.LookupPrivilegeValue(null, SeDebugPrivilege, out Luid luid))
            {
                int error = Marshal.GetLastWin32Error();
                return new PrivilegeEnableResult(true, false, Win32ErrorMapper.Map(error), error, new Win32Exception(error).Message);
            }

            TokenPrivileges privileges = new()
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SePrivilegeEnabled
            };

            if (!NativeMethods.AdjustTokenPrivileges(token, false, ref privileges, 0, nint.Zero, nint.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                return new PrivilegeEnableResult(true, false, Win32ErrorMapper.Map(error), error, new Win32Exception(error).Message);
            }

            int lastError = Marshal.GetLastWin32Error();
            if (lastError == Win32ErrorMapper.ErrorNotAllAssigned)
            {
                return new PrivilegeEnableResult(true, false, Win32PriorityStatus.PrivilegeUnavailable, lastError, "SeDebugPrivilege is not assigned to this token.");
            }

            return new PrivilegeEnableResult(true, true, Win32PriorityStatus.Success, null, "SeDebugPrivilege enabled.");
        }
        finally
        {
            _ = NativeMethods.CloseHandle(token);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenPrivileges
    {
        public uint PrivilegeCount;
        public Luid Luid;
        public uint Attributes;
    }

    private static partial class NativeMethods
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool LookupPrivilegeValue(string? systemName, string name, out Luid luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(nint tokenHandle, bool disableAllPrivileges, ref TokenPrivileges newState, uint bufferLength, nint previousState, nint returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(nint handle);
    }
}
