namespace PriorityGear.Windows;

public static class Win32ErrorMapper
{
    public const int ErrorAccessDenied = 5;
    public const int ErrorInvalidParameter = 87;
    public const int ErrorInvalidHandle = 6;
    public const int ErrorNotAllAssigned = 1300;

    public static Win32PriorityStatus Map(int error)
    {
        return error switch
        {
            ErrorAccessDenied => Win32PriorityStatus.AccessDenied,
            ErrorInvalidParameter => Win32PriorityStatus.InvalidParameter,
            ErrorInvalidHandle => Win32PriorityStatus.ProcessExited,
            ErrorNotAllAssigned => Win32PriorityStatus.PrivilegeUnavailable,
            _ => Win32PriorityStatus.UnknownWin32Error
        };
    }
}
