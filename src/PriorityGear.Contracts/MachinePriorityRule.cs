using PriorityGear.Core;

namespace PriorityGear.Contracts;

public sealed class MachinePriorityRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string? ExecutableName { get; set; }

    public string? FullPath { get; set; }

    public string? PathSuffix { get; set; }

    public string? Notes { get; set; }

    public string? ServiceName { get; set; }

    public bool AllowSharedServiceHost { get; set; }

    public bool DryRunOnly { get; set; }

    public ProcessPriorityLevel BasePriority { get; set; } = ProcessPriorityLevel.Normal;

    public ProcessPriorityLevel? ActivePriority { get; set; }

    public bool ApprovedByAdmin { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? UpdatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
