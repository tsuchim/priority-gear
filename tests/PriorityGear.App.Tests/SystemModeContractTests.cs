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
            ServiceBinaryPath = @"C:\Program Files\PriorityGear\versions\20260514-004830\PriorityGear.Service.exe",
            ServiceVersionDirectory = @"C:\Program Files\PriorityGear\versions\20260514-004830",
            AuthorizationMode = "AdministratorsOnlyForMutation",
            SeDebugPrivilege = new PrivilegeStatusDto { Attempted = true, Succeeded = false, Status = "PrivilegeUnavailable" }
        };

        Assert.True(status.ServiceRunning);
        Assert.Equal("AdministratorsOnlyForMutation", status.AuthorizationMode);
        Assert.Contains("versions", status.ServiceVersionDirectory);
        Assert.True(status.SeDebugPrivilege.Attempted);
    }

    [Fact]
    public void SystemModeStatusFormatting_IncludesMonitorAndDiscoverySummary()
    {
        ServiceStatusDto status = new()
        {
            ServiceRunning = true,
            ConfiguredServiceAccount = "LocalSystem",
            ProcessIdentity = @"NT AUTHORITY\SYSTEM",
            ServiceBinaryPath = @"C:\Program Files\PriorityGear\versions\20260514-004830\PriorityGear.Service.exe",
            ServiceVersionDirectory = @"C:\Program Files\PriorityGear\versions\20260514-004830",
            SeDebugPrivilege = new PrivilegeStatusDto { Status = "Success" },
            MachineRuleMonitor = new MachineRuleMonitorStatusDto
            {
                MonitorRunning = true,
                LoadedMachineRuleCount = 2,
                EnabledApprovedRuleCount = 1,
                MatchedProcessCount = 1,
                LastApplySuccesses = 1,
                LastApplyFailures = 0
            },
            ServiceProcessDiscovery = new ServiceProcessDiscoveryStatusDto
            {
                Available = true,
                RunningServiceCount = 120,
                ServiceHostProcessCount = 80,
                SharedHostProcessCount = 20,
                TotalDiscoveredGroupCount = 171,
                ReturnedGroupCount = 100,
                Truncated = true,
                Limit = 100
            }
        };

        string text = PriorityGear.App.MainWindow.FormatSystemModeStatus(status);

        Assert.Contains("configured=LocalSystem", text);
        Assert.Contains("identity=NT AUTHORITY\\SYSTEM", text);
        Assert.Contains("versions\\20260514-004830", text);
        Assert.Contains("matched=1", text);
        Assert.Contains("services=120", text);
        Assert.Contains("100/171 returned, truncated", text);
    }
}
