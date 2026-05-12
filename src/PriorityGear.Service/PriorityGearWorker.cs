using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PriorityGear.Contracts;
using PriorityGear.Windows;

namespace PriorityGear.Service;

public sealed class PriorityGearWorker(
    ILogger<PriorityGearWorker> logger,
    PrivilegeService privilegeService,
    Win32PriorityApplier priorityApplier,
    MachineRuleStore machineRuleStore) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private PrivilegeEnableResult _privilege = new(false, false, Win32PriorityStatus.PrivilegeUnavailable, null, "Not attempted.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _privilege = privilegeService.EnableSeDebugPrivilege();
        logger.LogInformation("PriorityGear Service started. SeDebugPrivilege: {Status}", _privilege.Status);

        while (!stoppingToken.IsCancellationRequested)
        {
            using NamedPipeServerStream pipe = new(
                ServiceContractConstants.PipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(stoppingToken);
            await HandleClientAsync(pipe, stoppingToken);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        ServiceResponse response;
        try
        {
            using StreamReader reader = new(pipe, leaveOpen: true);
            string? line = await reader.ReadLineAsync(cancellationToken);
            ServiceRequest? request = string.IsNullOrWhiteSpace(line)
                ? null
                : JsonSerializer.Deserialize<ServiceRequest>(line, JsonOptions);
            response = request is null
                ? new ServiceResponse { Succeeded = false, Message = "Empty request." }
                : HandleRequest(request, pipe);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            response = new ServiceResponse { Succeeded = false, Message = ex.Message };
        }

        await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        string responseJson = JsonSerializer.Serialize(response, JsonOptions);
        await writer.WriteLineAsync(responseJson.AsMemory(), cancellationToken);
    }

    private ServiceResponse HandleRequest(ServiceRequest request, NamedPipeServerStream pipe)
    {
        return request.Kind switch
        {
            ServiceCommandKind.GetServiceStatus => StatusResponse(),
            ServiceCommandKind.GetMachineRules => RulesResponse(),
            ServiceCommandKind.TestApplyPriority => ApplyPriorityResponse(request, pipe, false),
            ServiceCommandKind.ApplyApprovedMachineRule => ApplyPriorityResponse(request, pipe, true),
            _ => new ServiceResponse { Succeeded = false, Message = "Unsupported command." }
        };
    }

    private ServiceResponse StatusResponse()
    {
        return new ServiceResponse
        {
            Succeeded = true,
            Message = "Service running.",
            Status = new ServiceStatusDto
            {
                ServiceRunning = true,
                ServiceAccount = Environment.UserName,
                SeDebugPrivilege = new PrivilegeStatusDto
                {
                    Attempted = _privilege.Attempted,
                    Succeeded = _privilege.Succeeded,
                    Status = _privilege.Status.ToString(),
                    Win32Error = _privilege.Win32Error
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
            MachineRules = machineRuleStore.Load().ToList()
        };
    }

    private ServiceResponse ApplyPriorityResponse(ServiceRequest request, NamedPipeServerStream pipe, bool requireApprovedRule)
    {
        if (!ServiceAuthorization.IsMutatingCommandAllowed(pipe))
        {
            return new ServiceResponse { Succeeded = false, Message = "Mutating service commands require an administrator caller." };
        }

        if (request.ProcessId is null || request.Priority is null)
        {
            return new ServiceResponse { Succeeded = false, Message = "ProcessId and Priority are required." };
        }

        if (requireApprovedRule && !HasApprovedRule(request.RuleId))
        {
            return new ServiceResponse { Succeeded = false, Message = "Approved machine rule is required." };
        }

        Win32PriorityResult result = priorityApplier.SetPriority(request.ProcessId.Value, request.Priority.Value);
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

    private bool HasApprovedRule(Guid? ruleId)
    {
        return ruleId is not null && machineRuleStore.Load().Any(rule => rule.Id == ruleId && rule.Enabled && rule.ApprovedByAdmin);
    }
}
