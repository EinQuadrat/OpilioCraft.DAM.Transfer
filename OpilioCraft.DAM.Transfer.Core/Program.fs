module OpilioCraft.DAM.Transfer.Core

open System

open Argu
open OpilioCraft.FSharp.Prelude
open OpilioCraft.DAM.Transfer.Runtime

// commandline definition
type CheckArgs =
    | [<NoCommandLine>] Dummy

    interface IArgParserTemplate with
        member x.Usage = ""

type SetupArgs =
    | [<NoCommandLine>] Dummy

    interface IArgParserTemplate with
        member x.Usage = ""

type RunArgs =
    | [<MainCommand; ExactlyOnce; Last>] Profile of ProfileName:string
    | Target_Path of Path:string
    | Slow_Down
    
    interface IArgParserTemplate with
        member x.Usage =
            match x with
            | Profile _ -> "name of transfer profile to be used"
            | Target_Path _ -> "overwrite configured transfer target"
            | Slow_Down _ -> "slow down execution for testing purposes"

[<NoAppSettings>]
type Arguments =
    | [<CliPrefix(CliPrefix.None)>] Check of ParseResults<CheckArgs>
    | [<CliPrefix(CliPrefix.None)>] Setup of ParseResults<SetupArgs>
    | [<CliPrefix(CliPrefix.None)>] Run of ParseResults<RunArgs>

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Check _   -> "check (and show) current setup"
            | Setup _   -> "run setup and write example configuration"
            | Run _ -> "run transfer with specified profile"

let getExitCode result =
    match result with
    | Ok _ -> 0
    | Error reason ->
        match reason with
        | UsageError -> 1
        | SetupPending -> 2
        | InvalidConfiguration _ -> 3
        | UnknownProfile -> 4
        | InvalidTargetPath _ -> 5
        | RuntimeError -> 99


[<EntryPoint>]
let main args =
    let errorHandler = ProcessExiter(colorizer = function | ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<Arguments>(programName = "TransferApp", errorHandler = errorHandler)
    
    match parser.ParseCommandLine args with
    | p when p.Contains(Check) -> runCheck ()
    | p when p.Contains(Setup) -> runSetup ()

    | p when p.Contains(Run) ->
        try
            UserSettings.verifySettings () // throws Exception on error

            let runArgs = p.GetResult(Run)

            let config = UserSettings.config ()
            let profilesCatalogue = UserSettings.profilesCatalogue ()
            let profileName = runArgs.GetResult(Profile)

            let targetPath = runArgs.TryGetResult(Target_Path) |> Option.defaultValue config.Locations.["Incoming"]

            if System.IO.Directory.Exists(targetPath)
            then
                match profilesCatalogue |> Map.tryFind profileName with
                | Some profile -> runTransfer profile targetPath (runArgs.Contains(Slow_Down))
                | _ -> Error UnknownProfile
            else
                Error (InvalidTargetPath(targetPath))

        with
        | :? IncompleteSetupException as exn -> Error SetupPending
        | :? InvalidUserSettingsException as exn -> Error (InvalidConfiguration(exn.Message))
        | exn -> Console.Error.WriteLine $"runtime error: {exn.Message}"; Error RuntimeError
    
    | _ ->
        Console.WriteLine $"{parser.PrintUsage()}"
        Error UsageError
    |> getExitCode
