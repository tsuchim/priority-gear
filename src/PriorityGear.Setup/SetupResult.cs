namespace PriorityGear.Setup;

public sealed record SetupResult(bool Succeeded, string Summary)
{
    public static SetupResult InstallSucceeded(string serviceIdentity)
    {
        if (string.IsNullOrWhiteSpace(serviceIdentity))
        {
            return new SetupResult(false, "Install failed: service status did not report a process identity.");
        }

        return new SetupResult(true, $"Install completed. Service identity: {serviceIdentity}");
    }

    public static SetupResult InstallFailed(string reason) => new(false, $"Install failed: {reason}");
}
