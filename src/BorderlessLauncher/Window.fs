// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

module BorderlessLauncher.Window2

open Windows.Win32
open Windows.Win32.Foundation
open Windows.Win32.Graphics.Gdi
open Windows.Win32.UI.WindowsAndMessaging

let private borderlessStyle = WINDOW_STYLE.WS_VISIBLE ||| WINDOW_STYLE.WS_CLIPCHILDREN

type Rect =
    { Left: int
      Top: int
      Right: int
      Bottom: int }
module Rect =
    let ofNative (rect: RECT) =
        { Left = rect.left
          Top = rect.top
          Right = rect.right
          Bottom = rect.bottom }

    let ofWindow (window: nativeint) =
        let mutable rect = RECT()
        PInvoke.GetWindowRect(HWND window, &rect) |> ignore
        ofNative rect

type MonitorInfo =
    { Monitor: Rect
      WorkArea: Rect }
module MonitorInfo =
    let ofNative (info: MONITORINFO) =
        { Monitor = Rect.ofNative info.rcMonitor
          WorkArea = Rect.ofNative info.rcWork }

    let get (window: nativeint) =
        let monitor = PInvoke.MonitorFromWindow(HWND window, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTOPRIMARY)
        let mutable monitorInfo = MONITORINFO()
        monitorInfo.cbSize <- uint sizeof<MONITORINFO>
        PInvoke.GetMonitorInfo(monitor, &monitorInfo) |> ignore
        ofNative monitorInfo

let showErrorMessageBox (text: string) (title: string option) =
    let style = MESSAGEBOX_STYLE.MB_ICONERROR ||| MESSAGEBOX_STYLE.MB_OK
    PInvoke.MessageBox(HWND.Null, text, Option.toObj title, style) |> ignore

let setBorderless (window: nativeint) (nextWindow: nativeint option) x y width height =
    let hWnd = HWND window
    let hInsertAfter =
        match nextWindow with
        | Some handle -> HWND handle
        | None -> HWND.HWND_TOP
    PInvoke.SetWindowPos(hWnd, hInsertAfter, x, y, width, height, SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED) |> ignore
    PInvoke.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, nativeint borderlessStyle) |> ignore

let setForegroundWindow (window: nativeint) =
    PInvoke.SetForegroundWindow (HWND window) |> ignore
