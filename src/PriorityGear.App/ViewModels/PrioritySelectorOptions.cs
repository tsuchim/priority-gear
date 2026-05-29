using PriorityGear.Core;

namespace PriorityGear.App.ViewModels;

public sealed record PriorityOption(string Label, ProcessPriorityLevel? Priority)
{
    public override string ToString() => Label;
}

public static class PrioritySelectorOptions
{
    public static IReadOnlyList<ProcessPriorityLevel> ConcretePriorities { get; } =
    [
        ProcessPriorityLevel.High,
        ProcessPriorityLevel.AboveNormal,
        ProcessPriorityLevel.Normal,
        ProcessPriorityLevel.BelowNormal,
        ProcessPriorityLevel.Idle
    ];

    public static IReadOnlyList<PriorityOption> ActivePriorities { get; } =
    [
        new("Same as normal", null),
        new("High", ProcessPriorityLevel.High),
        new("Above Normal", ProcessPriorityLevel.AboveNormal),
        new("Normal", ProcessPriorityLevel.Normal),
        new("Below Normal", ProcessPriorityLevel.BelowNormal),
        new("Idle", ProcessPriorityLevel.Idle)
    ];
}
