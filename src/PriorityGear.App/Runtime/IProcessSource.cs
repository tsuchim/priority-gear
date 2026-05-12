using PriorityGear.Core;

namespace PriorityGear.App.Runtime;

public interface IProcessSource
{
    IReadOnlyList<ProcessSnapshot> GetProcesses();
}
