using System.Runtime.InteropServices;

namespace PriorityGear.Windows;

public sealed class ForegroundWindowProvider
{
    public int? GetForegroundProcessId()
    {
        nint window = NativeMethods.GetForegroundWindow();
        if (window == 0)
        {
            return null;
        }

        _ = NativeMethods.GetWindowThreadProcessId(window, out uint processId);
        return processId == 0 ? null : checked((int)processId);
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
    }
}
