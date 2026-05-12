namespace PriorityGear.VerificationSetup;

public sealed record VerificationInstallPlan(
    string ServiceName,
    string DisplayName,
    string InstallDirectory,
    string ServiceExePath,
    string LogDirectory)
{
    public static VerificationInstallPlan CreateDefault()
    {
        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PriorityGear");
        string programData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PriorityGear");

        return new VerificationInstallPlan(
            "PriorityGear.Service",
            "PriorityGear System Mode Service",
            installDirectory,
            Path.Combine(installDirectory, "PriorityGear.Service.exe"),
            Path.Combine(programData, "Logs"));
    }
}
