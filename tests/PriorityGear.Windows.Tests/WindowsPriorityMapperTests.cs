using System.Diagnostics;
using PriorityGear.Core;
using PriorityGear.Windows;

namespace PriorityGear.Windows.Tests;

public sealed class WindowsPriorityMapperTests
{
    [Theory]
    [InlineData(ProcessPriorityLevel.Idle, ProcessPriorityClass.Idle)]
    [InlineData(ProcessPriorityLevel.BelowNormal, ProcessPriorityClass.BelowNormal)]
    [InlineData(ProcessPriorityLevel.Normal, ProcessPriorityClass.Normal)]
    [InlineData(ProcessPriorityLevel.AboveNormal, ProcessPriorityClass.AboveNormal)]
    [InlineData(ProcessPriorityLevel.High, ProcessPriorityClass.High)]
    public void ToWindows_MapsSupportedPriorities(ProcessPriorityLevel priority, ProcessPriorityClass expected)
    {
        Assert.Equal(expected, WindowsPriorityMapper.ToWindows(priority));
    }

    [Fact]
    public void NormalRulePriorityValues_DoNotExposeRealtime()
    {
        Assert.DoesNotContain(
            Enum.GetNames<ProcessPriorityLevel>(),
            static name => string.Equals(name, "Realtime", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(5, Win32PriorityStatus.AccessDenied)]
    [InlineData(6, Win32PriorityStatus.ProcessExited)]
    [InlineData(87, Win32PriorityStatus.InvalidParameter)]
    [InlineData(1300, Win32PriorityStatus.PrivilegeUnavailable)]
    public void Win32ErrorMapper_MapsKnownErrors(int error, Win32PriorityStatus expected)
    {
        Assert.Equal(expected, Win32ErrorMapper.Map(error));
    }
}
