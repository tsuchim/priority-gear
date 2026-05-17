namespace PriorityGear.Setup;

public sealed record SetupInstallPlan(
    string ServiceName,
    string DisplayName,
    string BaseInstallDirectory,
    string Version,
    string VersionInstallDirectory,
    string ServiceExePath,
    string ProgramDataDirectory,
    string MachineRulesPath,
    string LogDirectory)
{
    public static SetupInstallPlan Create(string version)
    {
        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PriorityGear");
        string versionDirectory = Path.Combine(installDirectory, "versions", version);
        string programData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PriorityGear");

        return new SetupInstallPlan(
            "PriorityGear.Service",
            "PriorityGear System Mode Service",
            installDirectory,
            version,
            versionDirectory,
            Path.Combine(versionDirectory, "PriorityGear.Service.exe"),
            programData,
            Path.Combine(programData, "rules.machine.json"),
            Path.Combine(programData, "Logs"));
    }
}
