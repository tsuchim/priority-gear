using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public sealed class AdminPipeServer(ServiceCommandHandler handler)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
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

            await pipe.WaitForConnectionAsync(cancellationToken);
            await HandleClientAsync(pipe, cancellationToken);
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        ServiceAuthorizationResult authorization = ServiceAuthorization.EvaluateAdminPipeClient(pipe);
        ServiceRequest? request = await PipeJsonProtocol.ReadRequestAsync(pipe, cancellationToken);
        ServiceResponse response = request is null
            ? new ServiceResponse { Succeeded = false, Message = "Empty request.", Authorization = authorization.ToDto() }
            : handler.HandleAdmin(request, authorization);
        await PipeJsonProtocol.WriteResponseAsync(pipe, response, cancellationToken);
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
