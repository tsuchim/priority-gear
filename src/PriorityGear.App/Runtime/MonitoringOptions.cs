namespace PriorityGear.App.Runtime;

public sealed record MonitoringOptions(
    TimeSpan ForegroundPollingInterval,
    TimeSpan ProcessRescanInterval,
    TimeSpan ReapplyInterval,
    TimeSpan FailureLogThrottle)
{
    public static MonitoringOptions Default { get; } = new(
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(10));
}
