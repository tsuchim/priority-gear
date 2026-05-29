using System.Diagnostics;
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
    public void ApplyApprovedMachineRuleRejectsDryRunOnlyRule()
    {
        Guid ruleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        ServiceCommandHandler handler = HandlerWithRules([
            new MachinePriorityRule
            {
                Id = ruleId,
                Enabled = true,
                ApprovedByAdmin = true,
                DryRunOnly = true,
                ExecutableName = "testhost.exe"
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
        Assert.Contains("dry-run", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyApprovedMachineRuleRejectsExecutableOnlySvchostRule()
    {
        Guid ruleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        ServiceCommandHandler handler = HandlerWithRules([
            new MachinePriorityRule
            {
                Id = ruleId,
                Enabled = true,
                ApprovedByAdmin = true,
                ExecutableName = "svchost.exe"
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
        Assert.Contains("svchost.exe", response.Message, StringComparison.OrdinalIgnoreCase);
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
    public void MachineRuleStoreMalformedJsonReportsFailure()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Service.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "rules.machine.json");
        File.WriteAllText(path, "{ invalid json");
        MachineRuleStore store = new(path);

        MachineRuleStoreResult result = store.TryLoad();

        Assert.False(result.Succeeded);
        Assert.Equal("{ invalid json", File.ReadAllText(path));
    }

    [Fact]
    public void AddMachineRuleDoesNotOverwriteMalformedJson()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Service.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "rules.machine.json");
        File.WriteAllText(path, "{ invalid json");
        MachineRuleStore store = new(path);
        ServiceFileLog log = new();
        ServiceCommandHandler handler = new(
            new Win32PriorityApplier(),
            store,
            new MachineRuleMonitor(store, new Win32PriorityApplier(), new ServiceProcessDiscovery(new Win32PriorityApplier()), log),
            new ServiceProcessDiscovery(new Win32PriorityApplier()),
            () => new PrivilegeEnableResult(true, true, Win32PriorityStatus.Success, null, "OK"));

        ServiceResponse response = handler.HandleAdmin(
            new ServiceRequest
            {
                Kind = ServiceCommandKind.AddMachineRule,
                MachineRule = new MachinePriorityRule { DisplayName = "test", ExecutableName = "test.exe", Enabled = true, ApprovedByAdmin = true }
            },
            new ServiceAuthorizationResult("admin", "S-1-5-32-544", true, "PipeAcl", true, string.Empty));

        Assert.False(response.Succeeded);
        Assert.Contains("not overwritten", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("{ invalid json", File.ReadAllText(path));
    }

    [Fact]
    public void MachineRuleMatcherIgnoresDisabledAndUnapprovedRules()
    {
        Assert.False(MachineRuleMatcher.IsRuntimeEligible(new MachinePriorityRule { Enabled = false, ApprovedByAdmin = true }));
        Assert.False(MachineRuleMatcher.IsRuntimeEligible(new MachinePriorityRule { Enabled = true, ApprovedByAdmin = false }));
        Assert.True(MachineRuleMatcher.IsRuntimeEligible(new MachinePriorityRule { Enabled = true, ApprovedByAdmin = true }));
    }

    [Fact]
    public void MachineRuleMatcherRejectsSvchostExecutableOnlyRulesByDefault()
    {
        Assert.False(MachineRuleMatcher.IsRuntimeEligible(new MachinePriorityRule
        {
            Enabled = true,
            ApprovedByAdmin = true,
            ExecutableName = "svchost.exe"
        }));

        Assert.True(MachineRuleMatcher.IsRuntimeEligible(new MachinePriorityRule
        {
            Enabled = true,
            ApprovedByAdmin = true,
            ExecutableName = "svchost.exe",
            ServiceName = "example-service",
            AllowSharedServiceHost = true
        }));
    }

    [Fact]
    public void ServiceDiscoveryRequestCarriesDirectServiceName()
    {
        ServiceRequest request = new()
        {
            Kind = ServiceCommandKind.DiscoverServiceProcesses,
            ServiceName = "PriorityGear.TestTarget.Service"
        };

        string json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        ServiceRequest? roundTrip = JsonSerializer.Deserialize<ServiceRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(ServiceCommandKind.DiscoverServiceProcesses, roundTrip!.Kind);
        Assert.Equal("PriorityGear.TestTarget.Service", roundTrip.ServiceName);
    }

    [Fact]
    public void ServiceDiscoveryStatusReportsTruncationMetadata()
    {
        ServiceProcessDiscoveryStatusDto status = new()
        {
            TotalDiscoveredGroupCount = 172,
            ReturnedGroupCount = 100,
            Truncated = true,
            Limit = 100
        };

        Assert.Equal(172, status.TotalDiscoveredGroupCount);
        Assert.Equal(100, status.ReturnedGroupCount);
        Assert.True(status.Truncated);
        Assert.Equal(100, status.Limit);
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

    [Fact]
    public async Task MachineRuleMonitor_ReappliesWhenOnlyCoreReserveChanges()
    {
        string processName = Process.GetCurrentProcess().ProcessName + ".exe";
        MachinePriorityRule rule = RuntimeRule(processName);
        MachineRuleStore store = StoreWithRules([rule]);
        List<(int ProcessId, ProcessPriorityLevel Priority)> priorityCalls = [];
        List<(int ProcessId, int Reserve)> affinityCalls = [];
        MachineRuleMonitor monitor = MonitorWithFakes(
            store,
            (pid, priority) =>
            {
                priorityCalls.Add((pid, priority));
                return new Win32PriorityResult(true, Win32PriorityStatus.Success, priority, null, "OK");
            },
            (pid, reserve) =>
            {
                affinityCalls.Add((pid, reserve));
                return CoreReserveApplyResult.Success(reserve, "OK");
            });

        await monitor.ScanAsync(CancellationToken.None);
        rule.CoreReserve = 2;
        store.Save([rule]);
        await monitor.ScanAsync(CancellationToken.None);

        Assert.True(priorityCalls.Count >= 2);
        Assert.Contains(affinityCalls, call => call.Reserve == 2);
        Assert.DoesNotContain(monitor.GetStatus().Processes, process => process.LastResult == "AlreadyApplied");
    }

    [Fact]
    public async Task MachineRuleMonitor_FailedCoreReserveIsNotRecordedAsSuccessfulApplication()
    {
        string processName = Process.GetCurrentProcess().ProcessName + ".exe";
        MachinePriorityRule rule = RuntimeRule(processName);
        rule.CoreReserve = 1;
        MachineRuleStore store = StoreWithRules([rule]);
        int priorityCalls = 0;
        MachineRuleMonitor monitor = MonitorWithFakes(
            store,
            (_, priority) =>
            {
                priorityCalls++;
                return new Win32PriorityResult(true, Win32PriorityStatus.Success, priority, null, "OK");
            },
            (_, reserve) => CoreReserveApplyResult.Failure(reserve, "topology unavailable", "CoreTopologyUnsupported"));

        await monitor.ScanAsync(CancellationToken.None);
        await monitor.ScanAsync(CancellationToken.None);

        Assert.True(priorityCalls >= 2);
        Assert.Contains(monitor.GetStatus().Processes, process => process.LastResult.Contains("CoreReserveFailed", StringComparison.Ordinal));
    }

    private static ServiceCommandHandler HandlerWithRules(IReadOnlyList<MachinePriorityRule> rules)
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Service.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "rules.machine.json");
        File.WriteAllText(path, JsonSerializer.Serialize(rules, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        MachineRuleStore store = new(path);
        ServiceFileLog log = new();
        return new ServiceCommandHandler(
            new Win32PriorityApplier(),
            store,
            new MachineRuleMonitor(store, new Win32PriorityApplier(), new ServiceProcessDiscovery(new Win32PriorityApplier()), log),
            new ServiceProcessDiscovery(new Win32PriorityApplier()),
            () => new PrivilegeEnableResult(true, false, Win32PriorityStatus.PrivilegeUnavailable, 1300, "Unavailable"));
    }

    private static MachinePriorityRule RuntimeRule(string processName)
    {
        return new MachinePriorityRule
        {
            Id = Guid.NewGuid(),
            DisplayName = processName,
            ExecutableName = processName,
            BasePriority = ProcessPriorityLevel.Normal,
            Enabled = true,
            ApprovedByAdmin = true
        };
    }

    private static MachineRuleStore StoreWithRules(IReadOnlyList<MachinePriorityRule> rules)
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Service.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "rules.machine.json");
        MachineRuleStore store = new(path);
        store.Save(rules);
        return store;
    }

    private static MachineRuleMonitor MonitorWithFakes(
        MachineRuleStore store,
        Func<int, ProcessPriorityLevel, Win32PriorityResult> setPriority,
        Func<int, int, CoreReserveApplyResult> applyCoreReserve)
    {
        return new MachineRuleMonitor(
            store,
            setPriority,
            applyCoreReserve,
            new ServiceProcessDiscovery(new Win32PriorityApplier()),
            TempLog());
    }

    private static ServiceFileLog TempLog()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Service.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return new ServiceFileLog(Path.Combine(directory, "service-current.log"));
    }
}
