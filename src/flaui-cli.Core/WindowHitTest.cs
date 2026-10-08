using System.Drawing;
using System.Runtime.InteropServices;

namespace FlauiCli.Core;

/// <summary>
/// Answers whether a screen point currently shows a window of a given process. A coordinate click
/// lands on whatever is on top, so this is the guard against clicking into an unrelated window.
/// </summary>
internal static class WindowHitTest
{
    private const uint GA_ROOT = 2;

    public static bool BelongsToProcess(Point point, int processId)
    {
        var hwnd = WindowFromPoint(new POINT { X = point.X, Y = point.Y });
        if (hwnd == IntPtr.Zero) return false;
        GetWindowThreadProcessId(GetAncestor(hwnd, GA_ROOT), out var owner);
        return owner == processId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out int processId);
}
