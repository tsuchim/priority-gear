namespace PriorityGear.Setup;

public static class SetupStartup
{
    public static IReadOnlyList<string> InitialLines(SetupCommandLine command, string logPath)
    {
        return
        [
            "PriorityGear Setup started.",
            $"Log: {logPath}",
            $"Mode: {ModeName(command)}"
        ];
    }

    public static string ModeName(SetupCommandLine command)
    {
        return command.Action == SetupCommandAction.Uninstall ? "uninstall" : "install/update";
    }
}
