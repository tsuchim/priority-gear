using System.IO.Pipes;
using System.Security.Principal;
using System.Text.Json;
using PriorityGear.Contracts;
using PriorityGear.Core;

if (args.Length >= 1 && string.Equals(args[0], "machine-rules", StringComparison.OrdinalIgnoreCase))
{
    return await HandleMachineRulesAsync(args);
}

if (args.Length >= 1 && string.Equals(args[0], "service-processes", StringComparison.OrdinalIgnoreCase))
{
    return await HandleServiceProcessesAsync(args);
}

if (args.Length < 2 || !string.Equals(args[0], "service", StringComparison.OrdinalIgnoreCase))
{
    PrintUsage();
    return 2;
}

ServiceRequest request;
string pipeName;

switch (args[1].ToLowerInvariant())
{
    case "status":
        request = new ServiceRequest { Kind = ServiceCommandKind.GetServiceStatus };
        pipeName = ServiceContractConstants.StatusPipeName;
        break;
    case "test-apply":
        request = new ServiceRequest
        {
            Kind = ServiceCommandKind.TestApplyPriority,
            ProcessId = ReadIntOption(args, "--pid"),
            Priority = ReadPriorityOption(args, "--priority")
        };
        pipeName = ServiceContractConstants.AdminPipeName;
        break;
    case "apply-rule":
        request = new ServiceRequest
        {
            Kind = ServiceCommandKind.ApplyApprovedMachineRule,
            ProcessId = ReadIntOption(args, "--pid"),
            RuleId = ReadGuidOption(args, "--rule-id"),
            Priority = TryReadPriorityOption(args, "--priority")
        };
        pipeName = ServiceContractConstants.AdminPipeName;
        break;
    case "probe":
        request = new ServiceRequest
        {
            Kind = ServiceCommandKind.ProbePriorityAccess,
            ProcessId = ReadIntOption(args, "--pid")
        };
        pipeName = ServiceContractConstants.AdminPipeName;
        break;
    default:
        PrintUsage();
        return 2;
}

ServiceResponse response = await SendAsync(pipeName, request);
Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
return response.Succeeded ? 0 : 1;

static async Task<ServiceResponse> SendAsync(string pipeName, ServiceRequest request)
{
    JsonSerializerOptions wireOptions = new(JsonSerializerDefaults.Web);
    try
    {
        TokenImpersonationLevel impersonationLevel = string.Equals(pipeName, ServiceContractConstants.AdminPipeName, StringComparison.Ordinal)
            ? TokenImpersonationLevel.Impersonation
            : TokenImpersonationLevel.Identification;
        await using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, impersonationLevel);
        await pipe.ConnectAsync(5000);
        await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        using StreamReader reader = new(pipe, leaveOpen: true);
        await writer.WriteLineAsync(JsonSerializer.Serialize(request, wireOptions));
        await writer.FlushAsync();
        string? responseJson = await reader.ReadLineAsync();
        return string.IsNullOrWhiteSpace(responseJson)
            ? new ServiceResponse { Succeeded = false, Message = "Pipe connected but EOF was received before a response line." }
            : JsonSerializer.Deserialize<ServiceResponse>(responseJson, wireOptions) ?? new ServiceResponse { Succeeded = false, Message = "Invalid service response." };
    }
    catch (JsonException ex)
    {
        return new ServiceResponse { Succeeded = false, Message = $"Invalid service response JSON: {ex.Message}" };
    }
    catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
    {
        return new ServiceResponse { Succeeded = false, Message = $"Pipe request failed: {ex.GetType().Name}: {ex.Message}" };
    }
}

static int ReadIntOption(string[] args, string name)
{
    string value = ReadStringOption(args, name);
    return int.TryParse(value, out int result) ? result : throw new ArgumentException($"{name} must be an integer.");
}

static Guid ReadGuidOption(string[] args, string name)
{
    string value = ReadStringOption(args, name);
    return Guid.TryParse(value, out Guid result) ? result : throw new ArgumentException($"{name} must be a GUID.");
}

static ProcessPriorityLevel ReadPriorityOption(string[] args, string name)
{
    string value = ReadStringOption(args, name);
    return Enum.TryParse(value, ignoreCase: true, out ProcessPriorityLevel priority)
        ? priority
        : throw new ArgumentException($"{name} is not a supported priority.");
}

static string ReadStringOption(string[] args, string name)
{
    for (int index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }

    throw new ArgumentException($"Missing required option {name}.");
}

static void PrintUsage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  PriorityGear.Cli service status");
    Console.Error.WriteLine("  PriorityGear.Cli service test-apply --pid <pid> --priority BelowNormal");
    Console.Error.WriteLine("  PriorityGear.Cli service apply-rule --rule-id <guid> --pid <pid>");
    Console.Error.WriteLine("  PriorityGear.Cli service apply-rule --rule-id <guid> --pid <pid> --priority BelowNormal");
    Console.Error.WriteLine("  PriorityGear.Cli service probe --pid <pid>");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules list");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules add --name <name> --exe <exeName> --priority BelowNormal --approve");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules add-service --name <name> --service-name <serviceName> --priority BelowNormal --approve [--dry-run] [--allow-shared-service-host]");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules update --id <id> --name <name> --exe <exeName> --priority BelowNormal --approve");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules enable|disable|delete --id <id>");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules approve|unapprove --id <id>");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules reload");
    Console.Error.WriteLine("  PriorityGear.Cli machine-rules scan-now");
    Console.Error.WriteLine("  PriorityGear.Cli service-processes list");
    Console.Error.WriteLine("  PriorityGear.Cli service-processes show --service-name <name>");
    Console.Error.WriteLine("  PriorityGear.Cli service-processes show-pid --pid <pid>");
    Console.Error.WriteLine("  PriorityGear.Cli service-processes probe --service-name <name>");
}

static ProcessPriorityLevel? TryReadPriorityOption(string[] args, string name)
{
    for (int index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
        {
            return Enum.TryParse(args[index + 1], ignoreCase: true, out ProcessPriorityLevel priority)
                ? priority
                : throw new ArgumentException($"{name} is not a supported priority.");
        }
    }

    return null;
}

static async Task<int> HandleMachineRulesAsync(string[] args)
{
    if (args.Length < 2)
    {
        PrintUsage();
        return 2;
    }

    ServiceRequest request = args[1].ToLowerInvariant() switch
    {
        "list" => new ServiceRequest { Kind = ServiceCommandKind.GetMachineRules },
        "add" => new ServiceRequest
        {
            Kind = ServiceCommandKind.AddMachineRule,
            MachineRule = new MachinePriorityRule
            {
                DisplayName = ReadStringOption(args, "--name"),
                ExecutableName = ReadStringOption(args, "--exe"),
                BasePriority = ReadPriorityOption(args, "--priority"),
                Enabled = true,
                ApprovedByAdmin = args.Any(static arg => string.Equals(arg, "--approve", StringComparison.OrdinalIgnoreCase))
            }
        },
        "add-service" => new ServiceRequest
        {
            Kind = ServiceCommandKind.AddMachineRule,
            MachineRule = new MachinePriorityRule
            {
                DisplayName = ReadStringOption(args, "--name"),
                ServiceName = ReadStringOption(args, "--service-name"),
                BasePriority = ReadPriorityOption(args, "--priority"),
                Enabled = true,
                ApprovedByAdmin = args.Any(static arg => string.Equals(arg, "--approve", StringComparison.OrdinalIgnoreCase)),
                DryRunOnly = args.Any(static arg => string.Equals(arg, "--dry-run", StringComparison.OrdinalIgnoreCase)),
                AllowSharedServiceHost = args.Any(static arg => string.Equals(arg, "--allow-shared-service-host", StringComparison.OrdinalIgnoreCase))
            }
        },
        "update" => new ServiceRequest
        {
            Kind = ServiceCommandKind.UpdateMachineRule,
            MachineRule = new MachinePriorityRule
            {
                Id = ReadGuidOption(args, "--id"),
                DisplayName = ReadStringOption(args, "--name"),
                ExecutableName = ReadStringOption(args, "--exe"),
                BasePriority = ReadPriorityOption(args, "--priority"),
                Enabled = !args.Any(static arg => string.Equals(arg, "--disabled", StringComparison.OrdinalIgnoreCase)),
                ApprovedByAdmin = args.Any(static arg => string.Equals(arg, "--approve", StringComparison.OrdinalIgnoreCase))
            }
        },
        "enable" => new ServiceRequest { Kind = ServiceCommandKind.EnableMachineRule, RuleId = ReadGuidOption(args, "--id") },
        "disable" => new ServiceRequest { Kind = ServiceCommandKind.DisableMachineRule, RuleId = ReadGuidOption(args, "--id") },
        "approve" => new ServiceRequest { Kind = ServiceCommandKind.ApproveMachineRule, RuleId = ReadGuidOption(args, "--id") },
        "unapprove" => new ServiceRequest { Kind = ServiceCommandKind.UnapproveMachineRule, RuleId = ReadGuidOption(args, "--id") },
        "delete" => new ServiceRequest { Kind = ServiceCommandKind.DeleteMachineRule, RuleId = ReadGuidOption(args, "--id") },
        "reload" => new ServiceRequest { Kind = ServiceCommandKind.ReloadMachineRules },
        "scan-now" => new ServiceRequest { Kind = ServiceCommandKind.ScanMachineRulesNow },
        _ => throw new ArgumentException("Unsupported machine-rules command.")
    };

    string pipeName = request.Kind == ServiceCommandKind.GetMachineRules
        ? ServiceContractConstants.StatusPipeName
        : ServiceContractConstants.AdminPipeName;
    ServiceResponse response = await SendAsync(pipeName, request);
    Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
    return response.Succeeded ? 0 : 1;
}

static async Task<int> HandleServiceProcessesAsync(string[] args)
{
    if (args.Length < 2)
    {
        PrintUsage();
        return 2;
    }

    ServiceResponse response = await SendAsync(ServiceContractConstants.StatusPipeName, new ServiceRequest { Kind = ServiceCommandKind.DiscoverServiceProcesses });
    if (!response.Succeeded || response.ServiceProcesses is null)
    {
        Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        return 1;
    }

    IEnumerable<ServiceProcessInfoDto> processes = response.ServiceProcesses;
    switch (args[1].ToLowerInvariant())
    {
        case "show":
        case "probe":
            string serviceName = ReadStringOption(args, "--service-name");
            processes = processes.Where(process => process.ServiceNames.Any(name => string.Equals(name, serviceName, StringComparison.OrdinalIgnoreCase)));
            break;
        case "show-pid":
            int pid = ReadIntOption(args, "--pid");
            processes = processes.Where(process => process.ProcessId == pid);
            break;
        case "list":
            break;
        default:
            PrintUsage();
            return 2;
    }

    response.ServiceProcesses = processes.ToList();
    Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
    return 0;
}
