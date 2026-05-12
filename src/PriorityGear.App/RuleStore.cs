using System.IO;
using System.Text.Json;
using PriorityGear.Core;

namespace PriorityGear.App;

public sealed class RuleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _path;

    public RuleStore()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PriorityGear");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "rules.json");
    }

    public IReadOnlyList<PriorityRule> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        using FileStream stream = File.OpenRead(_path);
        return JsonSerializer.Deserialize<List<PriorityRule>>(stream, JsonOptions) ?? [];
    }

    public void Save(IEnumerable<PriorityRule> rules)
    {
        using FileStream stream = File.Create(_path);
        JsonSerializer.Serialize(stream, rules.ToList(), JsonOptions);
    }
}
