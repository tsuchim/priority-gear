using System.Security.Principal;

namespace PriorityGear.Service;

public sealed class ServiceFileLog
{
    private readonly object _gate = new();

    public ServiceFileLog()
    {
        string directory = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "PriorityGear",
            "Logs");
        Directory.CreateDirectory(directory);
        Path = System.IO.Path.Combine(directory, "service-current.log");
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
