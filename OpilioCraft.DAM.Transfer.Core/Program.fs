module OpilioCraft.DAM.Transfer.Core

open System

open Argu
open Runtime

open OpilioCraft.FSharp.Prelude

// commandline definition
type Arguments =
    | [<AltCommandLine("-c")>] Check
    | [<AltCommandLine("-s")>] Setup
    | Run of ProfileName:string
    | TargetPath of Path:string
    | SlowDown

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Check _   -> "check (and show) current setup"
            | Setup _   -> "run setup and write example configuration"
            | Run _ -> "run transfer with specified profile"
            | TargetPath _ -> "overwrite configured transfer target"
            | SlowDown _ -> "slow down execution for testing purposes"

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

            let config = UserSettings.config ()
            let profilesCatalogue = UserSettings.profilesCatalogue ()
            let profileName = p.GetResult(Run)

            let targetPath = if p.Contains(TargetPath) then p.GetResult(TargetPath) else config.Locations.["Incoming"]

            if System.IO.Directory.Exists(targetPath)
            then
                match (profileName, profilesCatalogue) ||> Map.tryFind with
                | Some profile -> runTransfer profile targetPath
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
