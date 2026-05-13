using PriorityGear.Core;

namespace PriorityGear.Contracts;

public static class ServiceContractConstants
{
    public const string StatusPipeName = "PriorityGear.Service.Status.v0";
    public const string AdminPipeName = "PriorityGear.Service.Admin.v0";
}

public enum ServiceCommandKind
{
    GetServiceStatus,
    GetMachineRules,
    TestApplyPriority,
    ApplyApprovedMachineRule,
    ProbePriorityAccess,
    AddMachineRule,
    UpdateMachineRule,
    EnableMachineRule,
    DisableMachineRule,
    ApproveMachineRule,
    UnapproveMachineRule,
    DeleteMachineRule,
    ReloadMachineRules,
    ScanMachineRulesNow,
    DiscoverServiceProcesses
}

public sealed class ServiceRequest
{
    public ServiceCommandKind Kind { get; set; }

    public int? ProcessId { get; set; }

    public ProcessPriorityLevel? Priority { get; set; }

    public Guid? RuleId { get; set; }

    public string? ServiceName { get; set; }

    public MachinePriorityRule? MachineRule { get; set; }
}

public sealed class ServiceResponse
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = string.Empty;

    public ServiceStatusDto? Status { get; set; }

    public List<MachinePriorityRule>? MachineRules { get; set; }

    public PriorityApplyDto? PriorityApply { get; set; }

    public ServiceAuthorizationDto? Authorization { get; set; }

    public List<ServiceProcessInfoDto>? ServiceProcesses { get; set; }

    public ServiceProcessDiscoveryStatusDto? ServiceProcessDiscovery { get; set; }
}

public sealed class ServiceStatusDto
{
    public bool ServiceRunning { get; set; }

    public string ServiceAccount { get; set; } = string.Empty;

    public string ConfiguredServiceAccount { get; set; } = string.Empty;

    public string ProcessIdentity { get; set; } = string.Empty;

    public string NetworkIdentity { get; set; } = string.Empty;

    public PrivilegeStatusDto SeDebugPrivilege { get; set; } = new();

    public string AuthorizationMode { get; set; } = "AdministratorsOnlyForMutation";

    public MachineRuleMonitorStatusDto MachineRuleMonitor { get; set; } = new();

    public ServiceProcessDiscoveryStatusDto ServiceProcessDiscovery { get; set; } = new();
}

public sealed class MachineRuleMonitorStatusDto
{
    public bool MonitorRunning { get; set; }

    public DateTimeOffset? LastScanTime { get; set; }

    public DateTimeOffset? NextScanEstimate { get; set; }

    public int LoadedMachineRuleCount { get; set; }

    public int EnabledApprovedRuleCount { get; set; }

    public int MatchedProcessCount { get; set; }

    public int LastApplySuccesses { get; set; }

    public int LastApplyFailures { get; set; }

    public List<RuleRuntimeSummaryDto> Rules { get; set; } = [];

    public List<ProcessRuntimeSummaryDto> Processes { get; set; } = [];
}

public sealed class RuleRuntimeSummaryDto
{
    public Guid RuleId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int MatchedProcessCount { get; set; }
}

public sealed class ProcessRuntimeSummaryDto
{
    public int ProcessId { get; set; }

    public string ExecutableName { get; set; } = string.Empty;

    public Guid RuleId { get; set; }

    public string DesiredPriority { get; set; } = string.Empty;

    public string LastResult { get; set; } = string.Empty;

    public DateTimeOffset? LastApplyTime { get; set; }
}

public sealed class PrivilegeStatusDto
{
    public bool Attempted { get; set; }

    public bool Succeeded { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? Win32Error { get; set; }
}

public sealed class PriorityApplyDto
{
    public bool Succeeded { get; set; }

    public string Status { get; set; } = string.Empty;

    public int? Win32Error { get; set; }

    public string Message { get; set; } = string.Empty;
}

public sealed class ServiceAuthorizationDto
{
    public string? CallerName { get; set; }

    public string? CallerSid { get; set; }

    public bool IsAdministrator { get; set; }

    public string AuthorizationSource { get; set; } = "Unavailable";

    public bool CommandAllowed { get; set; }

    public string DenialReason { get; set; } = string.Empty;
}

public sealed class ServiceProcessInfoDto
{
    public int ProcessId { get; set; }

    public string ExecutableName { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string? Owner { get; set; }

    public List<string> ServiceNames { get; set; } = [];

    public bool SharedServiceHost { get; set; }

    public string PriorityAccessStatus { get; set; } = string.Empty;

    public string? CurrentPriority { get; set; }
}

public sealed class ServiceProcessDiscoveryStatusDto
{
    public bool Available { get; set; }

    public int RunningServiceCount { get; set; }

    public int ServiceHostProcessCount { get; set; }

    public int SharedHostProcessCount { get; set; }

    public int TotalDiscoveredGroupCount { get; set; }

    public int ReturnedGroupCount { get; set; }

    public bool Truncated { get; set; }

    public int Limit { get; set; }

    public string Message { get; set; } = string.Empty;
}
