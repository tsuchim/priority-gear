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
}
