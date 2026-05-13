using System.IO.Pipes;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public sealed class StatusPipeServer(ServiceCommandHandler handler, ServiceFileLog log)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        log.Info("Status pipe server started.");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using NamedPipeServerStream pipe = new(
                    ServiceContractConstants.StatusPipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                log.Info("Status pipe waiting.");
                await pipe.WaitForConnectionAsync(cancellationToken);
                log.Info("Status pipe connected.");
                await HandleClientAsync(pipe, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                log.Info("Status pipe server stopping.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Status pipe server loop exception.");
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        try
        {
            ServiceRequest? request = await PipeJsonProtocol.ReadRequestAsync(pipe, cancellationToken);
            log.Info(request is null ? "Status pipe received empty request." : $"Status pipe command: {request.Kind}");
            ServiceResponse response = request is null
                ? new ServiceResponse { Succeeded = false, Message = "Empty request." }
                : handler.HandleStatus(request);
            log.Info($"Status pipe response before serialization: Succeeded={response.Succeeded}; Message={response.Message}");
            await PipeJsonProtocol.WriteResponseAsync(pipe, response, cancellationToken);
            log.Info("Status pipe response write succeeded.");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Status pipe client exception.");
            await TryWriteFailureAsync(pipe, $"Status pipe server exception: {ex.GetType().Name}: {ex.Message}", cancellationToken);
        }
    }

    private async Task TryWriteFailureAsync(NamedPipeServerStream pipe, string message, CancellationToken cancellationToken)
    {
        try
        {
            if (pipe.IsConnected)
            {
                await PipeJsonProtocol.WriteResponseAsync(pipe, new ServiceResponse { Succeeded = false, Message = message }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Status pipe failed to write structured exception response.");
        }
    }
}
