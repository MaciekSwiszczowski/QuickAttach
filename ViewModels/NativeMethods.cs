using System.Runtime.InteropServices;

namespace QuickAttach.ViewModels;

public static class NativeMethods
{
    // ReSharper disable twice InconsistentNaming
    // ReSharper disable twice IdentifierTypo
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public static readonly IntPtr HWND_TOPMOST = new(-1);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);
}