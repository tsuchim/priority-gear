namespace PriorityGear.VerificationSetup;

public static class VerificationPayload
{
    public static readonly string[] RequiredFiles =
    [
        "PriorityGear.Service.exe",
        "PriorityGear.Cli.exe",
        "PriorityGear.App.exe",
        "PriorityGear.TestTarget.exe"
    ];

    public static string PayloadDirectory(string setupDirectory)
    {
        return Path.Combine(setupDirectory, "payload");
    }

    public static IReadOnlyList<string> MissingFiles(string payloadDirectory)
    {
        return RequiredFiles
            .Where(file => !File.Exists(Path.Combine(payloadDirectory, file)))
            .ToList();
    }
}
