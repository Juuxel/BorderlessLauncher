// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

module BorderlessLauncher.Window

open System.Threading
open Windows.Win32
open Windows.Win32.Foundation
open Windows.Win32.Graphics.Gdi
open Windows.Win32.UI.WindowsAndMessaging

let borderlessStyle = WINDOW_STYLE.WS_VISIBLE ||| WINDOW_STYLE.WS_CLIPCHILDREN

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

let inline private useCharPtr str ([<InlineIfLambda>] f: nativeptr<char> -> 'a) =
    Window.WindowUtil.WithCharPtr(str, f)

type BlackBarWindow(owner, title, x, y, width, height) =
    static let className = "BorderlessLauncher.Window.BlackBarWindow"

    static let registerClass hInstance windowProcedure =
        let inner classNamePtr =
            let mutable windowClass = WNDCLASSW()
            windowClass.lpszClassName <- PCWSTR classNamePtr
            windowClass.hInstance <- hInstance
            windowClass.lpfnWndProc <- windowProcedure
            PInvoke.RegisterClass &windowClass |> ignore
        useCharPtr className inner

    static let createWindow title style x y width height hInstance =
        let inner classNamePtr titlePtr =
            PInvoke.CreateWindowEx(
                LanguagePrimitives.EnumOfValue 0u,
                PCWSTR classNamePtr,
                PCWSTR titlePtr,
                style,
                x,
                y,
                width,
                height,
                HWND.Null,
                HMENU.Null,
                hInstance
            )
        inner
        |> useCharPtr className
        |> useCharPtr title

    let owner = owner
    let title = title
    let x = x
    let y = y
    let width = width
    let height = height
    let gainedFocusEvent = new Event<unit>()

    let paint hWnd =
        let mutable ps = PAINTSTRUCT()
        let hdc = PInvoke.BeginPaint(hWnd, &ps)
        let brush = PInvoke.CreateSolidBrush(COLORREF 0x00000000u)
        use rcPaint = fixed &ps.rcPaint
        PInvoke.FillRect(hdc, rcPaint, brush) |> ignore
        PInvoke.EndPaint(hWnd, &ps) |> ignore

    let messageLoop() =
        let mutable msg = MSG()
        while PInvoke.GetMessage(&msg, HWND.Null, 0u, 0u).Value <> 0 do
            PInvoke.TranslateMessage(&msg) |> ignore
            PInvoke.DispatchMessage(&msg) |> ignore

    let windowProcedure hWnd msg wParam lParam =
        match msg with
        | PInvoke.WM_PAINT ->
            paint hWnd
            LRESULT 0
        | PInvoke.WM_CLOSE ->
            PInvoke.DestroyWindow hWnd |> ignore
            LRESULT 0
        | PInvoke.WM_DESTROY ->
            PInvoke.PostQuitMessage 0
            LRESULT 0
        | PInvoke.WM_ACTIVATE ->
            gainedFocusEvent.Trigger()
            LRESULT 0
        | _ -> PInvoke.DefWindowProc(hWnd, msg, wParam, lParam)

    let start() =
        let hInstance = HINSTANCE (System.Runtime.InteropServices.Marshal.GetHINSTANCE owner)
        registerClass hInstance windowProcedure
        let hWnd = createWindow title borderlessStyle x y width height hInstance
        if hWnd = HWND.Null then
            None
        else
            PInvoke.ShowWindow(hWnd, SHOW_WINDOW_CMD.SW_NORMAL) |> ignore
            Some hWnd

    member val private windowHandle: HWND option = None with get, set

    [<CLIEvent>]
    member _.GainedFocusEvent = gainedFocusEvent.Publish

    member this.StartOffThread() =
        let evt = new AutoResetEvent false
        let thread = Thread (fun () ->
            this.windowHandle <- start()
            evt.Set() |> ignore
            if this.windowHandle.IsSome then
                messageLoop())
        thread.Start()
        evt.WaitOne() |> ignore
        this.windowHandle |> Option.map nativeint

    member this.Destroy() =
        PInvoke.PostMessage(this.windowHandle.Value, PInvoke.WM_CLOSE, WPARAM 0un, LPARAM 0n) |> ignore
