using PriorityGear.Core;

namespace PriorityGear.App.Runtime;

public sealed class MonitoringController
{
    private readonly IProcessSource _processSource;
    private readonly IPriorityApplier _priorityApplier;
    private readonly IForegroundProcessSource _foregroundProcessSource;
    private readonly PriorityRuleEngine _ruleEngine = new();
    private readonly MonitoringOptions _options;
    private readonly Dictionary<int, ManagedProcessState> _states = [];
    private readonly Dictionary<string, DateTimeOffset> _failureLogTimes = [];
    private IReadOnlyList<ProcessSnapshot> _processes = [];
    private IReadOnlyList<PriorityRule> _rules = [];
    private bool _running;
    private DateTimeOffset _lastRescan = DateTimeOffset.MinValue;
    private DateTimeOffset _lastReapply = DateTimeOffset.MinValue;

    public MonitoringController(
        IProcessSource processSource,
        IPriorityApplier priorityApplier,
        IForegroundProcessSource foregroundProcessSource,
        MonitoringOptions? options = null)
    {
        _processSource = processSource;
        _priorityApplier = priorityApplier;
        _foregroundProcessSource = foregroundProcessSource;
        _options = options ?? MonitoringOptions.Default;
    }

    public event EventHandler<MonitoringLogEntry>? LogProduced;

    public bool IsRunning => _running;

    public void SetRules(IReadOnlyList<PriorityRule> rules)
    {
        _rules = rules;
    }

    public MonitoringSnapshot Start(DateTimeOffset now)
    {
        _running = true;
        _lastReapply = now;
        Log("monitoring", "Monitoring started.");
        return RescanAndApply(now, forceReapply: true);
    }

    public MonitoringSnapshot Stop(DateTimeOffset now)
    {
        _running = false;
        Log("monitoring", "Monitoring stopped.");
        return Snapshot(now, new Dictionary<int, PriorityDecision>());
    }

    public MonitoringSnapshot Refresh(DateTimeOffset now)
    {
        Rescan(now);
        return _running ? Apply(now, forceReapply: true) : Snapshot(now, BuildDecisions());
    }

    public MonitoringSnapshot Tick(DateTimeOffset now)
    {
        if (!_running)
        {
            return Snapshot(now, BuildDecisions());
        }

        bool shouldRescan = now - _lastRescan >= _options.ProcessRescanInterval;
        bool forceReapply = now - _lastReapply >= _options.ReapplyInterval;

        if (shouldRescan)
        {
            Rescan(now);
        }

        MonitoringSnapshot snapshot = Apply(now, forceReapply);
        if (forceReapply)
        {
            _lastReapply = now;
        }

        return snapshot;
    }

    public MonitoringSnapshot RescanAndApply(DateTimeOffset now, bool forceReapply)
    {
        Rescan(now);
        return _running ? Apply(now, forceReapply) : Snapshot(now, BuildDecisions());
    }

    private void Rescan(DateTimeOffset now)
    {
        HashSet<int> previous = [.. _states.Keys];
        _processes = _processSource.GetProcesses();
        HashSet<int> current = [.. _processes.Select(static p => p.ProcessId)];

        foreach (int exitedProcessId in previous.Except(current).ToList())
        {
            ManagedProcessState state = _states[exitedProcessId];
            state.LastError = "Process exited.";
            _states.Remove(exitedProcessId);
            Log("process", $"Process {exitedProcessId} exited.");
        }

        _lastRescan = now;
    }

    private MonitoringSnapshot Apply(DateTimeOffset now, bool forceReapply)
    {
        IReadOnlyDictionary<int, PriorityDecision> decisions = BuildDecisions();

        foreach (PriorityDecision decision in decisions.Values)
        {
            ProcessSnapshot process = decision.Process;
            if (!process.Inspection.PriorityWriteLikelyPossible)
            {
                LogThrottledFailure(now, process, decision, process.Inspection.Status.ToString(), process.Inspection.Message ?? process.Inspection.Status.ToString());
                continue;
            }

            _states.TryGetValue(process.ProcessId, out ManagedProcessState? state);
            if (!forceReapply && !decision.ShouldApply(state))
            {
                continue;
            }

            PriorityApplyResult result = _priorityApplier.SetPriority(process.ProcessId, decision.DesiredPriority);
            _states[process.ProcessId] = ManagedProcessStateUpdater.FromDecision(decision, state, result, now);

            if (result.Succeeded)
            {
                Log("apply", $"{process.ExecutableName} ({process.ProcessId}) -> {decision.DesiredPriority}");
            }
            else
            {
                LogThrottledFailure(now, process, decision, result.ErrorCode ?? "ApplyFailed", result.Message);
            }
        }

        return Snapshot(now, decisions);
    }

    private IReadOnlyDictionary<int, PriorityDecision> BuildDecisions()
    {
        int? foregroundProcessId = _foregroundProcessSource.GetForegroundProcessId();
        Dictionary<int, PriorityDecision> decisions = [];

        foreach (ProcessSnapshot process in _processes)
        {
            PriorityDecision? decision = _ruleEngine.Decide(process, _rules, foregroundProcessId);
            if (decision is not null)
            {
                decisions[process.ProcessId] = decision;
            }
        }

        return decisions;
    }

    private MonitoringSnapshot Snapshot(DateTimeOffset now, IReadOnlyDictionary<int, PriorityDecision> decisions)
    {
        return new MonitoringSnapshot(
            _processes,
            new Dictionary<int, ManagedProcessState>(_states),
            decisions,
            _running,
            now);
    }

    private void Log(string category, string message)
    {
        LogProduced?.Invoke(this, new MonitoringLogEntry(DateTimeOffset.Now, category, message));
    }

    private void LogThrottledFailure(
        DateTimeOffset now,
        ProcessSnapshot process,
        PriorityDecision decision,
        string failureCategory,
        string message)
    {
        string key = $"{process.ProcessId}:{decision.Rule.Id}:{decision.DesiredPriority}:{failureCategory}";
        if (_failureLogTimes.TryGetValue(key, out DateTimeOffset last) &&
            now - last < _options.FailureLogThrottle)
        {
            return;
        }

        _failureLogTimes[key] = now;
        Log("failure", $"{process.ExecutableName} ({process.ProcessId}) failed for {decision.DesiredPriority}: {message}");
    }
}
