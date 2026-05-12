using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class PriorityGearWorker(
    ILogger<PriorityGearWorker> logger,
    PrivilegeService privilegeService,
    Win32PriorityApplier priorityApplier,
    MachineRuleStore machineRuleStore) : BackgroundService
{
    private PrivilegeEnableResult _privilege = new(false, false, Win32PriorityStatus.PrivilegeUnavailable, null, "Not attempted.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _privilege = privilegeService.EnableSeDebugPrivilege();
        logger.LogInformation("PriorityGear Service started. SeDebugPrivilege: {Status}", _privilege.Status);

        ServiceCommandHandler handler = new(priorityApplier, machineRuleStore, () => _privilege);
        StatusPipeServer statusPipeServer = new(handler);
        AdminPipeServer adminPipeServer = new(handler);

        await Task.WhenAll(
            statusPipeServer.RunAsync(stoppingToken),
            adminPipeServer.RunAsync(stoppingToken));
    }
}
