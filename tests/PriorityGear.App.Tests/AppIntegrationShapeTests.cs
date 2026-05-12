using System.IO;

namespace PriorityGear.App.Tests;

public sealed class AppIntegrationShapeTests
{
    [Fact]
    public void MainWindow_DoesNotContainOldDirectApplyRulesPath()
    {
        string source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "PriorityGear.App",
            "MainWindow.xaml.cs"));

        Assert.Contains("MonitoringController", source);
        Assert.DoesNotContain("private void ApplyRules", source);
        Assert.DoesNotContain("PriorityRuleEngine _ruleEngine", source);
    }

    private static string FindRepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "PriorityGear.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
