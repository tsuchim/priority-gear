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

    [Fact]
    public void Load_LegacyActivePriorityWithoutModeKeepsActiveOverride()
    {
        string path = TempPath();
        Guid id = Guid.NewGuid();
        File.WriteAllText(path, $$"""
        [
          {
            "id": "{{id}}",
            "displayName": "legacy.exe",
            "enabled": true,
            "match": { "executableName": "legacy.exe" },
            "basePriority": 2,
            "activePriority": 4,
            "scope": 0
          }
        ]
        """);

        RuleStoreLoadResult load = new RuleStore(path).Load();

        Assert.True(load.Succeeded);
        Assert.True(load.Rules.Single().ActiveModeEnabled);
        Assert.Equal(ProcessPriorityLevel.High, load.Rules.Single().ActivePriority);
    }

    [Fact]
    public void Save_NewRulePersistsSameAsNormalAndCoreReserveDefault()
    {
        string path = TempPath();
        PriorityRule rule = PriorityRule.ForExecutable("new.exe");

        Assert.False(rule.ActiveModeEnabled);
        Assert.Equal(0, rule.CoreReserve);

        RuleStore store = new(path);
        Assert.True(store.Save([rule]).Succeeded);
        string json = File.ReadAllText(path);

        Assert.Contains("\"activeModeEnabled\": false", json);
        Assert.Contains("\"coreReserve\": 0", json);
    }

    private static string TempPath()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PriorityGear.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "rules.json");
    }
}
