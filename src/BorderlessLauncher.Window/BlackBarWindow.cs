// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace BorderlessLauncher.Window;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

public sealed class BlackBarWindow(System.Reflection.Module owner, string title, int x, int y, int width, int height)
{
    private const string ClassName = "BorderlessLauncher.Window.BlackBarWindow";
    private readonly System.Reflection.Module owner = owner;
    private readonly string title = title;
    private readonly int x = x;
    private readonly int y = y;
    private readonly int width = width;
    private readonly int height = height;

    private IntPtr? windowHandle = null;

    public IntPtr? StartOffThread()
    {
        var evt = new AutoResetEvent(false);
        Thread thread = new(() =>
        {
            windowHandle = Start();
            evt.Set();
            if (windowHandle != null) MessageLoop();
        });
        thread.Start();
        evt.WaitOne();
        return windowHandle;
    }

    public void Destroy()
    {
        PInvoke.PostMessage((HWND) windowHandle!, PInvoke.WM_CLOSE, (WPARAM) 0u, (LPARAM) 0);
    }

    private IntPtr? Start()
    {
        var hInstance = new HINSTANCE(System.Runtime.InteropServices.Marshal.GetHINSTANCE(owner));
        RegisterClass(hInstance, new WNDPROC(WindowProcedure));
        var hWnd = CreateWindow(title, BorderlessWindows.BorderlessStyle, x, y, width, height, hInstance);
        if (hWnd == HWND.Null) return null;
        PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_NORMAL);
        return hWnd;
    }

    private void MessageLoop()
    {
        unsafe
        {
            MSG msg = new();
            while (PInvoke.GetMessage(&msg, HWND.Null, 0, 0))
            {
                PInvoke.TranslateMessage(&msg);
                PInvoke.DispatchMessage(&msg);
            }
        }
    }

    private LRESULT WindowProcedure(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_PAINT:
                Paint(hWnd);
                return new LRESULT(0);
            case PInvoke.WM_CLOSE:
                PInvoke.DestroyWindow(hWnd);
                return new LRESULT(0);
            case PInvoke.WM_DESTROY:
                PInvoke.PostQuitMessage(0);
                return new LRESULT(0);
            default:
                return PInvoke.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }

    private void Paint(HWND hWnd)
    {
        unsafe
        {
            var ps = new PAINTSTRUCT();
            var hdc = PInvoke.BeginPaint(hWnd, &ps);
            var brush = PInvoke.CreateSolidBrush(new COLORREF(0x00000000));
            PInvoke.FillRect(hdc, &ps.rcPaint, brush);
            PInvoke.EndPaint(hWnd, &ps);
        }
    }

    private static void RegisterClass(HINSTANCE hInstance, WNDPROC windowProcedure)
    {
        unsafe
        {
            fixed (char* classNamePtr = ClassName)
            {
                WNDCLASSW windowClass = new()
                {
                    lpszClassName = classNamePtr,
                    hInstance = hInstance,
                    lpfnWndProc = windowProcedure
                };
                PInvoke.RegisterClass(windowClass);
            }
        }
    }

    private static HWND CreateWindow(string title, WINDOW_STYLE style, int x, int y, int width, int height, HINSTANCE hInstance)
    {
        unsafe
        {
            fixed (char* classNamePtr = ClassName, titlePtr = title)
            {
                return PInvoke.CreateWindowEx(0, new PCWSTR(classNamePtr), new PCWSTR(titlePtr), style, x, y, width, height, HWND.Null, HMENU.Null, hInstance);
            }
        }
    }
}
