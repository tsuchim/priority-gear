using PriorityGear.Core;

namespace PriorityGear.App.Runtime;

public interface ICoreAffinityApplier
{
    CoreReserveApplyResult ApplyCoreReserve(int processId, int reserveCount);
}

public sealed class NoOpCoreAffinityApplier : ICoreAffinityApplier
{
    public CoreReserveApplyResult ApplyCoreReserve(int processId, int reserveCount)
    {
        return reserveCount == 0
            ? CoreReserveApplyResult.Disabled()
            : CoreReserveApplyResult.Failure(reserveCount, "Core Reserve affinity support is unavailable.", "Unsupported");
    }
}
