using PriorityGear.Core;

namespace PriorityGear.Windows;

public sealed record Win32PriorityResult(
    bool Succeeded,
    Win32PriorityStatus Status,
    ProcessPriorityLevel RequestedPriority,
    int? Win32Error,
    string Message);
