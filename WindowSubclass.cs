using System;
using System.Runtime.InteropServices;

namespace DevNest
{
    public class WindowSubclass
    {
        public delegate IntPtr SubclassProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("comctl32.dll")]
        public static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll")]
        public static extern IntPtr DefSubclassProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        private SubclassProc? _subclassProc;

        public void InstallSubclass(IntPtr hwnd, Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> callback)
        {
            _subclassProc = (h, m, w, l) => callback(h, m, w, l);
            SetWindowSubclass(hwnd, _subclassProc, 1, IntPtr.Zero);
        }
    }
}
