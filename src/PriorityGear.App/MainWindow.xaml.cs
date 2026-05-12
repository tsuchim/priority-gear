using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PriorityGear.Core;
using PriorityGear.Windows;
using Forms = System.Windows.Forms;

namespace PriorityGear.App;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ProcessSnapshot> _processes = [];
    private readonly ObservableCollection<PriorityRule> _rules = [];
    private readonly ObservableCollection<string> _logs = [];
    private readonly Dictionary<int, ManagedProcessState> _states = [];
    private readonly ProcessPriorityService _processPriorityService = new();
    private readonly ForegroundWindowProvider _foregroundWindowProvider = new();
    private readonly PriorityRuleEngine _ruleEngine = new();
    private readonly RuleStore _ruleStore = new();
    private readonly DispatcherTimer _foregroundTimer = new();
    private readonly DispatcherTimer _rescanTimer = new();
    private readonly DispatcherTimer _reapplyTimer = new();
    private Forms.NotifyIcon? _notifyIcon;
    private bool _monitoring;

    public MainWindow()
    {
        InitializeComponent();
        ProcessGrid.ItemsSource = _processes;
        RuleGrid.ItemsSource = _rules;
        LogList.ItemsSource = _logs;

        foreach (DataGridComboBoxColumn column in RuleGrid.Columns.OfType<DataGridComboBoxColumn>())
        {
            column.ItemsSource = Enum.GetValues<ProcessPriorityLevel>();
        }

        foreach (PriorityRule rule in _ruleStore.Load())
        {
            _rules.Add(rule);
        }

        ConfigureTimers();
        ConfigureTray();
        RefreshProcesses();
        SetStatus("Stopped");
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }

    private void ConfigureTimers()
    {
        _foregroundTimer.Interval = TimeSpan.FromMilliseconds(500);
        _foregroundTimer.Tick += (_, _) => ApplyRules();

        _rescanTimer.Interval = TimeSpan.FromSeconds(10);
        _rescanTimer.Tick += (_, _) => RefreshProcesses();

        _reapplyTimer.Interval = TimeSpan.FromSeconds(30);
        _reapplyTimer.Tick += (_, _) => ApplyRules(forceRecheck: true);
    }

    private void ConfigureTray()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "PriorityGear",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Items.Add("Open", null, (_, _) => ShowFromTray());
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => Close());
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshProcesses();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _monitoring = true;
        _foregroundTimer.Start();
        _rescanTimer.Start();
        _reapplyTimer.Start();
        Log("Monitoring started.");
        ApplyRules(forceRecheck: true);
        SetStatus("Running");
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _monitoring = false;
        _foregroundTimer.Stop();
        _rescanTimer.Stop();
        _reapplyTimer.Stop();
        Log("Monitoring stopped.");
        SetStatus("Stopped");
    }

    private void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProcessGrid.SelectedItem is not ProcessSnapshot process)
        {
            Log("Select a process before adding a rule.");
            return;
        }

        PriorityRule rule = PriorityRule.ForExecutable(process.ExecutableName, process.ExecutablePath);
        _rules.Add(rule);
        SaveRules();
        Log($"Added rule for {process.ExecutableName}.");
    }

    private void RuleGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(SaveRules);
    }

    private void RefreshProcesses()
    {
        IReadOnlyList<ProcessSnapshot> snapshots = _processPriorityService.GetProcesses();
        _processes.Clear();
        foreach (ProcessSnapshot snapshot in snapshots)
        {
            _processes.Add(snapshot);
        }

        SetStatus(_monitoring ? $"Running - {snapshots.Count} processes" : $"Stopped - {snapshots.Count} processes");
    }

    private void ApplyRules(bool forceRecheck = false)
    {
        int? foregroundProcessId = _foregroundWindowProvider.GetForegroundProcessId();

        foreach (ProcessSnapshot process in _processes)
        {
            PriorityDecision? decision = _ruleEngine.Decide(process, _rules, foregroundProcessId);
            if (decision is null)
            {
                continue;
            }

            if (process.Capability != ProcessCapability.ControllableNow)
            {
                Log($"{process.ExecutableName} ({process.ProcessId}) is not controllable: {process.Capability}.");
                continue;
            }

            _states.TryGetValue(process.ProcessId, out ManagedProcessState? state);
            if (!forceRecheck && !decision.ShouldApply(state))
            {
                continue;
            }

            PriorityApplyResult result = _processPriorityService.SetPriority(process.ProcessId, decision.DesiredPriority);
            ManagedProcessState nextState = state ?? new ManagedProcessState
            {
                ProcessId = process.ProcessId,
                ExecutablePath = process.ExecutablePath,
                RuleId = decision.Rule.Id
            };

            nextState.CurrentDesiredPriority = decision.DesiredPriority;
            nextState.IsForegroundActive = decision.IsForegroundActive;
            nextState.LastApplyResult = result;
            nextState.LastApplyTime = DateTimeOffset.Now;
            nextState.LastError = result.Succeeded ? null : result.Message;
            if (result.Succeeded)
            {
                nextState.LastAppliedPriority = decision.DesiredPriority;
            }

            _states[process.ProcessId] = nextState;
            Log(result.Succeeded
                ? $"{process.ExecutableName} ({process.ProcessId}) -> {decision.DesiredPriority}"
                : $"{process.ExecutableName} ({process.ProcessId}) failed: {result.Message}");
        }
    }

    private void SaveRules()
    {
        _ruleStore.Save(_rules);
    }

    private void SetStatus(string status)
    {
        StatusText.Text = status;
    }

    private void Log(string message)
    {
        _logs.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} {message}");
        while (_logs.Count > 200)
        {
            _logs.RemoveAt(_logs.Count - 1);
        }
    }
}
