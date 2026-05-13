using System.IO.Pipes;
using System.Security.Principal;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public sealed record ServiceAuthorizationResult(
    string? CallerName,
    string? CallerSid,
    bool IsAdministrator,
    string AuthorizationSource,
    bool CommandAllowed,
    string DenialReason)
{
    public ServiceAuthorizationDto ToDto()
    {
        return new ServiceAuthorizationDto
        {
            CallerName = CallerName,
            CallerSid = CallerSid,
            IsAdministrator = IsAdministrator,
            AuthorizationSource = AuthorizationSource,
            CommandAllowed = CommandAllowed,
            DenialReason = DenialReason
        };
    }
}

public static class ServiceAuthorization
{
    public static ServiceAuthorizationResult AllowStatus()
    {
        return new ServiceAuthorizationResult(null, null, false, "StatusPipe", true, string.Empty);
    }

    public static ServiceAuthorizationResult EvaluateAdminPipeClient(NamedPipeServerStream pipe)
    {
        string? callerName = null;
        string? callerSid = null;
        bool isAdministrator = false;
        string source = "PipeAcl";

        try
        {
            callerName = pipe.GetImpersonationUserName();
            pipe.RunAsClient(() =>
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                callerName = identity.Name;
                callerSid = identity.User?.Value;
                WindowsPrincipal principal = new(identity);
                isAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new ServiceAuthorizationResult(callerName, callerSid, false, "Unavailable", false, $"Caller identity unavailable: {ex.Message}");
        }

        return isAdministrator
            ? new ServiceAuthorizationResult(callerName, callerSid, true, source, true, string.Empty)
            : new ServiceAuthorizationResult(callerName, callerSid, false, source, false, "Mutating service commands require an administrator caller.");
    }
}
