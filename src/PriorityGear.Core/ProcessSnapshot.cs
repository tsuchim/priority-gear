namespace PriorityGear.Core;

public sealed record ProcessSnapshot(
    int ProcessId,
    string ExecutableName,
    string? ExecutablePath,
    ProcessPriorityLevel? CurrentPriority,
    ProcessCapability Capability)
{
    public ProcessInspection Inspection { get; init; } = ProcessInspection.Ready;
}
