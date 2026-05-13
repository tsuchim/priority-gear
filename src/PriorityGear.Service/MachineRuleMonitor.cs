using System.Diagnostics;
using PriorityGear.Contracts;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class MachineRuleMonitor(
    MachineRuleStore store,
    Win32PriorityApplier priorityApplier,
    ServiceFileLog log)
{
    private readonly Dictionary<string, ProcessRuntimeSummaryDto> _processes = [];
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
        _loadError = string.Empty;
        log.Info($"Machine rules loaded. Count={_rules.Count}");
    }

    public Task ScanAsync(CancellationToken cancellationToken)
    {
        Reload();
        _lastScan = DateTimeOffset.Now;
        _successes = 0;
        _failures = 0;

        if (!string.IsNullOrEmpty(_loadError))
        {
            return Task.CompletedTask;
        }

        foreach (Process process in Process.GetProcesses())
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (process)
            {
                foreach (MachinePriorityRule rule in _rules.Where(MachineRuleMatcher.IsRuntimeEligible))
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
        if (_processes.TryGetValue(key, out ProcessRuntimeSummaryDto? existing) &&
            string.Equals(existing.LastResult, "Success", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Win32PriorityResult result = priorityApplier.SetPriority(process.Id, rule.BasePriority);
        if (result.Succeeded)
        {
            _successes++;
        }
        else
        {
            _failures++;
        }

        _processes[key] = new ProcessRuntimeSummaryDto
        {
            ProcessId = process.Id,
            ExecutableName = process.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? process.ProcessName : process.ProcessName + ".exe",
            RuleId = rule.Id,
            DesiredPriority = rule.BasePriority.ToString(),
            LastResult = result.Succeeded ? "Success" : $"{result.Status}: {result.Message}",
            LastApplyTime = DateTimeOffset.Now
        };
    }
}
