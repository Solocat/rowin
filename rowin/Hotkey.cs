using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace rowin
{
    public partial class MainWindow
    {
        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource _source;
        private const int HOTKEY_ID = 9001;

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == HOTKEY_ID)
            {
                if (this.Visibility == Visibility.Visible) ToTray();
                else FromTray();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void HookHotkey()
        {
            _source = HwndSource.FromHwnd(Handle);
            _source.AddHook(HwndHook);
            RegisterHotKey(Handle, HOTKEY_ID, 0x001, 0x20);
        }

        private void UnHookHotkey()
        {
            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey(Handle, HOTKEY_ID);
        }
    }
}
