using System.IO.Pipes;
using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public static class PipeJsonProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ServiceRequest?> ReadRequestAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(pipe, leaveOpen: true);
        string? line = await reader.ReadLineAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(line)
            ? null
            : JsonSerializer.Deserialize<ServiceRequest>(line, JsonOptions);
    }

    public static async Task WriteResponseAsync(NamedPipeServerStream pipe, ServiceResponse response, CancellationToken cancellationToken)
    {
        await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        string responseJson = JsonSerializer.Serialize(response, JsonOptions);
        await writer.WriteLineAsync(responseJson.AsMemory(), cancellationToken);
    }
}
