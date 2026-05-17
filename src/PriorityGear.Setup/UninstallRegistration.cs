namespace PriorityGear.Setup;

public static class UninstallRegistration
{
    public const string Publisher = "Yuji Tsuchimoto";
    public const string DisplayName = "PriorityGear";

    public static string KeyName(string version) => $"PriorityGear_{version}";

    public static IReadOnlyDictionary<string, string> CreateValues(SetupInstallPlan plan, string setupExePath)
    {
        string quotedSetup = Quote(setupExePath);
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DisplayName"] = DisplayName,
            ["DisplayVersion"] = plan.Version.TrimStart('v', 'V'),
            ["Publisher"] = Publisher,
            ["InstallLocation"] = plan.VersionInstallDirectory,
            ["DisplayIcon"] = setupExePath,
            ["UninstallString"] = $"{quotedSetup} --uninstall",
            ["QuietUninstallString"] = $"{quotedSetup} --uninstall --silent",
            ["NoModify"] = "1",
            ["NoRepair"] = "1"
        };
    }

    private static string Quote(string value) => $"\"{value}\"";
}
