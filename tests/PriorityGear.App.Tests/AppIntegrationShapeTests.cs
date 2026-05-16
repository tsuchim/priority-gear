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

    [Fact]
    public void AppProject_UsesPriorityGearIconForExecutableAndTrayResource()
    {
        string root = FindRepositoryRoot();
        string project = File.ReadAllText(Path.Combine(root, "src", "PriorityGear.App", "PriorityGear.App.csproj"));
        string window = File.ReadAllText(Path.Combine(root, "src", "PriorityGear.App", "MainWindow.xaml"));

        Assert.Contains("<ApplicationIcon>..\\..\\assets\\prioritygear.ico</ApplicationIcon>", project);
        Assert.Contains("Link=\"Assets\\prioritygear.ico\"", project);
        Assert.Contains("Icon=\"Assets/prioritygear.ico\"", window);
        Assert.True(File.Exists(Path.Combine(root, "assets", "prioritygear.ico")));
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
