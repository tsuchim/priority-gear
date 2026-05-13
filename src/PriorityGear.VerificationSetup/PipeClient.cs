using System.IO.Pipes;
using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.VerificationSetup;

public static class PipeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ServiceResponse> SendAsync(string pipeName, ServiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(5000, cancellationToken);
            await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
            using StreamReader reader = new(pipe, leaveOpen: true);
            await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            await writer.FlushAsync(cancellationToken);
            string? responseJson = await reader.ReadLineAsync(cancellationToken);
            return ClassifyResponseLine(responseJson);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or UnauthorizedAccessException or OperationCanceledException)
        {
            return new ServiceResponse { Succeeded = false, Message = $"Pipe request failed: {ex.GetType().Name}: {ex.Message}" };
        }
    }

    public static ServiceResponse ClassifyResponseLine(string? responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
        {
            return new ServiceResponse { Succeeded = false, Message = "Pipe connected but EOF was received before a response line." };
        }

        try
        {
            return JsonSerializer.Deserialize<ServiceResponse>(responseJson, JsonOptions)
                ?? new ServiceResponse { Succeeded = false, Message = "Invalid service response: JSON deserialized to null." };
        }
        catch (JsonException ex)
        {
            return new ServiceResponse { Succeeded = false, Message = $"Invalid service response JSON: {ex.Message}" };
        }
    }
}
