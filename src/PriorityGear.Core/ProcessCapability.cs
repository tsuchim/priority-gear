namespace PriorityGear.Core;

public enum ProcessCapability
{
    ControllableNow,
    CurrentUserOnly,
    AdministratorRequired,
    ServiceModeRequired,
    ProtectedOrUnsupported,
    UnknownError
}
