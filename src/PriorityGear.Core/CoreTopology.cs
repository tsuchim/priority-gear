namespace PriorityGear.Core;

public enum CoreEfficiencyClass
{
    Unknown,
    Efficiency,
    Performance
}

public sealed record PhysicalCoreInfo(int CoreIndex, ulong LogicalProcessorMask, CoreEfficiencyClass EfficiencyClass, ushort ProcessorGroup = 0);

public sealed record CoreReservePlan(bool Succeeded, ulong? AllowedLogicalProcessorMask, string Message)
{
    public static CoreReservePlan Success(ulong mask, string message) => new(true, mask, message);

    public static CoreReservePlan Failure(string message) => new(false, null, message);
}

public static class CoreReservePlanner
{
    public static CoreReservePlan Plan(IReadOnlyList<PhysicalCoreInfo> cores, int reserveCount)
    {
        if (reserveCount < 0)
        {
            return CoreReservePlan.Failure("Core Reserve must be a non-negative integer.");
        }

        if (reserveCount == 0)
        {
            return CoreReservePlan.Success(0, "Core Reserve is disabled.");
        }

        if (cores.Count == 0)
        {
            return CoreReservePlan.Failure("Physical core topology is unavailable.");
        }

        if (reserveCount >= cores.Count)
        {
            return CoreReservePlan.Failure("Core Reserve must be less than the number of physical cores.");
        }

        if (cores.Any(static c => c.LogicalProcessorMask == 0))
        {
            return CoreReservePlan.Failure("Physical core topology contains an empty logical processor mask.");
        }

        if (cores.Any(static c => c.ProcessorGroup != 0))
        {
            return CoreReservePlan.Failure("Core Reserve does not support systems that require multiple processor groups.");
        }

        List<PhysicalCoreInfo> reserved = cores
            .OrderByDescending(static c => c.EfficiencyClass == CoreEfficiencyClass.Performance)
            .ThenBy(static c => c.CoreIndex)
            .Take(reserveCount)
            .ToList();

        ulong allMask = 0;
        foreach (PhysicalCoreInfo core in cores)
        {
            allMask |= core.LogicalProcessorMask;
        }

        ulong reservedMask = 0;
        foreach (PhysicalCoreInfo core in reserved)
        {
            reservedMask |= core.LogicalProcessorMask;
        }

        ulong allowedMask = allMask & ~reservedMask;
        if (allowedMask == 0)
        {
            return CoreReservePlan.Failure("Core Reserve would leave no logical processors available.");
        }

        bool hasPerformanceClass = cores.Any(static c => c.EfficiencyClass == CoreEfficiencyClass.Performance);
        string message = hasPerformanceClass
            ? $"Reserved {reserveCount} physical core(s), preferring P-cores."
            : $"Reserved {reserveCount} physical core(s); P-core/E-core distinction is unavailable.";

        return CoreReservePlan.Success(allowedMask, message);
    }
}
