using AltTabber.Models;
using AltTabber.Native;
using AltTabber.Utils;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AltTabber
{
    internal class AltTabberWindow
    {
        private IntPtr _hwnd;
        private IntPtr _hMenu;
        private bool _visible;
        private List<WindowInfo> _windows = new();
        private bool _isSelecting = false;
        private int _hoveredRow = -1;
        static List<RECT> _rowRects = new List<RECT>();


        const int LINE_HEIGHT = 50;
        const int WINDOW_WIDTH = 640;

        public void Run()
        {
            InitWindow();
            InitSystemTray();

            KeyboardHook.Install((hookID, nCode, wParam, lParam) => OnHotkeyPressed(hookID, nCode, wParam, lParam));

            PollMessages();
        }

        private void InitWindow()
        {
            // Create instance
            IntPtr hInstance = Win32.GetModuleHandle(null);

            // Register class
            WNDCLASSEX wndClass = new()
            {
                cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                lpfnWndProc = WndProc,
                hInstance = hInstance,
                lpszClassName = "AltTabber",
                hCursor = Win32.LoadCursor(IntPtr.Zero, Constants.IDC_ARROW)
            };
            Win32.RegisterClassEx(ref wndClass);

            // Create window
            _hwnd = Win32.CreateWindowEx(
                Constants.WS_EX_TOOLWINDOW | Constants.WS_EX_TOPMOST | Constants.WS_EX_LAYERED,
                "AltTabber", "AltTabber",
                Constants.WS_POPUP, 0, 0, 640, 100,
                IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
        }

        private void InitSystemTray()
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) throw new PlatformNotSupportedException("Icon is only supported on Windows 6.1 and later.");

            // Add tray icon
            NOTIFYICONDATA nid = new NOTIFYICONDATA();
            nid.cbSize = Marshal.SizeOf(nid);
            nid.hWnd = _hwnd;
            nid.uID = 1001;
            nid.uFlags = Constants.NIF_ICON | Constants.NIF_MESSAGE | Constants.NIF_TIP;
            nid.uCallbackMessage = Constants.WM_APP + 1;

            Icon icon = new("icon.ico", 32, 32);
            nid.hIcon = icon.Handle;
            nid.szTip = "Alt Tabber";
            Win32.Shell_NotifyIcon(Constants.NIM_ADD, ref nid);

            // Create context menu
            _hMenu = Win32.CreatePopupMenu();
            Win32.AppendMenu(_hMenu, Constants.MF_STRING, 1, "Exit");
        }

        private void HandleSystemTrayClick(IntPtr wParam, IntPtr lParam)
        {
            if ((int)lParam == Constants.WM_RBUTTONUP)
            {
                Win32.GetCursorPos(out POINT pt);
                Win32.TrackPopupMenu(_hMenu, Constants.TPM_LEFTALIGN | Constants.TPM_RIGHTBUTTON, pt.x, pt.y, 0, _hwnd, IntPtr.Zero);
            }
            else if ((int)wParam == 1)
            {
                Win32.PostQuitMessage(0);
            }
        }

        private void PollMessages()
        {
            // Message loop
            MSG msg;
            while (Win32.GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
            {
                Win32.TranslateMessage(ref msg);
                Win32.DispatchMessage(ref msg);
            }

            KeyboardHook.Uninstall();
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case Constants.WM_DESTROY:
                    Win32.PostQuitMessage(0);
                    return IntPtr.Zero;

                case Constants.WM_PAINT:
                    DrawPopup(hWnd);
                    return IntPtr.Zero;

                case Constants.WM_APP + 1:
                case Constants.WM_COMMAND:
                    HandleSystemTrayClick(wParam, lParam);
                    return IntPtr.Zero;

                case Constants.WM_LBUTTONDOWN:
                    HandlePopupClick(lParam);

                    return IntPtr.Zero;
                case Constants.WM_MOUSEMOVE:
                    HandleMouseHover(lParam);

                    return IntPtr.Zero;
                case Constants.WM_MOUSELEAVE:
                    HandleMouseLeave();

                    return IntPtr.Zero;
                case Constants.WM_SETCURSOR:
                    if (_hoveredRow >= 0)
                    {
                        SetCursor(Constants.IDC_HAND);
                    }
                    else
                    {
                        SetCursor(Constants.IDC_ARROW);
                    }

                    return IntPtr.Zero;
            }

            return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void RefreshWindowList()
        {
            _windows = WindowManager.GetOpenWindows();
            RedrawPopup();
        }

        private IntPtr OnHotkeyPressed(IntPtr hookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == Constants.WM_KEYDOWN_MSG)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                bool isKeyHandled = _visible ? HandleKeyboardInputVisible(vkCode) : HandleKeyboardInputHidden(vkCode);
                if (isKeyHandled) return (IntPtr)1; // swallow key
            }

            return Win32.CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private bool HandleKeyboardInputHidden(int vkCode)
        {
            bool isKeyHandled = false;

            switch (vkCode)
            {
                case Constants.VK_SPACE:
                    if ((Win32.GetAsyncKeyState(Constants.VK_CONTROL) & Constants.VK_PRESSED) == Constants.VK_PRESSED)
                    {
                        ShowPopup();
                        isKeyHandled = true;
                    }
                    break;
            }
            return isKeyHandled;
        }

        private bool HandleKeyboardInputVisible(int vkCode)
        {
            bool isKeyHandled = false;

            switch (vkCode)
            {
                case Constants.VK_ESCAPE:
                    HidePopup();
                    isKeyHandled = true;
                    break;

                case Constants.VK_UP:
                    _hoveredRow--;

                    if (_hoveredRow <= -1)
                    {
                        _hoveredRow = 0;
                    }
                    RedrawPopup();
                    isKeyHandled = true;
                    break;

                case Constants.VK_DOWN:
                    _hoveredRow++;

                    if (_hoveredRow > _windows.Count - 1)
                    {
                        _hoveredRow = _windows.Count - 1;
                    }
                    RedrawPopup();
                    isKeyHandled = true;
                    break;

                case Constants.VK_RETURN:
                    if (_hoveredRow >= 0)
                    {
                        var win = _windows[_hoveredRow];
                        SwitchToWindow(win.Hwnd);
                        isKeyHandled = true;
                        break;

                    }
                    break;
                default:
                    char hotKey = (char)vkCode;

                    if (_isSelecting && hotKey >= '1' && hotKey <= '9')
                    {
                        SelectDuplicateWindowByHotKey(hotKey);
                        isKeyHandled = true;
                    }
                    else if (hotKey >= 'A' && hotKey <= 'Z')
                    {
                        FilterWindowsByHotKey(hotKey);
                        isKeyHandled = true;
                    }
                    break;
            }

            return isKeyHandled;
        }

        private void SelectDuplicateWindowByHotKey(char hotKey)
        {
            int index = hotKey - '1';
            if (index >= 0 && index < _windows.Count)
            {
                SwitchToWindow(_windows[index].Hwnd);
                _isSelecting = false;
                _windows.Clear();
            }
        }

        private void FilterWindowsByHotKey(char hotKey)
        {
            List<WindowInfo> tempWindow = _windows;
            _windows = _windows
                    .Where(w => char.ToUpper(w.HotKey) == char.ToUpper(hotKey))
                    .Take(9)
                    .ToList();
            if (_windows.Count == 0)
            {
                _windows = tempWindow;
                tempWindow.Clear();
                return;
            }

            if (_windows.Count == 1)
            {
                SwitchToWindow(_windows[0].Hwnd);
            }
            else if (_windows.Count > 1)
            {
                for (int i = 0; i < _windows.Count && i < 9; i++)
                {
                    _windows[i].HotKey = (char)('1' + i);
                }

                _isSelecting = true;
                RedrawPopup();
            }
        }

        private void SetCursor(int cursorID)
        {
            IntPtr hCursor = Win32.LoadCursor(IntPtr.Zero, cursorID);
            Win32.SetCursor(hCursor);
        }

        private void HandleMouseLeave()
        {
            _hoveredRow = -1;
            RedrawPopup();
        }

        private void HandleMouseHover(IntPtr lParam)
        {
            int mouseX = (short)(lParam.ToInt64() & 0xFFFF);
            int mouseY = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

            int newHovered = -1;

            for (int i = 0; i < _rowRects.Count; i++)
            {
                RECT r = _rowRects[i];
                if (mouseX >= r.left && mouseX <= r.right &&
                    mouseY >= r.top && mouseY <= r.bottom)
                {
                    newHovered = i;
                    break;
                }
            }

            if (newHovered != _hoveredRow)
            {
                _hoveredRow = newHovered;
                RedrawPopup();
            }

            TRACKMOUSEEVENT tme = new TRACKMOUSEEVENT
            {
                cbSize = (uint)Marshal.SizeOf(typeof(TRACKMOUSEEVENT)),
                dwFlags = Constants.TME_LEAVE,
                hwndTrack = _hwnd
            };
            Win32.TrackMouseEvent(ref tme);
        }

        private void HandlePopupClick(IntPtr lParam)
        {
            int mouseX = Win32.GET_X_LPARAM(lParam);
            int mouseY = Win32.GET_Y_LPARAM(lParam);

            for (int i = 0; i < _rowRects.Count; i++)
            {
                RECT r = _rowRects[i];
                if (mouseX >= r.left && mouseX <= r.right &&
                    mouseY >= r.top && mouseY <= r.bottom)
                {
                    var win = _windows[i];
                    SwitchToWindow(win.Hwnd);
                    break;
                }
            }

            HidePopup();
        }

        private void ShowPopup()
        {
            int totalHeight = _windows.Count * LINE_HEIGHT + 20;
            Win32.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, WINDOW_WIDTH + 80, totalHeight + 80, Constants.SWP_NOMOVE | Constants.SWP_NOZORDER);
            Win32.ShowWindow(_hwnd, Constants.SW_SHOW);
            Win32.SetForegroundWindow(_hwnd);
            RefreshWindowList();
            _visible = true;
            _isSelecting = false;
        }

        private void HidePopup()
        {
            Win32.ShowWindow(_hwnd, Constants.SW_HIDE);
            _visible = false;
            _isSelecting = false;
        }

        private void RedrawPopup()
        {
            Win32.InvalidateRect(_hwnd, IntPtr.Zero, true);
        }

        private void DrawPopup(IntPtr hWnd)
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) throw new PlatformNotSupportedException("GraphicsPath is only supported on Windows 6.1 and later.");

            PAINTSTRUCT ps;
            Win32.BeginPaint(hWnd, out ps);
            int height = _windows.Count * LINE_HEIGHT + 20;

            // Create offscreen ARGB surface
            using (Bitmap bmp = new Bitmap(WINDOW_WIDTH + 80, height + 80, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int cornerRadius = 16;
                int shadowSize = 40;
                int shadowOffset = 5;
                Rectangle rect = new Rectangle(40, 40, WINDOW_WIDTH, height);

                // --- Draw shadow ---
                using (GraphicsPath shadowPath = DrawingHelpers.RoundedRect(
                    new Rectangle(rect.Left - shadowSize, rect.Top - shadowSize + shadowOffset,
                                  rect.Width + shadowSize * 2, rect.Height + shadowSize * 2),
                    cornerRadius + shadowSize))
                using (PathGradientBrush brush = new PathGradientBrush(shadowPath))
                {
                    brush.CenterColor = Color.FromArgb(180, 0, 0, 0); // inner shadow dark
                    brush.SurroundColors = new[] { Color.FromArgb(0, 0, 0, 0) }; // fade out
                    brush.FocusScales = new PointF(0.5f, 0.0f);
                    g.FillPath(brush, shadowPath);
                }

                // --- Fill main background ---
                using (GraphicsPath path = DrawingHelpers.RoundedRect(rect, cornerRadius))
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(43, 43, 43)))
                    g.FillPath(bgBrush, path);

                // --- Border ---
                using (Pen pen = new Pen(Color.FromArgb(58, 58, 58), 1))
                    g.DrawPath(pen, DrawingHelpers.RoundedRect(rect, cornerRadius));

                // --- Rows (highlight, icon, text) ---
                using (Font font = new Font("Segoe UI", 12, FontStyle.Bold, GraphicsUnit.Point))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    int textOffset = 45;
                    int y = rect.Top + 20;
                    int x = rect.Left + 30;
                    _rowRects = new List<RECT>();

                    for (int i = 0; i < _windows.Count; i++)
                    {
                        var win = _windows[i];
                        int highlightOffset = 8;
                        RECT rectStore = new RECT
                        {
                            left = rect.Left,
                            top = y - highlightOffset,
                            right = rect.Right,
                            bottom = y + LINE_HEIGHT - highlightOffset - 2
                        };
                        _rowRects.Add(rectStore);

                        // Highlight row
                        if (i == _hoveredRow)
                        {
                            using (Brush hlBrush = new SolidBrush(Color.FromArgb(57, 57, 57)))
                            {
                                g.FillRectangle(hlBrush,
                                    new Rectangle(rectStore.left, rectStore.top,
                                                  rectStore.right - rectStore.left,
                                                  rectStore.bottom - rectStore.top));
                            }
                        }

                        // Draw icon
                        if (win.Icon != IntPtr.Zero)
                        {
                            using (Icon ico = Icon.FromHandle(win.Icon))
                            {
                                g.DrawIcon(ico, new Rectangle(x, y, 32, 32));
                            }
                        }

                        // Draw text
                        string hotKey = "(" + win.HotKey.ToString() + ")";
                        string text = hotKey + " | " + win.Title;
                        if (text.Length > 62)
                            text = text.Substring(0, 59) + "...";

                        g.DrawString(text, font, textBrush, x + textOffset, y + 4);

                        y += LINE_HEIGHT;
                    }
                }

                // --- Push bitmap to layered window ---
                IntPtr screenDC = Win32.GetDC(IntPtr.Zero);
                IntPtr memDC = Win32.CreateCompatibleDC(screenDC);
                IntPtr hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                IntPtr oldBmp = Win32.SelectObject(memDC, hBitmap);

                // Get screen dimensions
                int screenWidth = Win32.GetSystemMetrics(Constants.SM_CXSCREEN);
                int screenHeight = Win32.GetSystemMetrics(Constants.SM_CYSCREEN);

                // Calculate centered position
                int posX = (screenWidth - bmp.Width) / 2;
                int posY = (screenHeight - bmp.Height) / 4;

                // Update the topPos to center the window
                POINT topPos = new POINT(posX, posY);

                SIZE size = new SIZE(bmp.Width, bmp.Height);
                POINT pointSource = new POINT(0, 0);
                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = Constants.AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = Constants.AC_SRC_ALPHA
                };

                Win32.UpdateLayeredWindow(hWnd, screenDC, ref topPos, ref size, memDC,
                                    ref pointSource, 0, ref blend, Constants.ULW_ALPHA);

                // cleanup
                Win32.SelectObject(memDC, oldBmp);
                Win32.DeleteObject(hBitmap);
                Win32.DeleteDC(memDC);
                Win32.ReleaseDC(IntPtr.Zero, screenDC);
            }
            Win32.EndPaint(hWnd, ref ps);
        }


        private void SwitchToWindow(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            if (Win32.GetWindowPlacement(hwnd, ref placement))
            {
                if (placement.showCmd == Constants.SW_SHOWMINIMIZED)
                {
                    Win32.ShowWindow(hwnd, Constants.SW_RESTORE); // Restore only if minimized
                }
            }
            Win32.BringWindowToTop(hwnd);
            Win32.SetForegroundWindow(hwnd);

            HidePopup();
        }
    }
}
