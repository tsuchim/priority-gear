namespace PriorityGear.Windows;

public enum Win32PriorityStatus
{
    Success,
    AccessDenied,
    ProcessExited,
    InvalidParameter,
    ProtectedOrUnsupported,
    PrivilegeUnavailable,
    PrivilegeEnableFailed,
    UnknownWin32Error
}
