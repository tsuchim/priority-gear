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
            using FileStream stream = File.OpenRead(_path);
            List<PriorityRule>? rules = JsonSerializer.Deserialize<List<PriorityRule>>(stream, JsonOptions);
            return new RuleStoreLoadResult(true, rules ?? [], null, _path);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new RuleStoreLoadResult(false, [], ex.Message, _path);
        }
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
