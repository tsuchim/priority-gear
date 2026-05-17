using System.Text;

namespace PriorityGear.Setup;

public sealed class SetupLog(string path)
{
    private readonly StringBuilder _content = new();
    private readonly object _sync = new();

    public string Path { get; } = path;

    public event Action<string>? LineWritten;

    public void Info(string message) => Write("INFO", message);

    public void Fail(string message) => Write("FAIL", message);

    public void Section(string title) => Write("STEP", title);

    public void Flush()
    {
        lock (_sync)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.WriteAllText(Path, _content.ToString(), Encoding.UTF8);
        }
    }

    public override string ToString()
    {
        lock (_sync)
        {
            return _content.ToString();
        }
    }

    private void Write(string level, string message)
    {
        string line = $"[{DateTimeOffset.Now:u}] {level}: {message}";
        lock (_sync)
        {
            _content.AppendLine(line);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            File.AppendAllText(Path, line + Environment.NewLine, Encoding.UTF8);
        }

        LineWritten?.Invoke(line);
    }
}
