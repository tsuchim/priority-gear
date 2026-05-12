namespace PriorityGear.Core;

public enum ProcessStatus
{
    Ready,
    Matched,
    Applied,
    PathUnavailable,
    CurrentPriorityUnavailable,
    PriorityWriteDenied,
    AdministratorLikelyRequired,
    ServiceModeLikelyRequired,
    ProtectedOrUnsupported,
    ProcessExited,
    UnknownError
}
