namespace PriorityGear.Setup;

public static class StartMenuShortcut
{
    public static ShortcutSpec CreateSpec(SetupInstallPlan plan)
    {
        return new ShortcutSpec(
            plan.StartMenuShortcutPath,
            plan.AppExePath,
            plan.VersionInstallDirectory,
            "PriorityGear");
    }

    public static void Create(SetupInstallPlan plan)
    {
        ShortcutSpec spec = CreateSpec(plan);
        Directory.CreateDirectory(Path.GetDirectoryName(spec.ShortcutPath)!);

        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is unavailable; cannot create Start Menu shortcut.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Failed to create WScript.Shell.");
        dynamic shortcut = shell.CreateShortcut(spec.ShortcutPath);
        shortcut.TargetPath = spec.TargetPath;
        shortcut.WorkingDirectory = spec.WorkingDirectory;
        shortcut.Description = spec.Description;
        shortcut.Save();

        if (!File.Exists(spec.ShortcutPath))
        {
            throw new FileNotFoundException($"Start Menu shortcut was not created: {spec.ShortcutPath}", spec.ShortcutPath);
        }
    }

    public static void Delete(SetupInstallPlan plan)
    {
        if (File.Exists(plan.StartMenuShortcutPath))
        {
            File.Delete(plan.StartMenuShortcutPath);
        }

        if (Directory.Exists(plan.StartMenuDirectory) &&
            !Directory.EnumerateFileSystemEntries(plan.StartMenuDirectory).Any())
        {
            Directory.Delete(plan.StartMenuDirectory);
        }
    }
}

public sealed record ShortcutSpec(
    string ShortcutPath,
    string TargetPath,
    string WorkingDirectory,
    string Description);
