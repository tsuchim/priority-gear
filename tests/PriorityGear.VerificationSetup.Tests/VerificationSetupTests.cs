using PriorityGear.VerificationSetup;

namespace PriorityGear.VerificationSetup.Tests;

[TestClass]
public sealed class VerificationSetupTests
{
    [TestMethod]
    public void RequiredPayloadListContainsAllVerificationBinaries()
    {
        CollectionAssert.AreEquivalent(
            new[]
            {
                "PriorityGear.Service.exe",
                "PriorityGear.Cli.exe",
                "PriorityGear.App.exe",
                "PriorityGear.TestTarget.exe"
            },
            VerificationPayload.RequiredFiles);
    }

    [TestMethod]
    public void MissingFilesReportsOnlyAbsentPayloadFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), "priority-gear-payload-test-" + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "PriorityGear.Service.exe"), string.Empty);

            IReadOnlyList<string> missing = VerificationPayload.MissingFiles(root);

            Assert.IsFalse(missing.Contains("PriorityGear.Service.exe"));
            Assert.IsTrue(missing.Contains("PriorityGear.Cli.exe"));
            Assert.IsTrue(missing.Contains("PriorityGear.App.exe"));
            Assert.IsTrue(missing.Contains("PriorityGear.TestTarget.exe"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void DefaultInstallPlanUsesProgramFilesAndExpectedServiceName()
    {
        VerificationInstallPlan plan = VerificationInstallPlan.Create("20260513-164500");

        Assert.AreEqual("PriorityGear.Service", plan.ServiceName);
        Assert.AreEqual("PriorityGear System Mode Service", plan.DisplayName);
        Assert.AreEqual("PriorityGear.TestTarget.Service", plan.TestTargetServiceName);
        Assert.AreEqual("PriorityGear TestTarget Service", plan.TestTargetDisplayName);
        Assert.AreEqual("20260513-164500", plan.Version);
        StringAssert.Contains(plan.BaseInstallDirectory, "PriorityGear");
        StringAssert.Contains(plan.VersionInstallDirectory, Path.Combine("versions", "20260513-164500"));
        StringAssert.Contains(plan.ServiceExePath, "PriorityGear.Service.exe");
        StringAssert.Contains(plan.ServiceExePath, plan.VersionInstallDirectory);
        StringAssert.Contains(plan.LogDirectory, "PriorityGear");
    }

    [TestMethod]
    public void VerificationLogFormatsFinalVerdictText()
    {
        string path = Path.Combine(Path.GetTempPath(), "priority-gear-verification-log-test.log");
        VerificationLog log = new(path);

        log.Section("Final verdict");
        log.Info("Final verdict: passed");

        string content = log.ToString();
        StringAssert.Contains(content, "STEP: Final verdict");
        StringAssert.Contains(content, "INFO: Final verdict: passed");
    }

    [TestMethod]
    public void PipeClientClassifiesEmptyResponse()
    {
        var response = PipeClient.ClassifyResponseLine(null);

        Assert.IsFalse(response.Succeeded);
        StringAssert.Contains(response.Message, "EOF");
    }

    [TestMethod]
    public void PipeClientClassifiesInvalidJsonResponse()
    {
        var response = PipeClient.ClassifyResponseLine("{");

        Assert.IsFalse(response.Succeeded);
        StringAssert.Contains(response.Message, "Invalid service response JSON");
    }
}
