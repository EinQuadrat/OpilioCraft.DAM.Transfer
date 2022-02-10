module OpilioCraft.DAM.Transfer.App.Program

open System

open Elmish.WPF

open OpilioCraft.FSharp.Prelude
open OpilioCraft.DAM.Transfer
open OpilioCraft.DAM.Transfer.App.Resources

[<EntryPoint; STAThread>]
let main _ =
    try
        UserSettings.verifySettings () // throws exception on error

        WpfProgram.mkProgram App.init App.update App.bindings
        |> WpfProgram.runWindow (new MainWindow())

    with
    | :? IncompleteSetupException as exn ->
        sprintf "%s\n\n%s: %s" (I18N.["IncompleteSetup"]) (I18N.["MissingFile"]) exn.MissingFile
        |> Shared.showError; 1

    | :? InvalidUserSettingsException as exn ->
        sprintf "%s\n\n%s\n%s" I18N.["CorruptSetup"] exn.File exn.ErrorMessage
        |> Shared.showError; 1

    | exn ->
        Shared.errorMessageTemplate exn.Message
        |> Shared.showError; 1
