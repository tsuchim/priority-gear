namespace PriorityGear.Setup;

public enum SetupCommandAction
{
    Install,
    Uninstall,
    Help,
    Version
}

public sealed record SetupCommandLine(SetupCommandAction Action, bool Silent, bool Verify);

public sealed record SetupCommandLineParseResult(bool Succeeded, SetupCommandLine? Command, string Message)
{
    public static SetupCommandLineParseResult Success(SetupCommandLine command) => new(true, command, string.Empty);

    public static SetupCommandLineParseResult Failure(string message) => new(false, null, message);
}

public static class SetupCommandLineParser
{
    public static SetupCommandLineParseResult Parse(IEnumerable<string> args)
    {
        SetupCommandAction? action = null;
        bool silent = false;
        bool verify = false;

        foreach (string arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--install":
                    if (action is not null) return SetupCommandLineParseResult.Failure("Only one action may be specified.");
                    action = SetupCommandAction.Install;
                    break;
                case "--uninstall":
                    if (action is not null) return SetupCommandLineParseResult.Failure("Only one action may be specified.");
                    action = SetupCommandAction.Uninstall;
                    break;
                case "--help":
                case "-h":
                case "-?":
                case "/?":
                    if (action is not null) return SetupCommandLineParseResult.Failure("Only one action may be specified.");
                    action = SetupCommandAction.Help;
                    break;
                case "--version":
                    if (action is not null) return SetupCommandLineParseResult.Failure("Only one action may be specified.");
                    action = SetupCommandAction.Version;
                    break;
                case "--silent":
                case "--quiet":
                    silent = true;
                    break;
                case "--verify":
                    verify = true;
                    break;
                default:
                    return SetupCommandLineParseResult.Failure($"Unknown setup option: {arg}");
            }
        }

        SetupCommandAction selectedAction = action ?? SetupCommandAction.Install;
        if (verify && selectedAction != SetupCommandAction.Install)
        {
            return SetupCommandLineParseResult.Failure("--verify is only valid with install.");
        }

        return SetupCommandLineParseResult.Success(new SetupCommandLine(selectedAction, silent, verify));
    }
}
