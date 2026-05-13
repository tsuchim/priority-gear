using System.Diagnostics;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public static class MachineRuleMatcher
{
    public static bool IsRuntimeEligible(MachinePriorityRule rule)
    {
        return rule.Enabled &&
            rule.ApprovedByAdmin &&
            !rule.DryRunOnly &&
            !IsUnsafeSvchostExecutableOnlyRule(rule);
    }

    public static bool IsUnsafeSvchostExecutableOnlyRule(MachinePriorityRule rule)
    {
        return string.IsNullOrWhiteSpace(rule.ServiceName) &&
            string.Equals(rule.ExecutableName, "svchost.exe", StringComparison.OrdinalIgnoreCase);
    }

    public static bool Matches(MachinePriorityRule rule, Process process, out string failure)
    {
        failure = string.Empty;
        string executableName = process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? process.ProcessName
            : process.ProcessName + ".exe";

        if (!string.IsNullOrWhiteSpace(rule.ExecutableName) &&
            !string.Equals(rule.ExecutableName, executableName, StringComparison.OrdinalIgnoreCase))
        {
            failure = "Target process does not match executable name.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.FullPath) || !string.IsNullOrWhiteSpace(rule.PathSuffix))
        {
            string? path;
            try
            {
                path = process.MainModule?.FileName;
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                failure = "Target process path is unavailable but the rule requires path matching.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(rule.FullPath) &&
                !string.Equals(rule.FullPath, path, StringComparison.OrdinalIgnoreCase))
            {
                failure = "Target process does not match full path.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(rule.PathSuffix) &&
                (path is null || !path.EndsWith(rule.PathSuffix, StringComparison.OrdinalIgnoreCase)))
            {
                failure = "Target process does not match path suffix.";
                return false;
            }
        }

        return true;
    }
}
