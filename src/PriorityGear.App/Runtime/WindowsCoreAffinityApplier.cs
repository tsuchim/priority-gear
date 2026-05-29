using System.ComponentModel;
using System.Diagnostics;
using PriorityGear.Core;
using PriorityGear.Windows;

namespace PriorityGear.App.Runtime;

public sealed class WindowsCoreAffinityApplier(WindowsCoreTopologyProvider topologyProvider) : ICoreAffinityApplier
{
    public CoreReserveApplyResult ApplyCoreReserve(int processId, int reserveCount)
    {
        if (reserveCount == 0)
        {
            return CoreReserveApplyResult.Disabled();
        }

        CoreReservePlan plan;
        try
        {
            plan = CoreReservePlanner.Plan(topologyProvider.GetPhysicalCores(), reserveCount);
        }
        catch (Win32Exception ex)
        {
            return CoreReserveApplyResult.Failure(reserveCount, ex.Message, ex.NativeErrorCode.ToString());
        }

        if (!plan.Succeeded || plan.AllowedLogicalProcessorMask is null)
        {
            return CoreReserveApplyResult.Failure(reserveCount, plan.Message, "CoreTopologyUnsupported");
        }

        try
        {
            using Process process = Process.GetProcessById(processId);
            process.ProcessorAffinity = (nint)plan.AllowedLogicalProcessorMask.Value;
            return CoreReserveApplyResult.Success(reserveCount, plan.Message);
        }
        catch (Win32Exception ex)
        {
            return CoreReserveApplyResult.Failure(reserveCount, ex.Message, ex.NativeErrorCode.ToString());
        }
        catch (UnauthorizedAccessException ex)
        {
            return CoreReserveApplyResult.Failure(reserveCount, ex.Message, "UnauthorizedAccess");
        }
        catch (InvalidOperationException ex)
        {
            return CoreReserveApplyResult.Failure(reserveCount, ex.Message, "InvalidOperation");
        }
        catch (ArgumentException ex)
        {
            return CoreReserveApplyResult.Failure(reserveCount, ex.Message, "ProcessNotFound");
        }
    }
}
