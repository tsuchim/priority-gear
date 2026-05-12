using System.Text;

namespace PriorityGear.VerificationSetup;

public sealed class VerificationLog(string path)
{
    private readonly StringBuilder _content = new();

    public string Path { get; } = path;

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Fail(string message)
    {
        Write("FAIL", message);
    }

    public void Section(string title)
    {
        Write("STEP", title);
    }

    public void Flush()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, _content.ToString(), Encoding.UTF8);
    }

    public override string ToString()
    {
        return _content.ToString();
    }

    private void Write(string level, string message)
    {
        _content.Append('[')
            .Append(DateTimeOffset.Now.ToString("u"))
            .Append("] ")
            .Append(level)
            .Append(": ")
            .AppendLine(message);
    }
}
