using System.Diagnostics;
using PriorityGear.Contracts;
using PriorityGear.Core;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class ServiceCommandHandler(
    Win32PriorityApplier priorityApplier,
    MachineRuleStore machineRuleStore,
    Func<PrivilegeEnableResult> privilegeProvider)
{
    public ServiceResponse HandleStatus(ServiceRequest request)
    {
        return request.Kind switch
        {
            ServiceCommandKind.GetServiceStatus => StatusResponse(),
            ServiceCommandKind.GetMachineRules => RulesResponse(),
            _ => new ServiceResponse
            {
                Succeeded = false,
                Message = "This command is not available on the status pipe.",
                Authorization = ServiceAuthorization.AllowStatus().ToDto()
            }
        };
    }

    public ServiceResponse HandleAdmin(ServiceRequest request, ServiceAuthorizationResult authorization)
    {
        if (!authorization.CommandAllowed)
        {
            return new ServiceResponse
            {
                Succeeded = false,
                Message = authorization.DenialReason,
                Authorization = authorization.ToDto()
            };
        }

        ServiceResponse response = request.Kind switch
        {
            ServiceCommandKind.GetServiceStatus => StatusResponse(),
            ServiceCommandKind.GetMachineRules => RulesResponse(),
            ServiceCommandKind.TestApplyPriority => ApplyPriorityResponse(request, requireApprovedRule: false),
            ServiceCommandKind.ApplyApprovedMachineRule => ApplyPriorityResponse(request, requireApprovedRule: true),
            _ => new ServiceResponse { Succeeded = false, Message = "Unsupported command." }
        };

        response.Authorization = authorization.ToDto();
        return response;
    }

    private ServiceResponse StatusResponse()
    {
        PrivilegeEnableResult privilege = privilegeProvider();
        return new ServiceResponse
        {
            Succeeded = true,
            Message = "Service running.",
            Authorization = ServiceAuthorization.AllowStatus().ToDto(),
            Status = new ServiceStatusDto
            {
                ServiceRunning = true,
                ServiceAccount = Environment.UserName,
                SeDebugPrivilege = new PrivilegeStatusDto
                {
                    Attempted = privilege.Attempted,
                    Succeeded = privilege.Succeeded,
                    Status = privilege.Status.ToString(),
                    Win32Error = privilege.Win32Error
                }
            }
        };
    }

    private ServiceResponse RulesResponse()
    {
        return new ServiceResponse
        {
            Succeeded = true,
            Message = "Machine rules loaded.",
            Authorization = ServiceAuthorization.AllowStatus().ToDto(),
            MachineRules = machineRuleStore.Load().ToList()
        };
    }

    private ServiceResponse ApplyPriorityResponse(ServiceRequest request, bool requireApprovedRule)
    {
        if (request.ProcessId is null)
        {
            return new ServiceResponse { Succeeded = false, Message = "ProcessId is required." };
        }

        ProcessPriorityLevel? priority = request.Priority;
        if (requireApprovedRule)
        {
            if (!TryGetMatchingApprovedRule(request, out MachinePriorityRule? rule, out string ruleFailure))
            {
                return new ServiceResponse { Succeeded = false, Message = ruleFailure };
            }

            priority ??= rule.BasePriority;
        }

        if (priority is null)
        {
            return new ServiceResponse { Succeeded = false, Message = "Priority is required." };
        }

        Win32PriorityResult result = priorityApplier.SetPriority(request.ProcessId.Value, priority.Value);
        return new ServiceResponse
        {
            Succeeded = result.Succeeded,
            Message = result.Message,
            PriorityApply = new PriorityApplyDto
            {
                Succeeded = result.Succeeded,
                Status = result.Status.ToString(),
                Win32Error = result.Win32Error,
                Message = result.Message
            }
        };
    }

    private bool TryGetMatchingApprovedRule(ServiceRequest request, out MachinePriorityRule rule, out string failure)
    {
        rule = null!;
        failure = string.Empty;
        if (request.RuleId is null)
        {
            failure = "RuleId is required.";
            return false;
        }

        MachinePriorityRule? foundRule = machineRuleStore.Load().FirstOrDefault(candidate => candidate.Id == request.RuleId);
        if (foundRule is null)
        {
            failure = "Machine rule was not found.";
            return false;
        }

        rule = foundRule;

        if (!rule.Enabled)
        {
            failure = "Machine rule is disabled.";
            return false;
        }

        if (!rule.ApprovedByAdmin)
        {
            failure = "Machine rule is not approved by an administrator.";
            return false;
        }

        if (request.ProcessId is null)
        {
            failure = "ProcessId is required.";
            return false;
        }

        try
        {
            using Process process = Process.GetProcessById(request.ProcessId.Value);
            string executableName = process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? process.ProcessName
                : process.ProcessName + ".exe";

            if (!string.IsNullOrWhiteSpace(rule.ExecutableName) &&
                !string.Equals(rule.ExecutableName, executableName, StringComparison.OrdinalIgnoreCase))
            {
                failure = "Target process does not match the approved machine rule executable name.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(rule.FullPath))
            {
                string? path = null;
                try
                {
                    path = process.MainModule?.FileName;
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    failure = "Target process path is unavailable but the machine rule requires full-path matching.";
                    return false;
                }

                if (!string.Equals(rule.FullPath, path, StringComparison.OrdinalIgnoreCase))
                {
                    failure = "Target process does not match the approved machine rule path.";
                    return false;
                }
            }

            return true;
        }
        catch (ArgumentException)
        {
            failure = "Target process exited.";
            return false;
        }
    }
}
