using System.Text.RegularExpressions;

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
        if (!Regex.IsMatch(version, @"^v[0-9]+\.[0-9]+\.[0-9]+$") ||
            version.Contains("..", StringComparison.Ordinal) ||
            version.Contains(Path.DirectorySeparatorChar) ||
            version.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException($"Setup version must be a plain semver tag such as v0.3.0: {version}", nameof(version));
        }

        string installDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "PriorityGear");
        string baseFullPath = Path.GetFullPath(installDirectory);
        string versionDirectory = Path.GetFullPath(Path.Combine(baseFullPath, "versions", version));
        string baseWithSeparator = baseFullPath.EndsWith(Path.DirectorySeparatorChar)
            ? baseFullPath
            : baseFullPath + Path.DirectorySeparatorChar;
        if (!versionDirectory.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Version install directory must stay under {baseFullPath}: {versionDirectory}", nameof(version));
        }

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
