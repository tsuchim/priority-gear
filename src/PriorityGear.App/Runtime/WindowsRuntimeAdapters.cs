using PriorityGear.Core;
using PriorityGear.Windows;

namespace PriorityGear.App.Runtime;

public sealed class WindowsProcessSource(ProcessPriorityService service) : IProcessSource
{
    public IReadOnlyList<ProcessSnapshot> GetProcesses()
    {
        return service.GetProcesses();
    }
}

public sealed class WindowsPriorityApplier(ProcessPriorityService service) : IPriorityApplier
{
    public PriorityApplyResult SetPriority(int processId, ProcessPriorityLevel priority)
    {
        return service.SetPriority(processId, priority);
    }
}

public sealed class WindowsForegroundProcessSource(ForegroundWindowProvider provider) : IForegroundProcessSource
{
    public int? GetForegroundProcessId()
    {
        return provider.GetForegroundProcessId();
    }
}
