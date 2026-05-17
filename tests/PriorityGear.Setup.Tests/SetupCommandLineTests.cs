using PriorityGear.Setup;

namespace PriorityGear.Setup.Tests;

public sealed class SetupCommandLineTests
{
    [Fact]
    public void NoArgumentsSelectsInteractiveInstall()
    {
        SetupCommandLineParseResult result = SetupCommandLineParser.Parse([]);

        Assert.True(result.Succeeded);
        Assert.Equal(SetupCommandAction.Install, result.Command!.Action);
        Assert.False(result.Command.Silent);
    }

    [Fact]
    public void SilentInstallSelectsInstallWithoutUi()
    {
        SetupCommandLineParseResult result = SetupCommandLineParser.Parse(["--install", "--silent"]);

        Assert.True(result.Succeeded);
        Assert.Equal(SetupCommandAction.Install, result.Command!.Action);
        Assert.True(result.Command.Silent);
    }

    [Fact]
    public void QuietUninstallSelectsUninstallWithoutUi()
    {
        SetupCommandLineParseResult result = SetupCommandLineParser.Parse(["--uninstall", "--quiet"]);

        Assert.True(result.Succeeded);
        Assert.Equal(SetupCommandAction.Uninstall, result.Command!.Action);
        Assert.True(result.Command.Silent);
    }

    [Fact]
    public void HelpAndVersionAreExplicitActions()
    {
        Assert.Equal(SetupCommandAction.Help, SetupCommandLineParser.Parse(["--help"]).Command!.Action);
        Assert.Equal(SetupCommandAction.Version, SetupCommandLineParser.Parse(["--version"]).Command!.Action);
    }

    [Fact]
    public void UnknownOptionFails()
    {
        SetupCommandLineParseResult result = SetupCommandLineParser.Parse(["--surprise"]);

        Assert.False(result.Succeeded);
        Assert.Contains("--surprise", result.Message);
    }

    [Fact]
    public void MultipleActionsFail()
    {
        SetupCommandLineParseResult result = SetupCommandLineParser.Parse(["--install", "--uninstall"]);

        Assert.False(result.Succeeded);
        Assert.Contains("Only one action", result.Message);
    }

    [Fact]
    public void VerifyIsOnlyValidForInstall()
    {
        SetupCommandLineParseResult result = SetupCommandLineParser.Parse(["--uninstall", "--verify"]);

        Assert.False(result.Succeeded);
        Assert.Contains("--verify", result.Message);
    }
}
