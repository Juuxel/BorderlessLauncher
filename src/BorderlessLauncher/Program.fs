// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

module BorderlessLauncher.Main

open Argu
open BorderlessLauncher.Window

type Arguments =
    | [<MainCommand; ExactlyOnce; Last>] Process of args: string list
    | [<Unique; AltCommandLine("-t")>] Timeout of int
    | [<AltCommandLine("-a")>] Keep_Aspect_Ratio
    | [<AltCommandLine("-d")>] Dodge_Taskbar
    | [<AltCommandLine("-b")>] Letterboxing
    | [<AltCommandLine("-c")>] Attach
    | [<AltCommandLine("-o")>] Always_On_Top

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Process _ -> "the process and its arguments"
            | Timeout _ -> "the extra time to wait after the main window has been created (in milliseconds)"
            | Keep_Aspect_Ratio -> "retain the aspect ratio of the original window"
            | Dodge_Taskbar -> "if the process cannot be fullscreened, also avoids placing it on top of the taskbar (in conjunction with -a)"
            | Letterboxing -> "if the process cannot be fullscreened, create a black bar window behind it (in conjunction with -a)"
            | Attach -> "attach onto an existing process; the process arguments will be ignored"
            | Always_On_Top -> "keep the game window (and black bar window) always on top of other apps; hides the taskbar"

type Exiter() =
    interface IExiter with
        member _.Name = "Exiter"
        member _.Exit(msg, errorCode) =
            eprintfn "%s" msg
            showErrorMessageBox msg None
            exit (int errorCode)

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "borderlesslauncher.exe", errorHandler = Exiter())
    let parseResults = parser.ParseCommandLine argv
    let args = parseResults.GetResult Process
    let timeout = parseResults.TryGetResult Timeout
    let options: Launcher.WindowOptions =
        { KeepAspectRatio = parseResults.Contains Keep_Aspect_Ratio
          DodgeTaskbar = parseResults.Contains Dodge_Taskbar
          Letterboxing = parseResults.Contains Letterboxing
          Attach = parseResults.Contains Attach
          AlwaysOnTop = parseResults.Contains Always_On_Top }
    Launcher.launch (List.head args) (List.tail args) timeout options
    0
