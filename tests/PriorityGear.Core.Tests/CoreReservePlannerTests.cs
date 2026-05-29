using PriorityGear.Core;

namespace PriorityGear.Core.Tests;

public sealed class CoreReservePlannerTests
{
    [Fact]
    public void Plan_RejectsReserveGreaterThanOrEqualToPhysicalCores()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [new PhysicalCoreInfo(0, 0b0011, CoreEfficiencyClass.Performance)],
            reserveCount: 1);

        Assert.False(plan.Succeeded);
        Assert.Null(plan.AllowedLogicalProcessorMask);
    }

    [Fact]
    public void Plan_ReservesPerformanceCoresFirst()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [
                new PhysicalCoreInfo(0, 0b0011, CoreEfficiencyClass.Efficiency),
                new PhysicalCoreInfo(1, 0b1100, CoreEfficiencyClass.Performance),
                new PhysicalCoreInfo(2, 0b110000, CoreEfficiencyClass.Performance)
            ],
            reserveCount: 1);

        Assert.True(plan.Succeeded);
        Assert.Equal((ulong)0b110011, plan.AllowedLogicalProcessorMask);
        Assert.Contains("P-cores", plan.Message);
    }

    [Fact]
    public void Plan_UnknownEfficiencyDoesNotClaimPcorePreference()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [
                new PhysicalCoreInfo(0, 0b0011, CoreEfficiencyClass.Unknown),
                new PhysicalCoreInfo(1, 0b1100, CoreEfficiencyClass.Unknown)
            ],
            reserveCount: 1);

        Assert.True(plan.Succeeded);
        Assert.DoesNotContain("preferring P-cores", plan.Message);
    }

    [Fact]
    public void Plan_RejectsNegativeReserve()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan([], reserveCount: -1);

        Assert.False(plan.Succeeded);
    }

    [Fact]
    public void Plan_RejectsMultipleProcessorGroups()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [
                new PhysicalCoreInfo(0, 0b0011, CoreEfficiencyClass.Performance, ProcessorGroup: 0),
                new PhysicalCoreInfo(1, 0b1100, CoreEfficiencyClass.Performance, ProcessorGroup: 1)
            ],
            reserveCount: 1);

        Assert.False(plan.Succeeded);
        Assert.Contains("processor groups", plan.Message);
    }
}
