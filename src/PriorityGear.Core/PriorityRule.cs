namespace PriorityGear.Core;

public sealed class PriorityRule
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public RuleMatchConditions Match { get; set; } = new();

    public ProcessPriorityLevel BasePriority { get; set; }

    public ProcessPriorityLevel ActivePriority { get; set; }

    public bool ActiveModeEnabled { get; set; }

    public RuleScope Scope { get; set; }

    public string? Notes { get; set; }

    public static PriorityRule ForExecutable(string executableName, string? fullPath = null)
    {
        return new PriorityRule
        {
            Id = Guid.NewGuid(),
            DisplayName = executableName,
            Enabled = true,
            Match = new RuleMatchConditions
            {
                ExecutableName = executableName,
                FullPath = fullPath
            },
            BasePriority = ProcessPriorityLevel.Normal,
            ActivePriority = ProcessPriorityLevel.AboveNormal,
            ActiveModeEnabled = true,
            Scope = RuleScope.CurrentUser
        };
    }
}
