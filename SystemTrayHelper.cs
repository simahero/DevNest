using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DevNest
{
    public class SystemTrayHelper
    {
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xF020;
        private const int WM_APP = 0x8000;
        private const int WM_TRAYICON = WM_APP + 1;

        // Shell_NotifyIcon messages
        private const int NIM_ADD = 0x00000000;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIM_DELETE = 0x00000002;

        // Icon flags
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_TIP = 0x00000004;

        // Mouse messages
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;

        [StructLayout(LayoutKind.Sequential)]
        public struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
        }

        [DllImport("shell32.dll")]
        public static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData); [DllImport("user32.dll")]
        public static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadImage(IntPtr hInst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconFromResource(byte[] iconData, uint cbIconData, bool fIcon, uint dwVersion); private NOTIFYICONDATA _notifyIconData;
        private IntPtr _windowHandle;
        private MainWindow _mainWindow;
        private IntPtr _customIcon = IntPtr.Zero;

        public SystemTrayHelper(MainWindow mainWindow, IntPtr windowHandle)
        {
            _mainWindow = mainWindow;
            _windowHandle = windowHandle;
            InitializeTrayIcon();
        }

        private void InitializeTrayIcon()
        {
            _notifyIconData = new NOTIFYICONDATA();
            _notifyIconData.cbSize = Marshal.SizeOf(_notifyIconData);
            _notifyIconData.hWnd = _windowHandle;
            _notifyIconData.uID = 1;
            _notifyIconData.uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP;
            _notifyIconData.uCallbackMessage = WM_TRAYICON;
            // Load custom icon from Assets folder
            _customIcon = LoadCustomIcon();
            _notifyIconData.hIcon = _customIcon;
            _notifyIconData.szTip = "DevNest - Development Environment";

            Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
        }

        public void RemoveTrayIcon()
        {
            Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);

            // Clean up custom icon if we created one
            if (_customIcon != IntPtr.Zero)
            {
                DestroyIcon(_customIcon);
                _customIcon = IntPtr.Zero;
            }
        }
        private IntPtr LoadCustomIcon()
        {
            try
            {
                // First, try to load a nice icon from shell32.dll
                IntPtr hIcon = ExtractIcon(IntPtr.Zero, "shell32.dll", 2);
                if (hIcon != IntPtr.Zero && hIcon != new IntPtr(1)) // ExtractIcon returns 1 if no icon found
                {
                    return hIcon;
                }

                // Try another nice icon from shell32.dll (folder icon)
                hIcon = ExtractIcon(IntPtr.Zero, "shell32.dll", 3);
                if (hIcon != IntPtr.Zero && hIcon != new IntPtr(1))
                {
                    return hIcon;
                }

                // Try the application icon (index 0)
                hIcon = ExtractIcon(IntPtr.Zero, "shell32.dll", 0);
                if (hIcon != IntPtr.Zero && hIcon != new IntPtr(1))
                {
                    return hIcon;
                }

                // Final fallback to default application icon
                return LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION
            }
            catch
            {
                // If anything goes wrong, use default icon
                return LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION
            }
        }        // Additional Win32 API functions for icon extraction
        [DllImport("shell32.dll")]
        public static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr ExtractAssociatedIcon(IntPtr hInst, string lpIconPath, ref int lpiIcon);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string lpFileName);

        public bool ProcessTrayIconMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_TRAYICON)
            {
                switch (lParam.ToInt32())
                {
                    case WM_LBUTTONUP:
                        _mainWindow.ShowMainWindow();
                        return true;
                    case WM_RBUTTONUP:
                        ShowContextMenu();
                        return true;
                }
            }
            return false;
        }
        private void ShowContextMenu()
        {
            // Get cursor position
            GetCursorPos(out POINT cursorPos);

            // Create Win32 popup menu
            IntPtr hMenu = CreatePopupMenu();

            // Add menu items
            AppendMenu(hMenu, MF_STRING, 1000, "Show Window");
            AppendMenu(hMenu, MF_SEPARATOR, 0, null);

            AppendMenu(hMenu, MF_STRING, 2000, "Sites");
            AppendMenu(hMenu, MF_STRING, 2100, "  Add Blank");
            AppendMenu(hMenu, MF_STRING, 2101, "  Add WordPress");
            AppendMenu(hMenu, MF_STRING, 2102, "  Add Laravel");
            AppendMenu(hMenu, MF_STRING, 2103, "  Add Symfony");

            var sites = _mainWindow.GetSites();
            if (sites.Count > 0)
            {
                AppendMenu(hMenu, MF_SEPARATOR, 0, null);
                for (int i = 0; i < sites.Count; i++)
                {
                    AppendMenu(hMenu, MF_STRING, (uint)(3000 + i), $"Open {sites[i]}");
                }
            }

            AppendMenu(hMenu, MF_SEPARATOR, 0, null);
            AppendMenu(hMenu, MF_STRING, 9000, "Exit");

            // Set foreground window (required for proper menu behavior)
            SetForegroundWindow(_windowHandle);

            // Show menu
            uint cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON,
                                    cursorPos.X, cursorPos.Y, 0, _windowHandle, IntPtr.Zero);

            // Handle menu selection
            //HandleMenuSelection(cmd, sites);

            // Clean up
            DestroyMenu(hMenu);
            PostMessage(_windowHandle, WM_NULL, IntPtr.Zero, IntPtr.Zero);
        }

        private void HandleMenuSelection(uint cmd, List<string> sites)
        {
            switch (cmd)
            {
                case 1000: // Show Window
                    _mainWindow.ShowMainWindow();
                    break;
                case 2100: // Add Blank
                    _mainWindow.AddSite("Blank");
                    break;
                case 2101: // Add WordPress
                    _mainWindow.AddSite("WordPress");
                    break;
                case 2102: // Add Laravel
                    _mainWindow.AddSite("Laravel");
                    break;
                case 2103: // Add Symfony
                    _mainWindow.AddSite("Symfony");
                    break;
                case 9000: // Exit
                    _mainWindow.ExitApplication();
                    break;
                default:
                    // Handle site opening (3000+)
                    if (cmd >= 3000 && cmd < 3000 + sites.Count)
                    {
                        int siteIndex = (int)(cmd - 3000);
                        _mainWindow.OpenSite(sites[siteIndex]);
                    }
                    break;
            }
        }

        // Additional Win32 API declarations for popup menus
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll")]
        public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

        [DllImport("user32.dll")]
        public static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        public static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint WM_NULL = 0x0000;
    }
}
