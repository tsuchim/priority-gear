using System.Text.Json;
using PriorityGear.Contracts;
using PriorityGear.Core;
using PriorityGear.Service;
using PriorityGear.Windows;

namespace PriorityGear.Service.Tests;

public sealed class ServiceCommandHandlerTests
{
    [Fact]
    public void StatusPipeRejectsMutationCommands()
    {
        ServiceCommandHandler handler = HandlerWithRules([]);

        ServiceResponse response = handler.HandleStatus(new ServiceRequest
        {
            Kind = ServiceCommandKind.TestApplyPriority,
            ProcessId = Environment.ProcessId,
            Priority = ProcessPriorityLevel.Normal
        });

        Assert.False(response.Succeeded);
        Assert.Contains("status pipe", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StatusPipeRejectsProbeCommands()
    {
        ServiceCommandHandler handler = HandlerWithRules([]);

        ServiceResponse response = handler.HandleStatus(new ServiceRequest
        {
            Kind = ServiceCommandKind.ProbePriorityAccess,
            ProcessId = Environment.ProcessId
        });

        Assert.False(response.Succeeded);
        Assert.Contains("status pipe", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdminCommandRejectsUnavailableCallerIdentity()
    {
        ServiceCommandHandler handler = HandlerWithRules([]);
        ServiceAuthorizationResult authorization = new(null, null, false, "Unavailable", false, "Caller identity unavailable.");

        ServiceResponse response = handler.HandleAdmin(new ServiceRequest { Kind = ServiceCommandKind.GetServiceStatus }, authorization);

        Assert.False(response.Succeeded);
        Assert.Equal("Unavailable", response.Authorization!.AuthorizationSource);
    }

    [Fact]
    public void ApplyApprovedMachineRuleRequiresApprovedEnabledRule()
    {
        ServiceCommandHandler handler = HandlerWithRules([
            new MachinePriorityRule
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Enabled = true,
                ApprovedByAdmin = false,
                ExecutableName = "example.exe"
            }
        ]);

        ServiceResponse response = handler.HandleAdmin(
            new ServiceRequest
            {
                Kind = ServiceCommandKind.ApplyApprovedMachineRule,
                RuleId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ProcessId = Environment.ProcessId,
                Priority = ProcessPriorityLevel.Normal
            },
            new ServiceAuthorizationResult("admin", "S-1-5-32-544", true, "PipeAcl", true, string.Empty));

        Assert.False(response.Succeeded);
        Assert.Contains("not approved", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyApprovedMachineRuleRejectsMismatchedExecutable()
    {
        Guid ruleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        ServiceCommandHandler handler = HandlerWithRules([
            new MachinePriorityRule
            {
                Id = ruleId,
                Enabled = true,
                ApprovedByAdmin = true,
                ExecutableName = "definitely-not-this-test-process.exe"
            }
        ]);

        ServiceResponse response = handler.HandleAdmin(
            new ServiceRequest
            {
                Kind = ServiceCommandKind.ApplyApprovedMachineRule,
                RuleId = ruleId,
                ProcessId = Environment.ProcessId,
                Priority = ProcessPriorityLevel.Normal
            },
            new ServiceAuthorizationResult("admin", "S-1-5-32-544", true, "PipeAcl", true, string.Empty));

        Assert.False(response.Succeeded);
        Assert.Contains("executable name", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProbePriorityAccessRequiresAdminAuthorization()
    {
        ServiceCommandHandler handler = HandlerWithRules([]);

        ServiceResponse response = handler.HandleAdmin(
            new ServiceRequest
            {
                Kind = ServiceCommandKind.ProbePriorityAccess,
                ProcessId = Environment.ProcessId
            },
            new ServiceAuthorizationResult("user", "S-1-5-21", false, "Impersonation", false, "Denied"));

        Assert.False(response.Succeeded);
        Assert.Equal("Denied", response.Message);
    }

    [Fact]
    public async Task PipeProtocolReadsSingleBoundedRequestLine()
    {
        await using MemoryStream stream = new();
        await using StreamWriter writer = new(stream, leaveOpen: true);
        await writer.WriteAsync("{\"kind\":0}\n");
        await writer.FlushAsync();
        stream.Position = 0;

        string? line = await PipeJsonProtocol.ReadRequestLineAsync(stream, CancellationToken.None);
        ServiceRequest? request = PipeJsonProtocol.DeserializeRequest(line);

        Assert.Equal("{\"kind\":0}", line);
        Assert.Equal(ServiceCommandKind.GetServiceStatus, request!.Kind);
    }

    [Fact]
    public async Task PipeProtocolRejectsOversizedRequestLine()
    {
        await using MemoryStream stream = new(new byte[PipeJsonProtocol.MaxRequestLineBytes + 1]);

        await Assert.ThrowsAsync<InvalidDataException>(() => PipeJsonProtocol.ReadRequestLineAsync(stream, CancellationToken.None));
    }

    private static ServiceCommandHandler HandlerWithRules(IReadOnlyList<MachinePriorityRule> rules)
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Service.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "rules.machine.json");
        File.WriteAllText(path, JsonSerializer.Serialize(rules, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return new ServiceCommandHandler(
            new Win32PriorityApplier(),
            new MachineRuleStore(path),
            () => new PrivilegeEnableResult(true, false, Win32PriorityStatus.PrivilegeUnavailable, 1300, "Unavailable"));
    }
}
