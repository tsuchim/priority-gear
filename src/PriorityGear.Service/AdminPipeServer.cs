using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public sealed class AdminPipeServer(ServiceCommandHandler handler, ServiceFileLog log)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        log.Info("Admin pipe server started.");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                PipeSecurity security = CreateAdminPipeSecurity();
                using NamedPipeServerStream pipe = NamedPipeServerStreamAcl.Create(
                    ServiceContractConstants.AdminPipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous,
                    0,
                    0,
                    security);

                log.Info("Admin pipe waiting.");
                await pipe.WaitForConnectionAsync(cancellationToken);
                log.Info("Admin pipe connected.");
                await HandleClientAsync(pipe, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                log.Info("Admin pipe server stopping.");
            }
            catch (Exception ex)
            {
                log.Error(ex, "Admin pipe server loop exception.");
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        ServiceAuthorizationResult authorization = ServiceAuthorization.EvaluateAdminPipeClient(pipe);
        try
        {
            log.Info($"Admin pipe authorization: Allowed={authorization.CommandAllowed}; Caller={authorization.CallerName}; Source={authorization.AuthorizationSource}; Denial={authorization.DenialReason}");
            ServiceRequest? request = await PipeJsonProtocol.ReadRequestAsync(pipe, cancellationToken);
            log.Info(request is null ? "Admin pipe received empty request." : $"Admin pipe command: {request.Kind}");
            ServiceResponse response = request is null
                ? new ServiceResponse { Succeeded = false, Message = "Empty request.", Authorization = authorization.ToDto() }
                : handler.HandleAdmin(request, authorization);
            log.Info($"Admin pipe response before serialization: Succeeded={response.Succeeded}; Message={response.Message}");
            await PipeJsonProtocol.WriteResponseAsync(pipe, response, cancellationToken);
            log.Info("Admin pipe response write succeeded.");
        }
        catch (Exception ex)
        {
            log.Error(ex, "Admin pipe client exception.");
            await TryWriteFailureAsync(pipe, authorization, $"Admin pipe server exception: {ex.GetType().Name}: {ex.Message}", cancellationToken);
        }
    }

    private async Task TryWriteFailureAsync(NamedPipeServerStream pipe, ServiceAuthorizationResult authorization, string message, CancellationToken cancellationToken)
    {
        try
        {
            if (pipe.IsConnected)
            {
                await PipeJsonProtocol.WriteResponseAsync(pipe, new ServiceResponse { Succeeded = false, Message = message, Authorization = authorization.ToDto() }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, "Admin pipe failed to write structured exception response.");
        }
    }

    private static PipeSecurity CreateAdminPipeSecurity()
    {
        PipeSecurity security = new();
        SecurityIdentifier administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null);
        SecurityIdentifier localSystem = new(WellKnownSidType.LocalSystemSid, null);

        security.AddAccessRule(new PipeAccessRule(administrators, PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(localSystem, PipeAccessRights.FullControl, AccessControlType.Allow));
        return security;
    }
}
