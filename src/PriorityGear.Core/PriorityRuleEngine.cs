namespace PriorityGear.Core;

public sealed class PriorityRuleEngine
{
    public PriorityDecision? Decide(ProcessSnapshot process, IEnumerable<PriorityRule> rules, int? foregroundProcessId)
    {
        foreach (PriorityRule rule in rules)
        {
            if (!rule.Enabled || rule.Scope != RuleScope.CurrentUser || !rule.Match.Matches(process))
            {
                continue;
            }

            bool isForegroundActive = foregroundProcessId == process.ProcessId;
            ProcessPriorityLevel desiredPriority = rule.ActiveModeEnabled && isForegroundActive
                ? rule.ActivePriority
                : rule.BasePriority;

            return new PriorityDecision(rule, process, isForegroundActive, desiredPriority);
        }

        return null;
    }
}
