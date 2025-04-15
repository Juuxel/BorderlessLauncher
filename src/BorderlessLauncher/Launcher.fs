// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

module BorderlessLauncher.Launcher

open BorderlessLauncher.Window
open System.Diagnostics

type Size =
    { Width: int
      Height: int }
    member self.CrossMultiply other =
        self.Width * other.Height - self.Height * other.Width

    member self.HasSameAspectRatio other =
        self.CrossMultiply other = 0

type Rect with
    member rect.Width = rect.Right - rect.Left

    member rect.Height = rect.Bottom - rect.Top

    member rect.Size =
        { Width = rect.Width
          Height = rect.Height }

let createOrAttach (processName: string) (args: string list) attach =
    if attach then
        let processName = System.IO.Path.GetFileNameWithoutExtension processName
        let processes = Process.GetProcessesByName processName
        match Array.tryHead processes with
        | Some proc -> proc
        | None ->
            let message = $"Could not find a process with name {processName}."
            showErrorMessageBox message None
            failwith message
    else
        let startInfo = new ProcessStartInfo(processName, args)
        startInfo.WorkingDirectory <- System.Environment.CurrentDirectory
        Process.Start startInfo |> nonNull

let launch (processName: string) (args: string list) (timeout: int option) (keepAspectRatio: bool) (dodgeTaskbar: bool) (blackBars: bool) (attach: bool) =
    use proc = createOrAttach processName args attach
    proc.WaitForInputIdle() |> ignore
    if timeout.IsSome then
        System.Threading.Thread.Sleep timeout.Value

    let handle = proc.MainWindowHandle
    let monitorInfo = MonitorInfo.get handle
    let monitorRect = monitorInfo.Monitor
    let monitorSize = monitorRect.Size
    let mutable viewportRect = monitorRect
    let targetSize =
        if keepAspectRatio then
            let windowSize = Rect.ofWindow handle |> _.Size
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

    let mutable bbWindowOpt: BlackBarWindow option = None

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
        if bbHandle.IsSome then
            bbWindowOpt <- Some bbWindow
            setBorderless
                bbHandle.Value
                None
                viewportRect.Left
                viewportRect.Top
                viewportRect.Width
                viewportRect.Height
            bbWindow.GainedFocusEvent.Add (fun _ -> setForegroundWindow handle)

    setBorderless handle None targetX targetY targetSize.Width targetSize.Height
    setForegroundWindow handle

    if bbWindowOpt.IsSome then
        proc.WaitForExit()
        bbWindowOpt.Value.Destroy()
