namespace PriorityGear.Core;

public sealed class RuleMatchConditions
{
    public string? ExecutableName { get; set; }

    public string? FullPath { get; set; }

    public string? PathSuffix { get; set; }

    public string? CommandLinePattern { get; set; }

    public string? ServiceName { get; set; }

    public bool Matches(ProcessSnapshot process)
    {
        if (!string.IsNullOrWhiteSpace(ExecutableName) &&
            !string.Equals(ExecutableName, process.ExecutableName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(FullPath) &&
            !string.Equals(FullPath, process.ExecutablePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(PathSuffix) &&
            (process.ExecutablePath is null ||
             !process.ExecutablePath.EndsWith(PathSuffix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}
