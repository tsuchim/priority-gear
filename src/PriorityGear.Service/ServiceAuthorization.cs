using System.IO.Pipes;
using System.Security.Principal;

namespace PriorityGear.Service;

public static class ServiceAuthorization
{
    public static bool IsMutatingCommandAllowed(NamedPipeServerStream pipe)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pipe.GetImpersonationUserName()))
            {
                return false;
            }

            using WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(current);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
