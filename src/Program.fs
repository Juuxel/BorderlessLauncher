// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

module BorderlessLauncher.Main

open Argu

type Arguments =
    | [<MainCommand; ExactlyOnce; Last>] Process of args: string list
    | [<Unique; AltCommandLine("-t")>] Timeout of int
    | [<AltCommandLine("-a")>] Keep_Aspect_Ratio
    | [<AltCommandLine("-d")>] Dodge_Taskbar
    | [<AltCommandLine("-b")>] Letterboxing

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Process _ -> "the process and its arguments"
            | Timeout _ -> "the extra time to wait after the main window has been created (in milliseconds)"
            | Keep_Aspect_Ratio -> "retain the aspect ratio of the original window"
            | Dodge_Taskbar -> "if the process cannot be fullscreened, also avoids placing it on top of the taskbar (in conjunction with -a)"
            | Letterboxing -> "if the process cannot be fullscreened, create a black bar window behind it (in conjunction with -a)"

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "borderlesslauncher.exe", errorHandler = ProcessExiter())
    let parseResults = parser.ParseCommandLine argv
    let args = parseResults.GetResult Process
    let timeout = parseResults.TryGetResult Timeout
    let keepAspectRatio = parseResults.Contains Keep_Aspect_Ratio
    let dodgeTaskbar = parseResults.Contains Dodge_Taskbar
    let blackBars = parseResults.Contains Letterboxing
    Launcher.launch (List.head args) (List.tail args) timeout keepAspectRatio dodgeTaskbar blackBars
    0
