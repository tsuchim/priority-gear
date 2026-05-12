using System.IO.Pipes;
using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.VerificationSetup;

public static class PipeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task<ServiceResponse> SendAsync(string pipeName, ServiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(5000, cancellationToken);
            await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
            using StreamReader reader = new(pipe, leaveOpen: true);
            await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            string? responseJson = await reader.ReadLineAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(responseJson)
                ? new ServiceResponse { Succeeded = false, Message = "Empty service response." }
                : JsonSerializer.Deserialize<ServiceResponse>(responseJson, JsonOptions) ?? new ServiceResponse { Succeeded = false, Message = "Invalid service response." };
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or OperationCanceledException)
        {
            return new ServiceResponse { Succeeded = false, Message = ex.Message };
        }
    }
}
