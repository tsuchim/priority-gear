using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

int holdSeconds = ReadHoldSeconds(args);
bool serviceMode = args.Any(arg => string.Equals(arg, "--service", StringComparison.OrdinalIgnoreCase));

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "PriorityGear TestTarget Service");
builder.Services.AddSingleton(new HoldOptions(holdSeconds, serviceMode));
builder.Services.AddHostedService<TestTargetWorker>();

await builder.Build().RunAsync();

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

internal sealed record HoldOptions(int HoldSeconds, bool ServiceMode);

internal sealed class TestTargetWorker(HoldOptions options, ILogger<TestTargetWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PriorityGear.TestTarget started. PID={ProcessId}; HoldSeconds={HoldSeconds}; ServiceMode={ServiceMode}", Environment.ProcessId, options.HoldSeconds, options.ServiceMode);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.HoldSeconds), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
