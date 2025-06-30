using DevNest.UI;
using System;
using System.Runtime.InteropServices;

internal class User32Dll
{
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1;

    private IntPtr _hWnd;
    private NotifyIconData _notifyIconData;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(NotifyIconMessage dwMessage, ref NotifyIconData lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInstance, string lpName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;

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
            szTip = "Tray Icon App" // Tooltip text
        };

        // Add the icon to the system tray
        bool result = Shell_NotifyIcon(NotifyIconMessage.Add, ref _notifyIconData);
        if (!result)
        {
            System.Diagnostics.Debug.WriteLine("Failed to add tray icon.");
        }
    }

    public void RemoveTrayIcon()
    {
        // Remove the icon from the system tray
        Shell_NotifyIcon(NotifyIconMessage.Delete, ref _notifyIconData);
    }
}