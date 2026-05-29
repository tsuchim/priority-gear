namespace PriorityGear.Core;

public sealed record RuleApplicationSignature(Guid RuleId, ProcessPriorityLevel DesiredPriority, int CoreReserve)
{
    public static RuleApplicationSignature FromDecision(PriorityDecision decision)
    {
        return new RuleApplicationSignature(
            decision.Rule.Id,
            decision.DesiredPriority,
            decision.Rule.CoreReserve);
    }
}
