using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PriorityGear.App.Runtime;
using PriorityGear.App.Storage;
using PriorityGear.App.ViewModels;
using PriorityGear.Contracts;
using PriorityGear.Core;
using PriorityGear.Windows;
using Forms = System.Windows.Forms;

namespace PriorityGear.App;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ProcessRowViewModel> _processes = [];
    private readonly ObservableCollection<RuleViewModel> _rules = [];
    private readonly ObservableCollection<string> _logs = [];
    private readonly RuleStore _ruleStore = new();
    private readonly SystemModeClient _systemModeClient = new();
    private readonly MonitoringController _monitoringController;
    private readonly DispatcherTimer _timer = new();
    private Forms.NotifyIcon? _notifyIcon;

    public MainWindow()
    {
        InitializeComponent();

        ProcessPriorityService processPriorityService = new();
        _monitoringController = new MonitoringController(
            new WindowsProcessSource(processPriorityService),
            new WindowsPriorityApplier(processPriorityService),
            new WindowsForegroundProcessSource(new ForegroundWindowProvider()));
        _monitoringController.LogProduced += (_, entry) => Log(entry.ToString());

        ProcessGrid.ItemsSource = _processes;
        RuleGrid.ItemsSource = _rules;
        LogList.ItemsSource = _logs;

        foreach (DataGridComboBoxColumn column in RuleGrid.Columns.OfType<DataGridComboBoxColumn>())
        {
            column.ItemsSource = Enum.GetValues<ProcessPriorityLevel>();
        }

        LoadRules();
        ConfigureTimer();
        ConfigureTray();
        RefreshSnapshot(_monitoringController.Refresh(DateTimeOffset.Now));
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }

    private void ConfigureTimer()
    {
        _timer.Interval = MonitoringOptions.Default.ForegroundPollingInterval;
        _timer.Tick += (_, _) => RefreshSnapshot(_monitoringController.Tick(DateTimeOffset.Now));
    }

    private void ConfigureTray()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "PriorityGear",
            Icon = LoadTrayIcon(),
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Items.Add("Open", null, (_, _) => ShowFromTray());
        _notifyIcon.ContextMenuStrip.Items.Add("Start monitoring", null, (_, _) => Dispatcher.Invoke(StartMonitoring));
        _notifyIcon.ContextMenuStrip.Items.Add("Stop monitoring", null, (_, _) => Dispatcher.Invoke(StopMonitoring));
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(Close));
        _notifyIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        System.Windows.Resources.StreamResourceInfo? resource = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Assets/prioritygear.ico"));
        if (resource?.Stream is null)
        {
            return System.Drawing.SystemIcons.Application;
        }

        using Stream stream = resource.Stream;
        using System.Drawing.Icon icon = new(stream);
        return (System.Drawing.Icon)icon.Clone();
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshSnapshot(_monitoringController.Refresh(DateTimeOffset.Now));
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartMonitoring();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopMonitoring();
    }

    private void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProcessGrid.SelectedItem is not ProcessRowViewModel selected)
        {
            Log("Select a process before adding a rule.");
            return;
        }

        PriorityRule rule = PriorityRule.ForExecutable(selected.ExecutableName, selected.Process.ExecutablePath);
        _rules.Add(new RuleViewModel(rule));
        CommitRules("Rule added.");
    }

    private void DeleteRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (RuleGrid.SelectedItem is not RuleViewModel selected)
        {
            Log("Select a rule before deleting.");
            return;
        }

        _rules.Remove(selected);
        CommitRules("Rule deleted.");
    }

    private void SaveRulesButton_Click(object sender, RoutedEventArgs e)
    {
        CommitRules("Rules saved.");
    }

    private async void RefreshServiceButton_Click(object sender, RoutedEventArgs e)
    {
        ServiceResponse response = await _systemModeClient.GetStatusAsync(TimeSpan.FromSeconds(2));
        if (response.Succeeded && response.Status is not null)
        {
            SystemModeStatusText.Text = FormatSystemModeStatus(response.Status);
        }
        else
        {
            SystemModeStatusText.Text = $"System Mode: unavailable ({response.Message})";
        }
    }

    public static string FormatSystemModeStatus(ServiceStatusDto status)
    {
        MachineRuleMonitorStatusDto monitor = status.MachineRuleMonitor;
        ServiceProcessDiscoveryStatusDto discovery = status.ServiceProcessDiscovery;
        string lastScan = monitor.LastScanTime?.LocalDateTime.ToString() ?? "never";
        string versionPath = string.IsNullOrWhiteSpace(status.ServiceVersionDirectory)
            ? status.ServiceBinaryPath
            : status.ServiceVersionDirectory;
        string discoveryCompleteness = discovery.Truncated
            ? $"{discovery.ReturnedGroupCount}/{discovery.TotalDiscoveredGroupCount} returned, truncated"
            : $"{discovery.ReturnedGroupCount}/{discovery.TotalDiscoveredGroupCount} returned";

        return
            $"System Mode: running={status.ServiceRunning}, configured={status.ConfiguredServiceAccount}, identity={status.ProcessIdentity}, network={status.NetworkIdentity}, SeDebug={status.SeDebugPrivilege.Status}{Environment.NewLine}" +
            $"Service: versionPath={versionPath}, binary={status.ServiceBinaryPath}{Environment.NewLine}" +
            $"Monitor: running={monitor.MonitorRunning}, rules={monitor.LoadedMachineRuleCount}, approved={monitor.EnabledApprovedRuleCount}, matched={monitor.MatchedProcessCount}, lastScan={lastScan}, results={monitor.LastApplySuccesses}/{monitor.LastApplyFailures}{Environment.NewLine}" +
            $"Discovery: available={discovery.Available}, services={discovery.RunningServiceCount}, hosts={discovery.ServiceHostProcessCount}, shared={discovery.SharedHostProcessCount}, {discoveryCompleteness}, limit={discovery.Limit}";
    }

    private void RuleGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(() => CommitRules("Rule edited."));
    }

    private void RuleGrid_CurrentCellChanged(object sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() => CommitRules("Rule edited."));
    }

    private void StartMonitoring()
    {
        _timer.Start();
        RefreshSnapshot(_monitoringController.Start(DateTimeOffset.Now));
    }

    private void StopMonitoring()
    {
        _timer.Stop();
        RefreshSnapshot(_monitoringController.Stop(DateTimeOffset.Now));
    }

    private void LoadRules()
    {
        RuleStoreLoadResult result = _ruleStore.Load();
        if (!result.Succeeded)
        {
            Log($"Rule load failed: {result.ErrorMessage}. File was not overwritten: {result.Path}");
            return;
        }

        foreach (PriorityRule rule in result.Rules)
        {
            _rules.Add(new RuleViewModel(rule));
        }

        Log($"Rules loaded from {result.Path}.");
        UpdateControllerRules();
    }

    private void CommitRules(string successMessage)
    {
        UpdateControllerRules();
        RuleStoreSaveResult result = _ruleStore.Save(_rules.Select(static r => r.Rule));
        if (result.Succeeded)
        {
            Log(successMessage);
        }
        else
        {
            Log($"Rule save failed: {result.ErrorMessage}");
        }

        RefreshSnapshot(_monitoringController.Refresh(DateTimeOffset.Now));
    }

    private void UpdateControllerRules()
    {
        _monitoringController.SetRules(_rules.Select(static r => r.Rule).ToList());
    }

    private void RefreshSnapshot(MonitoringSnapshot snapshot)
    {
        _processes.Clear();
        foreach (ProcessSnapshot process in snapshot.Processes.OrderBy(static p => p.ExecutableName).ThenBy(static p => p.ProcessId))
        {
            _processes.Add(ProcessRowViewModel.From(process, snapshot));
        }

        StatusText.Text = snapshot.IsRunning
            ? $"Monitoring running - {_processes.Count} processes - {_rules.Count} rules"
            : $"Monitoring stopped - {_processes.Count} processes - {_rules.Count} rules";
    }

    private void Log(string message)
    {
        _logs.Insert(0, $"{DateTimeOffset.Now:HH:mm:ss} {message}");
        while (_logs.Count > 300)
        {
            _logs.RemoveAt(_logs.Count - 1);
        }
    }
}
