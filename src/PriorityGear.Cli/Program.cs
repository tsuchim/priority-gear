using System.IO.Pipes;
using System.Text.Json;
using PriorityGear.Contracts;
using PriorityGear.Core;

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
    default:
        PrintUsage();
        return 2;
}

ServiceResponse response = await SendAsync(pipeName, request);
Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
return response.Succeeded ? 0 : 1;

static async Task<ServiceResponse> SendAsync(string pipeName, ServiceRequest request)
{
    try
    {
        await using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(5000);
        await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        using StreamReader reader = new(pipe, leaveOpen: true);
        await writer.WriteLineAsync(JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        string? responseJson = await reader.ReadLineAsync();
        return string.IsNullOrWhiteSpace(responseJson)
            ? new ServiceResponse { Succeeded = false, Message = "Empty service response." }
            : JsonSerializer.Deserialize<ServiceResponse>(responseJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new ServiceResponse { Succeeded = false, Message = "Invalid service response." };
    }
    catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException)
    {
        return new ServiceResponse { Succeeded = false, Message = ex.Message };
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
