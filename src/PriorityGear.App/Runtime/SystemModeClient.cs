using System.IO.Pipes;
using System.IO;
using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.App.Runtime;

public sealed class SystemModeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ServiceResponse> GetStatusAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        return await SendAsync(new ServiceRequest { Kind = ServiceCommandKind.GetServiceStatus }, timeout, cancellationToken);
    }

    private static async Task<ServiceResponse> SendAsync(ServiceRequest request, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        try
        {
            await using NamedPipeClientStream pipe = new(".", ServiceContractConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(timeoutSource.Token);
            await using StreamWriter writer = new(pipe, leaveOpen: true) { AutoFlush = true };
            string requestJson = JsonSerializer.Serialize(request, JsonOptions);
            await writer.WriteLineAsync(requestJson.AsMemory(), timeoutSource.Token);
            using StreamReader reader = new(pipe, leaveOpen: true);
            string? responseJson = await reader.ReadLineAsync(timeoutSource.Token);
            ServiceResponse? response = string.IsNullOrWhiteSpace(responseJson)
                ? null
                : JsonSerializer.Deserialize<ServiceResponse>(responseJson, JsonOptions);
            return response ?? new ServiceResponse { Succeeded = false, Message = "Empty service response." };
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException)
        {
            return new ServiceResponse { Succeeded = false, Message = ex.Message };
        }
    }
}
