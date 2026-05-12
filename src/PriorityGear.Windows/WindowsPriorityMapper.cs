using System.Diagnostics;
using PriorityGear.Core;

namespace PriorityGear.Windows;

public static class WindowsPriorityMapper
{
    public static ProcessPriorityClass ToWindows(ProcessPriorityLevel priority)
    {
        return priority switch
        {
            ProcessPriorityLevel.Idle => ProcessPriorityClass.Idle,
            ProcessPriorityLevel.BelowNormal => ProcessPriorityClass.BelowNormal,
            ProcessPriorityLevel.Normal => ProcessPriorityClass.Normal,
            ProcessPriorityLevel.AboveNormal => ProcessPriorityClass.AboveNormal,
            ProcessPriorityLevel.High => ProcessPriorityClass.High,
            _ => ProcessPriorityClass.Normal
        };
    }

    public static ProcessPriorityLevel FromWindows(ProcessPriorityClass priority)
    {
        return priority switch
        {
            ProcessPriorityClass.Idle => ProcessPriorityLevel.Idle,
            ProcessPriorityClass.BelowNormal => ProcessPriorityLevel.BelowNormal,
            ProcessPriorityClass.Normal => ProcessPriorityLevel.Normal,
            ProcessPriorityClass.AboveNormal => ProcessPriorityLevel.AboveNormal,
            ProcessPriorityClass.High => ProcessPriorityLevel.High,
            _ => ProcessPriorityLevel.Normal
        };
    }
}
