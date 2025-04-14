// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace BorderlessLauncher.Window;

public static class BorderlessWindows
{
    internal const WINDOW_STYLE BorderlessStyle = WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CLIPCHILDREN;

    public static MonitorInfo GetMonitorInfo(IntPtr window)
    {
        unsafe
        {
            var monitor = PInvoke.MonitorFromWindow((HWND) window, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY);
            MONITORINFO monitorInfo = new()
            {
                cbSize = (uint) sizeof(MONITORINFO)
            };
            PInvoke.GetMonitorInfo(monitor, &monitorInfo);
            return new MonitorInfo(new Rect(monitorInfo.rcMonitor), new Rect(monitorInfo.rcWork));
        }
    }

    public static void SetBorderless(IntPtr window, IntPtr? nextWindow, int x, int y, int width, int height)
    {
        HWND hWnd = (HWND) window;
        HWND hInsertAfter = nextWindow.HasValue ? (HWND) nextWindow.Value : HWND.HWND_TOPMOST;
        PInvoke.SetWindowPos(hWnd, hInsertAfter, x, y, width, height, SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
        PInvoke.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (int) BorderlessStyle);
    }

    public static Rect GetWindowRect(IntPtr window)
    {
        unsafe
        {
            RECT rect = new();
            PInvoke.GetWindowRect((HWND) window, &rect);
            return new Rect(rect);
        }
    }

    public record MonitorInfo(Rect Monitor, Rect WorkArea);
}
