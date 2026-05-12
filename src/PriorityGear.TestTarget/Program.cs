int holdSeconds = ReadHoldSeconds(args);
using CancellationTokenSource cancellation = new(TimeSpan.FromSeconds(holdSeconds));

Console.WriteLine($"PriorityGear.TestTarget PID={Environment.ProcessId} HoldSeconds={holdSeconds}");

try
{
    await Task.Delay(Timeout.InfiniteTimeSpan, cancellation.Token);
}
catch (OperationCanceledException)
{
}

return 0;

static int ReadHoldSeconds(string[] args)
{
    const int defaultSeconds = 120;
    for (int index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], "--hold-seconds", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(args[index + 1], out int seconds) &&
            seconds > 0)
        {
            return seconds;
        }
    }

    return defaultSeconds;
}
