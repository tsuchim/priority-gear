using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class PriorityGearWorker(
    ILogger<PriorityGearWorker> logger,
    PrivilegeService privilegeService,
    Win32PriorityApplier priorityApplier,
    MachineRuleStore machineRuleStore,
    MachineRuleMonitor machineRuleMonitor,
    ServiceProcessDiscovery serviceProcessDiscovery,
    ServiceFileLog serviceLog) : BackgroundService
{
    private PrivilegeEnableResult _privilege = new(false, false, Win32PriorityStatus.PrivilegeUnavailable, null, "Not attempted.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        serviceLog.Startup();
        _privilege = privilegeService.EnableSeDebugPrivilege();
        logger.LogInformation("PriorityGear Service started. SeDebugPrivilege: {Status}", _privilege.Status);
        serviceLog.Info($"SeDebugPrivilege result: Attempted={_privilege.Attempted}; Succeeded={_privilege.Succeeded}; Status={_privilege.Status}; Win32Error={_privilege.Win32Error}; Message={_privilege.Message}");

        ServiceCommandHandler handler = new(priorityApplier, machineRuleStore, machineRuleMonitor, serviceProcessDiscovery, () => _privilege);
        StatusPipeServer statusPipeServer = new(handler, serviceLog);
        AdminPipeServer adminPipeServer = new(handler, serviceLog);
        serviceLog.Info("Status and admin pipe servers starting.");

        await Task.WhenAll(
            statusPipeServer.RunAsync(stoppingToken),
            adminPipeServer.RunAsync(stoppingToken),
            RunMonitorAsync(stoppingToken));
    }

    private async Task RunMonitorAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await machineRuleMonitor.ScanAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                serviceLog.Error(ex, "Machine rule monitor scan failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
