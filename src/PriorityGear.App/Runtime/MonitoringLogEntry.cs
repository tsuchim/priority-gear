namespace PriorityGear.App.Runtime;

public sealed record MonitoringLogEntry(
    DateTimeOffset Timestamp,
    string Category,
    string Message)
{
    public override string ToString()
    {
        return $"{Timestamp:HH:mm:ss} [{Category}] {Message}";
    }
}
