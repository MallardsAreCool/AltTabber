namespace AltTabber.Native
{
    internal static class Constants
    {
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_EX_TOOLWINDOW = unchecked((int)0x00000080L);
        public const int WS_EX_TOPMOST = unchecked((int)0x00000008L);
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_RESTORE = 9;
        public const uint WM_DESTROY = 0x0002;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_CONTROL = 0x11;
        public const int VK_UP = 0x26;
        public const int VK_DOWN = 0x28;
        public const int VK_PRESSED = 0x8000;
        public const int VK_SPACE = 0x20;
        public const int VK_RETURN = 0x0D;
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
        public const int IDC_ARROW = 32512;
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN_MSG = 0x0100;
        public const int WM_PAINT = 0x000F;
        public const int WM_APP = 0x8000;
        public const int NIF_MESSAGE = 0x00000001;
        public const int NIF_ICON = 0x00000002;
        public const int NIF_TIP = 0x00000004;
        public const int NIM_ADD = 0x00000000;
        public const int WM_RBUTTONUP = 0x0205;
        public const uint TPM_LEFTALIGN = 0x0000;
        public const uint TPM_RIGHTBUTTON = 0x0002;
        public const uint WM_COMMAND = 0x111;
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_MOUSEMOVE = 0x0200;
        public const uint WM_MOUSELEAVE = 0x02A3;
        public const uint WM_SETCURSOR = 0x0020;
        public const int ID_EXIT = 1;
        public const int LINE_HEIGHT = 50;
        public const int WINDOW_WIDTH = 640;
        public const uint TME_LEAVE = 0x00000002;
        public const int IDC_HAND = 32649;
        public const uint IMAGE_ICON = 1;
        public const int WS_EX_LAYERED = 0x80000;
        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x02;
        public const int SW_SHOWMINIMIZED = 2;
        public const uint GW_OWNER = 4;
        public const uint WM_GETICON = 0x007F;
        public const int ICON_SMALL2 = 2;
        public const int ICON_BIG = 1;
        public const int GCL_HICON = -14;
        public const int GCL_HICONSM = -34;
        public const int MF_STRING = unchecked((int)0x00000000L);
        public const int LR_SHARED = 0x00008000;
    }
}
