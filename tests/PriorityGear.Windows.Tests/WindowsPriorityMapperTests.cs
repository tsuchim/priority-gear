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
}
