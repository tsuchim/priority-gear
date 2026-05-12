using PriorityGear.Core;

namespace PriorityGear.App.Runtime;

public sealed record MonitoringSnapshot(
    IReadOnlyList<ProcessSnapshot> Processes,
    IReadOnlyDictionary<int, ManagedProcessState> States,
    IReadOnlyDictionary<int, PriorityDecision> Decisions,
    bool IsRunning,
    DateTimeOffset CapturedAt);
