using AltTabber.Models;
using AltTabber.Native;
using System.Diagnostics;
using System.Text;

namespace AltTabber
{
    internal static class WindowManager
    {

        private static readonly Dictionary<string, char> _processMap = new()
        {
            { "code", 'v' }
        };

        public static List<WindowInfo> GetOpenWindows()
        {
            List<WindowInfo> result = new();
            IntPtr shellWindow = Win32.GetShellWindow();

            Win32.EnumWindows((hWnd, lParam) =>
            {
                if (hWnd == shellWindow) return true;
                if (!Win32.IsWindowVisible(hWnd)) return true;
                if (Win32.GetWindow(hWnd, Constants.GW_OWNER) != IntPtr.Zero) return true;

                int length = Win32.GetWindowTextLength(hWnd);
                if (length == 0) return true;

                StringBuilder sb = new(length + 1);
                Win32.GetWindowText(hWnd, sb, sb.Capacity);

                IntPtr hIcon = Win32.SendMessage(hWnd, Constants.WM_GETICON, Constants.ICON_SMALL2, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.SendMessage(hWnd, Constants.WM_GETICON, Constants.ICON_BIG, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.GetClassLongPtr(hWnd, Constants.GCL_HICON);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.GetClassLongPtr(hWnd, Constants.GCL_HICONSM);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.SendMessage(hWnd, Constants.WM_GETICON, Constants.ICON_BIG, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = Win32.SendMessage(hWnd, Constants.WM_GETICON, Constants.ICON_BIG, IntPtr.Zero);
                if (hIcon == new IntPtr(65579))
                    hIcon = IntPtr.Zero;
                if (hIcon == IntPtr.Zero) return true;

                uint pid;
                Win32.GetWindowThreadProcessId(hWnd, out pid);
                string processName = Process.GetProcessById((int)pid).ProcessName;
                char hotKey = string.IsNullOrEmpty(processName) ? '?' : processName[0];

                if (_processMap.ContainsKey(processName.ToLower()))
                    hotKey = _processMap[processName.ToLower()];

                result.Add(new WindowInfo
                {
                    Hwnd = hWnd,
                    Title = sb.ToString(),
                    ProcessName = processName,
                    HotKey = char.ToUpper(hotKey),
                    Icon = hIcon
                });
                return true;
            }, IntPtr.Zero);

            return result.OrderBy(o => o.Title).ToList();
        }
    }
}
