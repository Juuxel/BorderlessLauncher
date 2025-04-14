// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

module BorderlessLauncher.Launcher

open BorderlessLauncher.Window
open System.Diagnostics
open System.Runtime.InteropServices

type Size =
    { Width: int
      Height: int }
    member self.CrossMultiply other =
        self.Width * other.Height - self.Height * other.Width

    member self.HasSameAspectRatio other =
        self.CrossMultiply other = 0

[<Struct>]
[<StructLayout(LayoutKind.Sequential)>]
type Rect =
    { mutable Left: int
      mutable Top: int
      mutable Right: int
      mutable Bottom: int }
    static member Zero() = { Left = 0; Top = 0; Right = 0; Bottom = 0 }
    member self.Width = self.Right - self.Left
    member self.Height = self.Bottom - self.Top
    member self.Size = { Width = self.Width; Height = self.Height }

module private Native =
    [<System.Flags>]
    type SwpFlags =
        | SWP_FRAMECHANGED = 0x0020u
        | SWP_NOOWNERZORDER = 0x0200u
        | SWP_NOZORDER = 0x0004u

    let SWP_DEFAULT = SwpFlags.SWP_FRAMECHANGED ||| SwpFlags.SWP_NOOWNERZORDER ||| SwpFlags.SWP_NOZORDER

    [<System.Flags>]
    type WsFlags =
        | WS_VISIBLE = 0x10000000L
        | WS_CLIPCHILDREN = 0x02000000L

    let WS_DEFAULT = WsFlags.WS_VISIBLE ||| WsFlags.WS_CLIPCHILDREN

    // Native types
    type HWND = nativeint
    type HMONITOR = nativeint
    type DWORD = uint

    [<Struct>]
    [<StructLayout(LayoutKind.Sequential)>]
    type MonitorInfo =
        { mutable CbSize: DWORD
          mutable Monitor: Rect
          mutable WorkArea: Rect
          mutable Flags: DWORD }
        static member Default() =
            { CbSize = uint sizeof<MonitorInfo>
              Monitor = Rect.Zero()
              WorkArea = Rect.Zero()
              Flags = 0u }

    // Native constants
    let HWND_TOPMOST: HWND = -1
    let MONITOR_DEFAULTTOPRIMARY: DWORD = 1u
    let GWL_STYLE = -16

    [<DllImport("user32.dll")>]
    extern HMONITOR MonitorFromWindow(HWND hWnd, DWORD dwFlags)

    [<DllImport("user32.dll")>]
    extern bool GetMonitorInfoW(HMONITOR hMonitor, [<Out>] MonitorInfo& lpmi)

    [<DllImport("user32.dll")>]
    extern bool GetWindowRect(HWND hWnd, [<Out>] Rect& rect)

    [<DllImport("user32.dll")>]
    extern bool SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags)

    [<DllImport("user32.dll")>]
    extern int64 SetWindowLongPtrW(HWND hWnd, int nIndex, int64 dwNewLong)

let private getWindowRect handle =
    let mutable rect = Rect.Zero()
    let success = Native.GetWindowRect(handle, &rect)
#if DEBUG
    printfn "Window rect %A, success %b" rect success
#endif
    rect

let private getMonitorInfo window =
    let monitor = Native.MonitorFromWindow(window, Native.MONITOR_DEFAULTTOPRIMARY)
    let mutable monitorInfo = Native.MonitorInfo.Default()
    let success = Native.GetMonitorInfoW(monitor, &monitorInfo)
#if DEBUG
    printfn "Monitor %d, monitor info %A, success %b" monitor monitorInfo success
#endif
    monitorInfo

let launch (processName: string) (args: string list) (timeout: int option) (keepAspectRatio: bool) (dodgeTaskbar: bool) (blackBars: bool) =
    use proc = Process.Start(processName, args)
    proc.WaitForInputIdle() |> ignore
    if timeout.IsSome then
        System.Threading.Thread.Sleep timeout.Value

    let handle = proc.MainWindowHandle
    let monitorInfo = getMonitorInfo handle
    let monitorRect = monitorInfo.Monitor
    let monitorSize = monitorRect.Size
    let mutable viewportRect = monitorRect
    let targetSize =
        if keepAspectRatio then
            let windowSize = getWindowRect handle |> _.Size
            if windowSize.HasSameAspectRatio monitorSize then
                monitorSize
            else
                if dodgeTaskbar then
                    viewportRect <- monitorInfo.WorkArea
                let c = windowSize.CrossMultiply viewportRect.Size
                if c = 0 then
                    viewportRect.Size
                else if c > 0 then
                    // Window is wider
                    let width = viewportRect.Width
                    let height = float windowSize.Height / float windowSize.Width * float width |> round |> int
                    { Width = width; Height = height }
                else
                    // Window is narrower
                    let height = viewportRect.Height
                    let width = float windowSize.Width / float windowSize.Height * float height |> round |> int
                    { Width = width; Height = height }
        else
            monitorSize
    let targetX = monitorRect.Left + (viewportRect.Width - targetSize.Width) / 2
    let targetY = monitorRect.Top + (viewportRect.Height - targetSize.Height) / 2
#if DEBUG
    printfn "Target pos (%d, %d), size: %A" targetX targetY targetSize
#endif

    if keepAspectRatio && blackBars && targetSize <> monitorSize then
        let bbWindow = BlackBarWindow(
            owner = typeof<Size>.Module,
            title = "Letterboxing",
            x = viewportRect.Left,
            y = viewportRect.Right,
            width = monitorSize.Width,
            height = monitorSize.Height
        )
        let bbHandle = bbWindow.StartOffThread()
        if bbHandle.HasValue then
            Native.SetWindowPos(
                bbHandle.Value,
                Native.HWND_TOPMOST,
                viewportRect.Left,
                viewportRect.Top,
                viewportRect.Width,
                viewportRect.Height,
                uint Native.SWP_DEFAULT)
                |> ignore
            Native.SetWindowLongPtrW(bbHandle.Value, Native.GWL_STYLE, int64 Native.WS_DEFAULT) |> ignore

    Native.SetWindowPos(handle, Native.HWND_TOPMOST, targetX, targetY, targetSize.Width, targetSize.Height, uint Native.SWP_DEFAULT) |> ignore
    Native.SetWindowLongPtrW(handle, Native.GWL_STYLE, int64 Native.WS_DEFAULT) |> ignore
