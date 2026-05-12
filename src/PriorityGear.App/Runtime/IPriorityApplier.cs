using PriorityGear.Core;

namespace PriorityGear.App.Runtime;

public interface IPriorityApplier
{
    PriorityApplyResult SetPriority(int processId, ProcessPriorityLevel priority);
}
