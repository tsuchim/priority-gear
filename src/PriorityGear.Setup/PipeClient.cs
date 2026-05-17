using System.IO.Pipes;
using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.Setup;

internal static class PipeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ServiceResponse> SendStatusAsync(CancellationToken cancellationToken)
    {
        using NamedPipeClientStream pipe = new(
            ".",
            ServiceContractConstants.StatusPipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        await pipe.ConnectAsync(2000, cancellationToken);
        using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
        using StreamReader reader = new(pipe, leaveOpen: true);
        string request = JsonSerializer.Serialize(
            new ServiceRequest { Kind = ServiceCommandKind.GetServiceStatus },
            JsonOptions);
        await writer.WriteLineAsync(request);
        string? responseLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseLine))
        {
            return new ServiceResponse { Succeeded = false, Message = "Status pipe returned no response." };
        }

        return JsonSerializer.Deserialize<ServiceResponse>(responseLine, JsonOptions)
            ?? new ServiceResponse { Succeeded = false, Message = "Status pipe returned an empty JSON response." };
    }
}
