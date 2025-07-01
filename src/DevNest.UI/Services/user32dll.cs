using DevNest.UI;
using System;
using System.Runtime.InteropServices;

internal class User32Dll
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;

    private IntPtr _hWnd;
    private NotifyIconData _notifyIconData;
    private MainWindow? _mainWindow;
    private IntPtr _originalWndProc;
    private WndProcDelegate? _wndProcDelegate;
    private TrayMenuWindow? _trayMenuWindow;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(NotifyIconMessage dwMessage, ref NotifyIconData lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInstance, string lpName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    private const int GWL_WNDPROC = -4;

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public NotifyIconFlags uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [Flags]
    private enum NotifyIconFlags
    {
        Message = 0x01,
        Icon = 0x02,
        Tip = 0x04
    }

    private enum NotifyIconMessage
    {
        Add = 0x00,
        Modify = 0x01,
        Delete = 0x02
    }

    public void InitializeTrayIcon(MainWindow windows)
    {
        // Get the window handle for the main window
        _hWnd = WinRT.Interop.WindowNative.GetWindowHandle(windows);
        _mainWindow = windows;

        // ---------Load a custom icon from a file ---------
        IntPtr customIcon = LoadImage(IntPtr.Zero, @"your icon path it should be  .ico", IMAGE_ICON, 0, 0, LR_LOADFROMFILE);
        if (customIcon == IntPtr.Zero)
        {
            System.Diagnostics.Debug.WriteLine("Failed to load custom icon. Using default icon.");
            customIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512)); // Default application icon
        }

        // Populate the NotifyIconData structure
        _notifyIconData = new NotifyIconData
        {
            cbSize = Marshal.SizeOf(typeof(NotifyIconData)),
            hWnd = _hWnd,
            uID = 1,
            uFlags = NotifyIconFlags.Message | NotifyIconFlags.Icon | NotifyIconFlags.Tip,
            uCallbackMessage = WM_TRAYICON,
            hIcon = customIcon, // Use the custom icon
            szTip = "DevNest - Click to restore" // Tooltip text
        };

        // Add the icon to the system tray
        bool result = Shell_NotifyIcon(NotifyIconMessage.Add, ref _notifyIconData);
        if (!result)
        {
            System.Diagnostics.Debug.WriteLine("Failed to add tray icon.");
        }

        // Set up message handling for tray icon clicks
        SetupTrayIconMessageHandling();
    }

    public void RemoveTrayIcon()
    {
        // Restore original window procedure
        if (_originalWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_hWnd, GWL_WNDPROC, _originalWndProc);
        }

        // Remove the icon from the system tray
        Shell_NotifyIcon(NotifyIconMessage.Delete, ref _notifyIconData);
    }

    public void HideWindow()
    {
        if (_hWnd != IntPtr.Zero)
        {
            ShowWindow(_hWnd, SW_HIDE);
        }
    }

    public void ShowWindow()
    {
        if (_hWnd != IntPtr.Zero)
        {
            ShowWindow(_hWnd, SW_RESTORE);
            SetForegroundWindow(_hWnd);
        }
    }

    private void SetupTrayIconMessageHandling()
    {
        // Set up a custom window procedure to handle tray icon messages
        _wndProcDelegate = new WndProcDelegate(WndProc);
        _originalWndProc = GetWindowLongPtr(_hWnd, GWL_WNDPROC);
        SetWindowLongPtr(_hWnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            switch (lParam.ToInt32())
            {
                case WM_LBUTTONUP:
                    // Left click - restore window
                    if (_mainWindow != null)
                    {
                        _mainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            ShowWindow();
                            _mainWindow.Activate();
                        });
                    }
                    break;
                case WM_RBUTTONUP:
                    // Right click - show tray menu window
                    if (_mainWindow != null)
                    {
                        _mainWindow.DispatcherQueue.TryEnqueue(() =>
                        {
                            if (_trayMenuWindow == null)
                            {
                                _trayMenuWindow = new TrayMenuWindow();
                                PositionTrayMenuWindow(_trayMenuWindow);
                                _trayMenuWindow.Closed += (s, e) => _trayMenuWindow = null;
                                _trayMenuWindow.Activate();
                            }
                        });
                    }
                    break;
            }
        }
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    private void PositionTrayMenuWindow(TrayMenuWindow window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        int width = 480; // match XAML
        int height = 720; // match XAML
        int x = displayArea.WorkArea.Width + displayArea.WorkArea.X - width;
        int y = displayArea.WorkArea.Height + displayArea.WorkArea.Y - height;
        appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, width, height));
    }

    public void HandleTrayIconClick()
    {
        if (_mainWindow != null)
        {
            if (_mainWindow.Visible)
            {
                HideWindow();
            }
            else
            {
                ShowWindow();
                _mainWindow.Activate();
            }
        }
    }
}