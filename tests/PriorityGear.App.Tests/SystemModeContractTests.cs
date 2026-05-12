using System.Text.Json;
using PriorityGear.Contracts;
using PriorityGear.Core;

namespace PriorityGear.App.Tests;

public sealed class SystemModeContractTests
{
    [Fact]
    public void MachineRuleSerialization_RoundTripsApprovedAdminState()
    {
        MachinePriorityRule rule = new()
        {
            DisplayName = "machine test",
            Enabled = true,
            ExecutableName = "example.exe",
            BasePriority = ProcessPriorityLevel.High,
            ApprovedByAdmin = true,
            CreatedBy = "admin"
        };

        string json = JsonSerializer.Serialize(rule, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        MachinePriorityRule? roundTrip = JsonSerializer.Deserialize<MachinePriorityRule>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(roundTrip);
        Assert.True(roundTrip.ApprovedByAdmin);
        Assert.Equal(ProcessPriorityLevel.High, roundTrip.BasePriority);
    }

    [Fact]
    public void SystemModeScope_RemainsSeparateFromUserModeRules()
    {
        PriorityRule userRule = PriorityRule.ForExecutable("example.exe");
        MachinePriorityRule machineRule = new() { ExecutableName = "example.exe", ApprovedByAdmin = true };

        Assert.Equal(RuleScope.CurrentUser, userRule.Scope);
        Assert.True(machineRule.ApprovedByAdmin);
    }

    [Fact]
    public void ServiceContract_DoesNotDefineUnrestrictedGenericSetPriorityCommand()
    {
        string[] names = Enum.GetNames<ServiceCommandKind>();

        Assert.DoesNotContain("SetAnyProcessPriority", names);
        Assert.Contains(nameof(ServiceCommandKind.ApplyApprovedMachineRule), names);
    }

    [Fact]
    public void ServiceStatusContract_ReportsPrivilegeAndAuthorizationMode()
    {
        ServiceStatusDto status = new()
        {
            ServiceRunning = true,
            ServiceAccount = "LocalSystem",
            AuthorizationMode = "AdministratorsOnlyForMutation",
            SeDebugPrivilege = new PrivilegeStatusDto { Attempted = true, Succeeded = false, Status = "PrivilegeUnavailable" }
        };

        Assert.True(status.ServiceRunning);
        Assert.Equal("AdministratorsOnlyForMutation", status.AuthorizationMode);
        Assert.True(status.SeDebugPrivilege.Attempted);
    }
}
