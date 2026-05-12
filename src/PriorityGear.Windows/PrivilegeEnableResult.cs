namespace PriorityGear.Windows;

public sealed record PrivilegeEnableResult(
    bool Attempted,
    bool Succeeded,
    Win32PriorityStatus Status,
    int? Win32Error,
    string Message);
