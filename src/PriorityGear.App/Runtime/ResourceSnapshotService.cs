using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PriorityGear.Core;

namespace PriorityGear.App.Runtime;

public interface IResourceSnapshotService
{
    Task<IReadOnlyDictionary<int, ProcessResourceSnapshot>> CaptureAsync(TimeSpan duration, CancellationToken cancellationToken);
}

public sealed class ResourceSnapshotService : IResourceSnapshotService
{
    public async Task<IReadOnlyDictionary<int, ProcessResourceSnapshot>> CaptureAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        Dictionary<int, ResourceSample> start = CaptureSamples();
        await Task.Delay(duration, cancellationToken);
        Dictionary<int, ResourceSample> end = CaptureSamples();

        int logicalProcessors = Environment.ProcessorCount;
        Dictionary<int, ProcessResourceSnapshot> result = [];
        foreach ((int processId, ResourceSample startSample) in start)
        {
            ResourceSample endSample;
            if (!end.TryGetValue(processId, out endSample!))
            {
                endSample = new ResourceSample(processId, null, null, ResourceMetricStatus.ProcessExited, "Process exited during snapshot.");
            }

            result[processId] = ResourceSnapshotCalculator.Calculate(
                startSample,
                endSample,
                duration,
                logicalProcessors,
                ResourceMetric<double>.Unavailable(ResourceMetricStatus.Unsupported, "GPU attribution is unsupported on this machine or unavailable through verified counters."));
        }

        return result;
    }

    private static Dictionary<int, ResourceSample> CaptureSamples()
    {
        Dictionary<int, ResourceSample> samples = [];
        foreach (Process process in Process.GetProcesses())
        {
            using (process)
            {
                ResourceSample sample = CaptureSample(process);
                if (sample.ProcessId > 0)
                {
                    samples[sample.ProcessId] = sample;
                }
            }
        }

        return samples;
    }

    private static int SafeId(Process process)
    {
        try { return process.Id; } catch { return -1; }
    }

    private static ResourceSample CaptureSample(Process process)
    {
        int processId = SafeId(process);
        if (processId <= 0)
        {
            return new ResourceSample(processId, null, null, ResourceMetricStatus.ProcessExited, "Process exited before it could be sampled.");
        }

        try
        {
            TimeSpan cpu = process.TotalProcessorTime;
            long? diskBytes = TryGetIoBytes(process.Handle, out string? ioMessage);
            return new ResourceSample(
                processId,
                cpu,
                diskBytes,
                ResourceMetricStatus.Available,
                ioMessage);
        }
        catch (Win32Exception ex)
        {
            return new ResourceSample(processId, null, null, ResourceMetricStatus.AccessDenied, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ResourceSample(processId, null, null, ResourceMetricStatus.AccessDenied, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new ResourceSample(processId, null, null, ResourceMetricStatus.ProcessExited, ex.Message);
        }
    }

    private static long? TryGetIoBytes(nint processHandle, out string? message)
    {
        if (!NativeMethods.GetProcessIoCounters(processHandle, out IoCounters counters))
        {
            int error = Marshal.GetLastWin32Error();
            message = new Win32Exception(error).Message;
            return null;
        }

        message = null;
        return checked((long)(counters.ReadTransferCount + counters.WriteTransferCount + counters.OtherTransferCount));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    private static partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetProcessIoCounters(nint processHandle, out IoCounters counters);
    }
}
