using PriorityGear.Setup;

namespace PriorityGear.Setup.Tests;

public sealed class SetupPlanningTests
{
    [Fact]
    public void InstallPlanUsesVersionedProgramFilesPath()
    {
        SetupInstallPlan plan = SetupInstallPlan.Create("v0.3.0");

        Assert.Equal("v0.3.0", plan.Version);
        Assert.Equal("PriorityGear.Service", plan.ServiceName);
        Assert.Equal("PriorityGear System Mode Service", plan.DisplayName);
        Assert.Contains("PriorityGear", plan.BaseInstallDirectory);
        Assert.Contains(Path.Combine("versions", "v0.3.0"), plan.VersionInstallDirectory);
        Assert.Contains("PriorityGear.Service.exe", plan.ServiceExePath);
        Assert.Contains(plan.VersionInstallDirectory, plan.ServiceExePath);
        Assert.Contains("PriorityGear.Setup.exe", plan.SetupExePath);
        Assert.Contains(plan.VersionInstallDirectory, plan.SetupExePath);
    }

    [Fact]
    public void InstallPlanRejectsPathLikeVersion()
    {
        Assert.Throws<ArgumentException>(() => SetupInstallPlan.Create(@"..\..\Windows"));
        Assert.Throws<ArgumentException>(() => SetupInstallPlan.Create(@"v0.3.0\.."));
        Assert.Throws<ArgumentException>(() => SetupInstallPlan.Create("v0.3.0-preview.1"));
    }

    [Fact]
    public void ServiceConfigurationPlanUsesLocalSystemAndServiceBinary()
    {
        SetupInstallPlan plan = SetupInstallPlan.Create("v0.3.0");
        SetupPlanSummary summary = SetupPlanner.CreateInstallOrUpdatePlan(plan);

        Assert.Equal("PriorityGear.Service", summary.ServiceName);
        Assert.Equal("LocalSystem", summary.ServiceAccount);
        Assert.Equal(plan.ServiceExePath, summary.ServiceBinaryPath);
    }

    [Fact]
    public void InstallPlanPreservesProgramDataRulesAndLogs()
    {
        SetupInstallPlan plan = SetupInstallPlan.Create("v0.3.0");
        SetupPlanSummary summary = SetupPlanner.CreateInstallOrUpdatePlan(plan);

        Assert.True(summary.PreserveProgramData);
        Assert.True(summary.PreserveMachineRules);
        Assert.True(summary.PreserveLogs);
        Assert.Contains("rules.machine.json", plan.MachineRulesPath);
        Assert.Contains("Logs", plan.LogDirectory);
    }

    [Fact]
    public void UninstallPlanPreservesProgramDataByDefault()
    {
        SetupPlanSummary summary = SetupPlanner.CreateUninstallPlan(SetupInstallPlan.Create("v0.3.0"));

        Assert.True(summary.PreserveProgramData);
        Assert.True(summary.PreserveMachineRules);
        Assert.True(summary.PreserveLogs);
    }

    [Fact]
    public void InstallerSuccessSummaryDoesNotSucceedWithoutServiceIdentity()
    {
        SetupResult result = SetupResult.InstallSucceeded(string.Empty);

        Assert.False(result.Succeeded);
        Assert.Contains("service status", result.Summary);
    }

    [Fact]
    public void RequiredPayloadListContainsProductionBinaries()
    {
        Assert.Equal(
            new[]
            {
                "PriorityGear.Service.exe",
                "PriorityGear.Cli.exe",
                "PriorityGear.App.exe"
            }.Order(),
            SetupPayload.RequiredFiles.Order());
    }

    [Fact]
    public void ReleaseArtifactNamesUseInstallerNaming()
    {
        string repoRoot = FindRepoRoot();
        string packageScript = File.ReadAllText(Path.Combine(repoRoot, "scripts", "package-release.ps1"));
        string inspector = File.ReadAllText(Path.Combine(repoRoot, "scripts", "inspect-release-artifacts.ps1"));

        Assert.Contains("PriorityGear-$TagName-win-x64-installer.zip", packageScript);
        Assert.Contains("PriorityGear-$TagName-SHA256SUMS.txt", packageScript);
        Assert.Contains("PriorityGear-$TagName-win-x64-installer.zip", inspector);
        Assert.Contains("PriorityGear-$TagName-SHA256SUMS.txt", inspector);
    }

    [Fact]
    public void PackageInspectionRequiresSetupExecutable()
    {
        string repoRoot = FindRepoRoot();
        string inspector = File.ReadAllText(Path.Combine(repoRoot, "scripts", "inspect-release-artifacts.ps1"));

        Assert.Contains("PriorityGear.Setup.exe", inspector);
        Assert.Contains("PriorityGear.Service.exe", inspector);
        Assert.Contains("PriorityGear.App.exe", inspector);
        Assert.Contains("PriorityGear.Cli.exe", inspector);
        Assert.Contains("win-x64-installer.zip", inspector);
        Assert.Contains("setup-version.txt", inspector);
        Assert.Contains("winget-install.json", inspector);
        Assert.Contains("--install --silent", inspector);
        Assert.Contains("--uninstall --silent", inspector);
    }

    [Fact]
    public void UninstallRegistrationUsesMachineScopeQuietUninstall()
    {
        SetupInstallPlan plan = SetupInstallPlan.Create("v0.3.2");
        IReadOnlyDictionary<string, string> values = UninstallRegistration.CreateValues(plan, plan.SetupExePath);

        Assert.Equal("PriorityGear", values["DisplayName"]);
        Assert.Equal("0.3.2", values["DisplayVersion"]);
        Assert.Equal("Yuji Tsuchimoto", values["Publisher"]);
        Assert.Equal(plan.VersionInstallDirectory, values["InstallLocation"]);
        Assert.Equal($"\"{plan.SetupExePath}\" --uninstall", values["UninstallString"]);
        Assert.Equal($"\"{plan.SetupExePath}\" --uninstall --silent", values["QuietUninstallString"]);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PriorityGear.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
