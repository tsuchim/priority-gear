namespace PriorityGear.VerificationSetup;

public sealed record ScCommand(IReadOnlyList<string> Arguments)
{
    public string DisplayText => "sc.exe " + string.Join(" ", Arguments.Select(Escape));

    public static ScCommand CreateService(string serviceName, string binaryPath, string displayName)
    {
        return new ScCommand([
            "create",
            serviceName,
            "binPath=",
            binaryPath,
            "obj=",
            "LocalSystem",
            "start=",
            "demand",
            "DisplayName=",
            displayName
        ]);
    }

    public static ScCommand ConfigService(string serviceName, string binaryPath, string displayName)
    {
        return new ScCommand([
            "config",
            serviceName,
            "binPath=",
            binaryPath,
            "obj=",
            "LocalSystem",
            "start=",
            "demand",
            "DisplayName=",
            displayName
        ]);
    }

    public static ScCommand DeleteService(string serviceName)
    {
        return new ScCommand(["delete", serviceName]);
    }

    public static ScCommand QueryEx(string serviceName)
    {
        return new ScCommand(["queryex", serviceName]);
    }

    public static string ClassifyFailure(int exitCode)
    {
        return exitCode == 1639
            ? "sc.exe argument syntax error. Check command argument construction."
            : $"sc.exe exited with code {exitCode}.";
    }

    private static string Escape(string argument)
    {
        return argument.Any(char.IsWhiteSpace) || argument.Contains('"')
            ? "\"" + argument.Replace("\"", "\\\"") + "\""
            : argument;
    }
}
