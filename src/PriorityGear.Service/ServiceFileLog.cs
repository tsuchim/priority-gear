using System.Security.Principal;

namespace PriorityGear.Service;

public sealed class ServiceFileLog
{
    private readonly object _gate = new();

    public ServiceFileLog()
        : this(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PriorityGear",
            "Logs",
            "service-current.log"))
    {
    }

    public ServiceFileLog(string path)
    {
        string? directory = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Path = path;
    }

    public string Path { get; }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(Exception exception, string message)
    {
        Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    public void Startup()
    {
        Info($"Service startup. User={WindowsIdentity.GetCurrent().Name}; IsService={Environment.UserInteractive == false}; ProcessId={Environment.ProcessId}");
    }

    private void Write(string level, string message)
    {
        string line = $"[{DateTimeOffset.Now:u}] {level}: {message}{Environment.NewLine}";
        lock (_gate)
        {
            File.AppendAllText(Path, line);
        }
    }
}
