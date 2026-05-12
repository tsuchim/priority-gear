using System.IO.Pipes;
using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public sealed class StatusPipeServer(ServiceCommandHandler handler)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using NamedPipeServerStream pipe = new(
                ServiceContractConstants.StatusPipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await pipe.WaitForConnectionAsync(cancellationToken);
            await HandleClientAsync(pipe, cancellationToken);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        ServiceRequest? request = await PipeJsonProtocol.ReadRequestAsync(pipe, cancellationToken);
        ServiceResponse response = request is null
            ? new ServiceResponse { Succeeded = false, Message = "Empty request." }
            : handler.HandleStatus(request);
        await PipeJsonProtocol.WriteResponseAsync(pipe, response, cancellationToken);
    }
}
