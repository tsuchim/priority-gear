namespace PriorityGear.Setup;

public sealed record SetupPlanSummary(
    string ServiceName,
    string ServiceAccount,
    string ServiceBinaryPath,
    bool PreserveProgramData,
    bool PreserveMachineRules,
    bool PreserveLogs);

public static class SetupPlanner
{
    public static SetupPlanSummary CreateInstallOrUpdatePlan(SetupInstallPlan plan)
    {
        return new SetupPlanSummary(
            plan.ServiceName,
            "LocalSystem",
            plan.ServiceExePath,
            PreserveProgramData: true,
            PreserveMachineRules: true,
            PreserveLogs: true);
    }

    public static SetupPlanSummary CreateUninstallPlan(SetupInstallPlan plan)
    {
        return new SetupPlanSummary(
            plan.ServiceName,
            "LocalSystem",
            plan.ServiceExePath,
            PreserveProgramData: true,
            PreserveMachineRules: true,
            PreserveLogs: true);
    }
}
