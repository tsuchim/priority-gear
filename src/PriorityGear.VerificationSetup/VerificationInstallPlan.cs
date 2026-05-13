namespace PriorityGear.VerificationSetup;

public sealed record VerificationInstallPlan(
    string ServiceName,
    string DisplayName,
    string BaseInstallDirectory,
    string Version,
    string VersionInstallDirectory,
    string ServiceExePath,
    string LogDirectory)
{
    public static VerificationInstallPlan CreateDefault()
    {
        return Create(DateTime.Now.ToString("yyyyMMdd-HHmmss"));
    }

    public static VerificationInstallPlan Create(string version)
    {
        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PriorityGear");
        string versionDirectory = Path.Combine(installDirectory, "versions", version);
        string programData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PriorityGear");

        return new VerificationInstallPlan(
            "PriorityGear.Service",
            "PriorityGear System Mode Service",
            installDirectory,
            version,
            versionDirectory,
            Path.Combine(versionDirectory, "PriorityGear.Service.exe"),
            Path.Combine(programData, "Logs"));
    }
}
