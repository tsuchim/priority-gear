using System.Text.Json;
using PriorityGear.Contracts;

namespace PriorityGear.Service;

public sealed class MachineRuleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public MachineRuleStore()
        : this(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PriorityGear",
            "rules.machine.json"))
    {
    }

    public MachineRuleStore(string path)
    {
        Path = path;
    }

    public string Path { get; }

    public IReadOnlyList<MachinePriorityRule> Load()
    {
        if (!File.Exists(Path))
        {
            return [];
        }

        using FileStream stream = File.OpenRead(Path);
        return JsonSerializer.Deserialize<List<MachinePriorityRule>>(stream, JsonOptions) ?? [];
    }

    public MachineRuleStoreResult TryLoad()
    {
        try
        {
            return MachineRuleStoreResult.Success(Load());
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return MachineRuleStoreResult.Failure(ex.Message);
        }
    }

    public MachineRuleStoreResult Save(IReadOnlyList<MachinePriorityRule> rules)
    {
        try
        {
            string directory = System.IO.Path.GetDirectoryName(Path)!;
            Directory.CreateDirectory(directory);
            string tempPath = Path + ".tmp";
            using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, rules, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, Path, overwrite: true);
            return MachineRuleStoreResult.Success(rules);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return MachineRuleStoreResult.Failure(ex.Message);
        }
    }
}

public sealed record MachineRuleStoreResult(bool Succeeded, IReadOnlyList<MachinePriorityRule> Rules, string Error)
{
    public static MachineRuleStoreResult Success(IReadOnlyList<MachinePriorityRule> rules)
    {
        return new MachineRuleStoreResult(true, rules, string.Empty);
    }

    public static MachineRuleStoreResult Failure(string error)
    {
        return new MachineRuleStoreResult(false, [], error);
    }
}
