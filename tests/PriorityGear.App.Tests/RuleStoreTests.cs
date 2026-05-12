using System.IO;
using PriorityGear.App.Storage;
using PriorityGear.Core;

namespace PriorityGear.App.Tests;

public sealed class RuleStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsStableRuleId()
    {
        string path = TempPath();
        RuleStore store = new(path);
        PriorityRule rule = PriorityRule.ForExecutable("notepad.exe");

        RuleStoreSaveResult save = store.Save([rule]);
        RuleStoreLoadResult load = store.Load();

        Assert.True(save.Succeeded);
        Assert.True(load.Succeeded);
        Assert.Equal(rule.Id, load.Rules.Single().Id);
    }

    [Fact]
    public void MalformedJsonReportsFailureAndIsNotOverwrittenByLoad()
    {
        string path = TempPath();
        File.WriteAllText(path, "{ malformed");
        RuleStore store = new(path);

        RuleStoreLoadResult load = store.Load();

        Assert.False(load.Succeeded);
        Assert.Equal("{ malformed", File.ReadAllText(path));
    }

    [Fact]
    public void AtomicSaveWritesValidJson()
    {
        string path = TempPath();
        RuleStore store = new(path);

        RuleStoreSaveResult save = store.Save([PriorityRule.ForExecutable("notepad.exe")]);
        RuleStoreLoadResult load = store.Load();

        Assert.True(save.Succeeded);
        Assert.True(load.Succeeded);
        Assert.Single(load.Rules);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void DeletedRuleIsRemovedFromPersistence()
    {
        string path = TempPath();
        RuleStore store = new(path);
        PriorityRule keep = PriorityRule.ForExecutable("keep.exe");
        PriorityRule delete = PriorityRule.ForExecutable("delete.exe");
        Assert.True(store.Save([keep, delete]).Succeeded);

        RuleStoreSaveResult save = store.Save([keep]);
        RuleStoreLoadResult load = store.Load();

        Assert.True(save.Succeeded);
        Assert.True(load.Succeeded);
        Assert.Single(load.Rules);
        Assert.Equal(keep.Id, load.Rules.Single().Id);
    }

    private static string TempPath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "rules.json");
    }
}
