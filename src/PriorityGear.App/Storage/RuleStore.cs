using System.IO;
using System.Text.Json;
using PriorityGear.Core;

namespace PriorityGear.App.Storage;

public sealed record RuleStoreLoadResult(
    bool Succeeded,
    IReadOnlyList<PriorityRule> Rules,
    string? ErrorMessage,
    string Path);

public sealed record RuleStoreSaveResult(
    bool Succeeded,
    string? ErrorMessage,
    string Path);

public sealed class RuleStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _path;

    public RuleStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PriorityGear",
            "rules.json"))
    {
    }

    public RuleStore(string path)
    {
        _path = path;
    }

    public RuleStoreLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new RuleStoreLoadResult(true, [], null, _path);
        }

        try
        {
            string json = File.ReadAllText(_path);
            List<PriorityRule>? rules = JsonSerializer.Deserialize<List<PriorityRule>>(json, JsonOptions);
            PreserveLegacyActivePriority(json, rules);
            return new RuleStoreLoadResult(true, rules ?? [], null, _path);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new RuleStoreLoadResult(false, [], ex.Message, _path);
        }
    }

    private static void PreserveLegacyActivePriority(string json, List<PriorityRule>? rules)
    {
        if (rules is null)
        {
            return;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        int index = 0;
        foreach (JsonElement element in document.RootElement.EnumerateArray())
        {
            if (index >= rules.Count)
            {
                break;
            }

            bool hasActivePriority = HasProperty(element, "activePriority");
            bool hasActiveMode = HasProperty(element, "activeModeEnabled");
            if (hasActivePriority && !hasActiveMode)
            {
                rules[index].ActiveModeEnabled = true;
            }

            index++;
        }
    }

    private static bool HasProperty(JsonElement element, string name)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public RuleStoreSaveResult Save(IEnumerable<PriorityRule> rules)
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string tempPath = _path + ".tmp";
        try
        {
            using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, rules.ToList(), JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_path))
            {
                File.Replace(tempPath, _path, null);
            }
            else
            {
                File.Move(tempPath, _path);
            }

            return new RuleStoreSaveResult(true, null, _path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new RuleStoreSaveResult(false, ex.Message, _path);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (IOException)
            {
                // Temp cleanup failure should not hide the original persistence result.
            }
        }
    }
}
