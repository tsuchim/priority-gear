using PriorityGear.Core;

namespace PriorityGear.Core.Tests;

public sealed class CoreReservePlannerTests
{
    [Fact]
    public void Plan_RejectsReserveGreaterThanOrEqualToPhysicalCores()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [new PhysicalCoreInfo(0, 0b0011, 2)],
            reserveCount: 1);

        Assert.False(plan.Succeeded);
        Assert.Null(plan.AllowedLogicalProcessorMask);
    }

    [Fact]
    public void Plan_HeterogeneousDataReservesHighestEfficiencyClassFirst()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [
                new PhysicalCoreInfo(0, 0b0011, 0),
                new PhysicalCoreInfo(1, 0b1100, 1),
                new PhysicalCoreInfo(2, 0b110000, 2)
            ],
            reserveCount: 1);

        Assert.True(plan.Succeeded);
        Assert.Equal((ulong)0b001111, plan.AllowedLogicalProcessorMask);
        Assert.Contains("P-cores", plan.Message);
    }

    [Fact]
    public void Plan_MixedEfficiencyValuesReserveTwoBeforeOneBeforeZero()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [
                new PhysicalCoreInfo(0, 0b000011, 0),
                new PhysicalCoreInfo(1, 0b001100, 1),
                new PhysicalCoreInfo(2, 0b110000, 2)
            ],
            reserveCount: 2);

        Assert.True(plan.Succeeded);
        Assert.Equal((ulong)0b000011, plan.AllowedLogicalProcessorMask);
    }

    [Fact]
    public void Plan_UnknownEfficiencyDoesNotClaimPcorePreference()
    {
        CoreReservePlan plan = CoreReservePlanner.Plan(
            [
                new PhysicalCoreInfo(0, 0b0011, 0),
                new PhysicalCoreInfo(1, 0b1100, 0)
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
                new PhysicalCoreInfo(0, 0b0011, 2, ProcessorGroup: 0),
                new PhysicalCoreInfo(1, 0b1100, 2, ProcessorGroup: 1)
            ],
            reserveCount: 1);

        Assert.False(plan.Succeeded);
        Assert.Contains("processor groups", plan.Message);
    }
}
