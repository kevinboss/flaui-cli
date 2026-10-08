using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FlauiCli.Core;

/// <summary>
/// Captures a top-level window through PrintWindow, so the pixels are the window's own even when
/// another window covers it and no focus is stolen. Returns null when the window can't render
/// itself (minimized, or the area lies outside it), so callers can fall back to a screen grab.
/// </summary>
internal static class WindowCapture
{
    private const uint PW_RENDERFULLCONTENT = 2;

    public static (Bitmap Image, Point Origin)? Print(IntPtr hwnd, Rectangle area)
    {
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var rect)) return null;
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0) return null;

        using var window = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(window))
        {
            var hdc = g.GetHdc();
            try
            {
                if (!PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT)) return null;
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        var crop = Rectangle.Intersect(
            new Rectangle(area.X - rect.Left, area.Y - rect.Top, area.Width, area.Height),
            new Rectangle(0, 0, width, height));
        if (crop.IsEmpty) return null;

        return (window.Clone(crop, window.PixelFormat), new Point(rect.Left + crop.X, rect.Top + crop.Y));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdc, uint flags);
}
