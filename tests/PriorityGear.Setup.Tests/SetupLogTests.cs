using PriorityGear.Setup;

namespace PriorityGear.Setup.Tests;

public sealed class SetupLogTests
{
    [Fact]
    public void SetupLogInvokesProgressCallbackAndFlushesImmediately()
    {
        string path = Path.Combine(Path.GetTempPath(), "PriorityGear.Setup.Tests", Guid.NewGuid().ToString("N"), "setup.log");
        SetupLog log = new(path);
        List<string> lines = [];
        log.LineWritten += lines.Add;

        log.Info("PriorityGear Setup started.");

        Assert.Single(lines);
        Assert.Contains("PriorityGear Setup started.", lines[0]);
        Assert.True(File.Exists(path));
        Assert.Contains("PriorityGear Setup started.", File.ReadAllText(path));
    }

    [Fact]
    public void SetupLogSubscriberFailureDoesNotCrashLogging()
    {
        string path = Path.Combine(Path.GetTempPath(), "PriorityGear.Setup.Tests", Guid.NewGuid().ToString("N"), "setup.log");
        SetupLog log = new(path);
        log.LineWritten += _ => throw new InvalidOperationException("form disposed");

        log.Info("PriorityGear Setup started.");

        Assert.True(File.Exists(path));
        Assert.Contains("PriorityGear Setup started.", File.ReadAllText(path));
    }

    [Fact]
    public void SetupLogIoFailureDoesNotCrashLogging()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), "PriorityGear.Setup.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        SetupLog log = new(directoryPath);
        List<string> lines = [];
        log.LineWritten += lines.Add;

        log.Info("PriorityGear Setup started.");
        log.Flush();

        Assert.Single(lines);
        Assert.Contains("PriorityGear Setup started.", log.ToString());
    }

    [Fact]
    public void InitialLinesStartWithVisibleSetupMessage()
    {
        IReadOnlyList<string> lines = SetupStartup.InitialLines(
            new SetupCommandLine(SetupCommandAction.Install, Silent: false, Verify: false),
            @"C:\ProgramData\PriorityGear\Logs\setup-test.log");

        Assert.Equal("PriorityGear Setup started.", lines[0]);
        Assert.Contains("Log:", lines[1]);
        Assert.Contains("Mode: install/update", lines[2]);
    }
}
