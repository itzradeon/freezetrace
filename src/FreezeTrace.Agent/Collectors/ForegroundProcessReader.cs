using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FreezeTrace.Agent.Collectors;

internal static class ForegroundProcessReader
{
    public static string? TryGetForegroundProcessName()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return null;

        _ = GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
            return null;

        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
