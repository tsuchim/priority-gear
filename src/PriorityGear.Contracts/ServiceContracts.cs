using PriorityGear.Core;

namespace PriorityGear.Contracts;

public static class ServiceContractConstants
{
    public const string PipeName = "PriorityGear.Service.v0";
}

public enum ServiceCommandKind
{
    GetServiceStatus,
    GetMachineRules,
    TestApplyPriority,
    ApplyApprovedMachineRule
}

public sealed class ServiceRequest
{
    public ServiceCommandKind Kind { get; set; }

    public int? ProcessId { get; set; }

    public ProcessPriorityLevel? Priority { get; set; }

    public Guid? RuleId { get; set; }
}

public sealed class ServiceResponse
{
    public bool Succeeded { get; set; }

    public string Message { get; set; } = string.Empty;

    public ServiceStatusDto? Status { get; set; }

    public List<MachinePriorityRule>? MachineRules { get; set; }

    public PriorityApplyDto? PriorityApply { get; set; }
}

public sealed class ServiceStatusDto
{
    public bool ServiceRunning { get; set; }

    public string ServiceAccount { get; set; } = string.Empty;

    public PrivilegeStatusDto SeDebugPrivilege { get; set; } = new();

    public string AuthorizationMode { get; set; } = "AdministratorsOnlyForMutation";
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
