using System.Runtime.InteropServices;
using QuickAttach.ViewModels;

namespace QuickAttach.Services;

public class WindowManager
{
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;


    // P/Invoke declarations
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);

    // This function will get all window handles for a given process ID
    public List<IntPtr> GetRootWindowsOfProcess(int pid)
    {
        var windowHandles = new List<IntPtr>();
        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out var windowPid);
            if (windowPid == pid)
            {
                windowHandles.Add(hWnd);
            }

            return true;
        }, IntPtr.Zero);

        return windowHandles;
    }

    public void SetWindowPositions(List<IntPtr> windowHandles)
    {
        IntPtr hwndTopmost = new(-1);

        foreach (var handle in windowHandles)
        {
            SetWindowPos(handle, hwndTopmost, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}