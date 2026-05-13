using System.Diagnostics;
using PriorityGear.Contracts;
using PriorityGear.Core;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class ServiceCommandHandler(
    Win32PriorityApplier priorityApplier,
    MachineRuleStore machineRuleStore,
    MachineRuleMonitor machineRuleMonitor,
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
            ServiceCommandKind.ProbePriorityAccess => ProbePriorityAccessResponse(request),
            ServiceCommandKind.AddMachineRule => AddRuleResponse(request),
            ServiceCommandKind.UpdateMachineRule => UpdateRuleResponse(request),
            ServiceCommandKind.EnableMachineRule => SetRuleEnabledResponse(request, true),
            ServiceCommandKind.DisableMachineRule => SetRuleEnabledResponse(request, false),
            ServiceCommandKind.ApproveMachineRule => SetRuleApprovedResponse(request, true),
            ServiceCommandKind.UnapproveMachineRule => SetRuleApprovedResponse(request, false),
            ServiceCommandKind.DeleteMachineRule => DeleteRuleResponse(request),
            ServiceCommandKind.ReloadMachineRules => ReloadRulesResponse(),
            ServiceCommandKind.ScanMachineRulesNow => ScanRulesResponse(),
            ServiceCommandKind.DiscoverServiceProcesses => DiscoverServiceProcessesResponse(),
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
                ConfiguredServiceAccount = "LocalSystem",
                ProcessIdentity = System.Security.Principal.WindowsIdentity.GetCurrent().Name,
                NetworkIdentity = Environment.UserName,
                SeDebugPrivilege = new PrivilegeStatusDto
                {
                    Attempted = privilege.Attempted,
                    Succeeded = privilege.Succeeded,
                    Status = privilege.Status.ToString(),
                    Win32Error = privilege.Win32Error
                },
                MachineRuleMonitor = machineRuleMonitor.GetStatus()
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
            MachineRules = machineRuleStore.TryLoad().Rules.ToList()
        };
    }

    private ServiceResponse AddRuleResponse(ServiceRequest request)
    {
        if (request.MachineRule is null) return new ServiceResponse { Succeeded = false, Message = "MachineRule is required." };
        if (!TryLoadRulesForMutation(out List<MachinePriorityRule> rules, out ServiceResponse failure)) return failure;
        request.MachineRule.Id = request.MachineRule.Id == Guid.Empty ? Guid.NewGuid() : request.MachineRule.Id;
        request.MachineRule.CreatedAt = DateTimeOffset.UtcNow;
        rules.Add(request.MachineRule);
        return SaveRules(rules, "Machine rule added.");
    }

    private ServiceResponse UpdateRuleResponse(ServiceRequest request)
    {
        if (request.MachineRule is null) return new ServiceResponse { Succeeded = false, Message = "MachineRule is required." };
        if (!TryLoadRulesForMutation(out List<MachinePriorityRule> rules, out ServiceResponse failure)) return failure;
        int index = rules.FindIndex(rule => rule.Id == request.MachineRule.Id);
        if (index < 0) return new ServiceResponse { Succeeded = false, Message = "Machine rule was not found." };
        request.MachineRule.UpdatedAt = DateTimeOffset.UtcNow;
        rules[index] = request.MachineRule;
        return SaveRules(rules, "Machine rule updated.");
    }

    private ServiceResponse SetRuleEnabledResponse(ServiceRequest request, bool enabled)
    {
        return MutateRule(request, rule => rule.Enabled = enabled, enabled ? "Machine rule enabled." : "Machine rule disabled.");
    }

    private ServiceResponse SetRuleApprovedResponse(ServiceRequest request, bool approved)
    {
        return MutateRule(request, rule => rule.ApprovedByAdmin = approved, approved ? "Machine rule approved." : "Machine rule unapproved.");
    }

    private ServiceResponse DeleteRuleResponse(ServiceRequest request)
    {
        if (request.RuleId is null) return new ServiceResponse { Succeeded = false, Message = "RuleId is required." };
        if (!TryLoadRulesForMutation(out List<MachinePriorityRule> rules, out ServiceResponse failure)) return failure;
        int removed = rules.RemoveAll(rule => rule.Id == request.RuleId.Value);
        return removed == 0 ? new ServiceResponse { Succeeded = false, Message = "Machine rule was not found." } : SaveRules(rules, "Machine rule deleted.");
    }

    private ServiceResponse ReloadRulesResponse()
    {
        machineRuleMonitor.Reload();
        return new ServiceResponse { Succeeded = true, Message = "Machine rules reloaded.", Status = StatusResponse().Status };
    }

    private ServiceResponse ScanRulesResponse()
    {
        machineRuleMonitor.ScanAsync(CancellationToken.None).GetAwaiter().GetResult();
        return new ServiceResponse { Succeeded = true, Message = "Machine rule scan completed.", Status = StatusResponse().Status };
    }

    private ServiceResponse DiscoverServiceProcessesResponse()
    {
        List<ServiceProcessInfoDto> processes = Process.GetProcesses().Take(100).Select(process =>
        {
            using (process)
            {
                return new ServiceProcessInfoDto
                {
                    ProcessId = process.Id,
                    ExecutableName = process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? process.ProcessName : process.ProcessName + ".exe",
                    Path = TryGetPath(process),
                    CurrentPriority = TryGetPriority(process),
                    PriorityAccessStatus = priorityApplier.ProbeSetPriorityAccess(process.Id).Status.ToString()
                };
            }
        }).ToList();
        return new ServiceResponse { Succeeded = true, Message = "Service process discovery completed.", ServiceProcesses = processes };
    }

    private ServiceResponse MutateRule(ServiceRequest request, Action<MachinePriorityRule> mutate, string message)
    {
        if (request.RuleId is null) return new ServiceResponse { Succeeded = false, Message = "RuleId is required." };
        if (!TryLoadRulesForMutation(out List<MachinePriorityRule> rules, out ServiceResponse failure)) return failure;
        MachinePriorityRule? rule = rules.FirstOrDefault(candidate => candidate.Id == request.RuleId.Value);
        if (rule is null) return new ServiceResponse { Succeeded = false, Message = "Machine rule was not found." };
        mutate(rule);
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        return SaveRules(rules, message);
    }

    private ServiceResponse SaveRules(IReadOnlyList<MachinePriorityRule> rules, string message)
    {
        MachineRuleStoreResult result = machineRuleStore.Save(rules);
        if (!result.Succeeded) return new ServiceResponse { Succeeded = false, Message = result.Error };
        machineRuleMonitor.Reload();
        return new ServiceResponse { Succeeded = true, Message = message, MachineRules = result.Rules.ToList() };
    }

    private bool TryLoadRulesForMutation(out List<MachinePriorityRule> rules, out ServiceResponse failure)
    {
        MachineRuleStoreResult load = machineRuleStore.TryLoad();
        if (!load.Succeeded)
        {
            rules = [];
            failure = new ServiceResponse { Succeeded = false, Message = $"Machine rule file could not be loaded and was not overwritten: {load.Error}" };
            return false;
        }

        rules = load.Rules.ToList();
        failure = new ServiceResponse();
        return true;
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

    private ServiceResponse ProbePriorityAccessResponse(ServiceRequest request)
    {
        if (request.ProcessId is null)
        {
            return new ServiceResponse { Succeeded = false, Message = "ProcessId is required." };
        }

        Win32PriorityResult result = priorityApplier.ProbeSetPriorityAccess(request.ProcessId.Value);
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

        MachinePriorityRule? foundRule = machineRuleStore.TryLoad().Rules.FirstOrDefault(candidate => candidate.Id == request.RuleId);
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
            return MachineRuleMatcher.Matches(rule, process, out failure);
        }
        catch (ArgumentException)
        {
            failure = "Target process exited.";
            return false;
        }
    }

    private static string? TryGetPath(Process process)
    {
        try { return process.MainModule?.FileName; } catch { return null; }
    }

    private static string? TryGetPriority(Process process)
    {
        try { return process.PriorityClass.ToString(); } catch { return null; }
    }
}
