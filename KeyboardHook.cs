using AltTabber.Native;

namespace AltTabber
{
    internal static class KeyboardHook
    {
        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc? _proc;

        public static void Install(Func<IntPtr, int, IntPtr, IntPtr, IntPtr> onHotkey)
        {
            _proc = (nCode, wParam, lParam) =>
            {
                return onHotkey(_hookID, nCode, wParam, lParam);
            };
            _hookID = Win32.SetWindowsHookEx(Constants.WH_KEYBOARD_LL, _proc, Win32.GetModuleHandle(null), 0);
        }

        public static void Uninstall()
        {
            if (_hookID != IntPtr.Zero)
                Win32.UnhookWindowsHookEx(_hookID);
        }
    }
}
