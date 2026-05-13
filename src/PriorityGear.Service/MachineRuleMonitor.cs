using System.Diagnostics;
using PriorityGear.Contracts;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class MachineRuleMonitor(
    MachineRuleStore store,
    Win32PriorityApplier priorityApplier,
    ServiceProcessDiscovery serviceProcessDiscovery,
    ServiceFileLog log)
{
    private readonly Dictionary<string, ProcessRuntimeSummaryDto> _processes = [];
    private readonly HashSet<string> _lastSuccessfulApplications = [];
    private readonly TimeSpan _scanInterval = TimeSpan.FromSeconds(30);
    private DateTimeOffset? _lastScan;
    private IReadOnlyList<MachinePriorityRule> _rules = [];
    private string _loadError = string.Empty;
    private int _successes;
    private int _failures;

    public void Reload()
    {
        MachineRuleStoreResult result = store.TryLoad();
        if (!result.Succeeded)
        {
            _loadError = result.Error;
            log.Info($"Machine rule load failed: {result.Error}");
            return;
        }

        _rules = result.Rules;
        PruneStateForLoadedRules();
        _loadError = string.Empty;
        log.Info($"Machine rules loaded. Count={_rules.Count}");
    }

    public Task ScanAsync(CancellationToken cancellationToken)
    {
        Reload();
        _lastScan = DateTimeOffset.Now;
        _successes = 0;
        _failures = 0;
        _processes.Clear();

        if (!string.IsNullOrEmpty(_loadError))
        {
            return Task.CompletedTask;
        }

        foreach (MachinePriorityRule serviceRule in _rules.Where(rule => rule.Enabled && rule.ApprovedByAdmin && !string.IsNullOrWhiteSpace(rule.ServiceName)))
        {
            ApplyServiceRule(serviceRule);
        }

        foreach (Process process in Process.GetProcesses())
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (process)
            {
                foreach (MachinePriorityRule rule in _rules.Where(rule => MachineRuleMatcher.IsRuntimeEligible(rule) && string.IsNullOrWhiteSpace(rule.ServiceName)))
                {
                    if (!MachineRuleMatcher.Matches(rule, process, out _))
                    {
                        continue;
                    }

                    ApplyRule(rule, process);
                    break;
                }
            }
        }

        return Task.CompletedTask;
    }

    public MachineRuleMonitorStatusDto GetStatus()
    {
        List<RuleRuntimeSummaryDto> ruleSummaries = _rules
            .Take(20)
            .Select(rule => new RuleRuntimeSummaryDto
            {
                RuleId = rule.Id,
                DisplayName = rule.DisplayName,
                MatchedProcessCount = _processes.Values.Count(process => process.RuleId == rule.Id)
            })
            .ToList();

        return new MachineRuleMonitorStatusDto
        {
            MonitorRunning = true,
            LastScanTime = _lastScan,
            NextScanEstimate = _lastScan?.Add(_scanInterval),
            LoadedMachineRuleCount = _rules.Count,
            EnabledApprovedRuleCount = _rules.Count(MachineRuleMatcher.IsRuntimeEligible),
            MatchedProcessCount = _processes.Count,
            LastApplySuccesses = _successes,
            LastApplyFailures = _failures,
            Rules = ruleSummaries,
            Processes = _processes.Values.Take(50).ToList()
        };
    }

    private void ApplyRule(MachinePriorityRule rule, Process process)
    {
        string key = $"{process.Id}:{rule.Id}:{rule.BasePriority}";
        if (!rule.DryRunOnly && _lastSuccessfulApplications.Contains(key))
        {
            _processes[key] = CreateProcessSummary(rule, process.Id, process.ProcessName, "AlreadyApplied");
            return;
        }

        Win32PriorityResult result = rule.DryRunOnly
            ? new Win32PriorityResult(true, Win32PriorityStatus.Success, rule.BasePriority, null, "DryRun")
            : priorityApplier.SetPriority(process.Id, rule.BasePriority);
        if (result.Succeeded)
        {
            _successes++;
            if (!rule.DryRunOnly)
            {
                _lastSuccessfulApplications.Add(key);
            }
        }
        else
        {
            _failures++;
            _lastSuccessfulApplications.Remove(key);
        }

        _processes[key] = CreateProcessSummary(
            rule,
            process.Id,
            process.ProcessName,
            rule.DryRunOnly ? "DryRun" : result.Succeeded ? "Success" : $"{result.Status}: {result.Message}");
    }

    private void ApplyServiceRule(MachinePriorityRule rule)
    {
        ServiceProcessInfoDto? serviceProcess = serviceProcessDiscovery.FindByServiceName(rule.ServiceName!);
        if (serviceProcess is null)
        {
            return;
        }

        if (serviceProcess.SharedServiceHost && !rule.AllowSharedServiceHost)
        {
            _failures++;
            _processes[$"{serviceProcess.ProcessId}:{rule.Id}:shared"] = new ProcessRuntimeSummaryDto
            {
                ProcessId = serviceProcess.ProcessId,
                ExecutableName = serviceProcess.ExecutableName,
                RuleId = rule.Id,
                DesiredPriority = rule.BasePriority.ToString(),
                LastResult = "SharedHostRejected",
                LastApplyTime = DateTimeOffset.Now
            };
            return;
        }

        if (string.Equals(serviceProcess.ExecutableName, "svchost.exe", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(rule.ServiceName))
        {
            return;
        }

        try
        {
            using Process process = Process.GetProcessById(serviceProcess.ProcessId);
            if (MachineRuleMatcher.Matches(rule, process, out _))
            {
                ApplyRule(rule, process);
            }
        }
        catch
        {
        }
    }

    private void PruneStateForLoadedRules()
    {
        HashSet<Guid> ruleIds = _rules.Select(rule => rule.Id).ToHashSet();
        foreach (string key in _lastSuccessfulApplications.ToList())
        {
            if (!ruleIds.Any(ruleId => key.Contains($":{ruleId}:", StringComparison.OrdinalIgnoreCase)))
            {
                _lastSuccessfulApplications.Remove(key);
            }
        }

        foreach (string key in _processes.Keys.ToList())
        {
            if (!ruleIds.Any(ruleId => key.Contains($":{ruleId}:", StringComparison.OrdinalIgnoreCase)))
            {
                _processes.Remove(key);
            }
        }
    }

    private static ProcessRuntimeSummaryDto CreateProcessSummary(MachinePriorityRule rule, int processId, string processName, string result)
    {
        return new ProcessRuntimeSummaryDto
        {
            ProcessId = processId,
            ExecutableName = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? processName : processName + ".exe",
            RuleId = rule.Id,
            DesiredPriority = rule.BasePriority.ToString(),
            LastResult = result,
            LastApplyTime = DateTimeOffset.Now
        };
    }
}
