module OpilioCraft.DAM.Transfer.Runtime

open System
open System.IO
open System.Text.Json

open OpilioCraft.FSharp.Prelude

// runtime implementation
let runCheck () =
    try
        Console.WriteLine "Checking DAM configuration... "

        Console.WriteLine $". path = {Settings.ConfigFilename}"

        Console.Write ". load settings: "
        UserSettings.config () |> ignore
        Console.WriteLine "OK"

        Console.Write ". verify Location.Incoming: "
        UserSettings.config ()
        |> fun settings -> settings.Locations |> UserSettings.verifyLocation "Incoming"
        |> fun _ -> Console.WriteLine "OK"
        Console.WriteLine ()

        Console.WriteLine "Checking transfer profiles catalogue... "

        Console.WriteLine $". path = {Settings.ProfilesCatalogueFilename}"
        
        Console.Write ". load settings: "
        UserSettings.profilesCatalogue () |> ignore
        Console.WriteLine "OK"
        
        Ok ()
    with
    | :? IncompleteSetupException as exn ->
            Logging.emitError SetupPending $"missing user settings file: {exn.MissingFile}"
    | :? InvalidUserSettingsException as exn ->
            Logging.emitError (InvalidConfiguration exn.Message) $"invalid user settings: {exn.File}, {exn.ErrorMessage}"
    | exn ->
            Logging.emitError (InvalidConfiguration exn.Message) $"something is wrong with the setup: {exn.Message}"

let runSetup () =
    let exampleConfig : DAMConfig = 
        {
            Locations = Map [
                "Incoming", "Enter path here"
                "LightTable", "Enter path here"
                "Processed", "Enter path here"
                "TrashBin", "Enter path here"
            ]
            Dependencies = Map [
                "ContentStoreCmdlets", @"C:\Program Files\Common Files\ContentStoreFramework\OpilioCraft.ContentStore.Cmdlets.dll"
            ]
            ContentCategories = Map [
                "Image", [ ".arw"; ".jpg"; ".jpeg" ]
                "Movie", [ ".mov"; ".mp4"; ".mts" ]
            ]
        }

    let exampleSource =
        {
            Path = "Insert source path here"
            Selectors = [ "some file selector, e.g. *.jpg" ]
        }

    let exampleDriveProfile =
        {
            Device = DeviceType.Drive
            Sources = [ exampleSource ]
            Action = TransferAction.Copy
            Options = Set.ofList [ TransferOption.Silent ]
        }

    let examplePortableProfile =
        {
            Device = DeviceType.Portable "PhoneName"
            Sources = [ exampleSource ]
            Action = TransferAction.Copy
            Options = Set.ofList [ TransferOption.Silent ]
        }

    try
        if not <| Directory.Exists(OpilioCraft.Settings.AppDataLocation)
        then
            Console.Write "Directory for user settings missing"
            Directory.CreateDirectory(OpilioCraft.Settings.AppDataLocation) |> ignore
            Console.WriteLine " --> created"

        if not <| File.Exists(Settings.ConfigFilename)
        then
            Console.WriteLine "DAM configuration file missing"
            let json = JsonSerializer.Serialize<DAMConfig>(exampleConfig, options = UserSettings.configJsonOptions)
            File.WriteAllText(Settings.ConfigFilename, json)
            Console.WriteLine "Example of DAM configuration saved. Please make sure to customize settings before using this app."
            Console.WriteLine $"\nFile is located here: {Settings.ConfigFilename}"
        else
            Console.Write "Checking DAM configuration... "
            UserSettings.config ()
            |> fun settings -> settings.Locations |> UserSettings.verifyLocation "Incoming"
            |> fun _ -> Console.WriteLine "OK"

        let profilesCatalogue : ProfilesCatalogue =
            Map.empty
            |> Map.add "Enter a name for the profile / drive example" exampleDriveProfile
            |> Map.add "Enter a name for the profile / portable example" examplePortableProfile

        if not <| File.Exists(Settings.ProfilesCatalogueFilename)
        then
            let json = JsonSerializer.Serialize<ProfilesCatalogue>(profilesCatalogue, options = UserSettings.profilesCatalogueJsonOptions)
            File.WriteAllText(Settings.ProfilesCatalogueFilename, json)
            Console.WriteLine "Example of profiles catalogue saved. Please make sure to customize settings before using this app."
            Console.WriteLine $"\nFile is located here: {Settings.ProfilesCatalogueFilename}"
        else
            Console.Write "Checking transfer profiles catalogue... "
            UserSettings.profilesCatalogue () |> ignore
            Console.WriteLine "OK"

        Ok ()
    with
    | exn -> Logging.emitError RuntimeError $"setup error: {exn.Message}"

let runTransfer transferProfile targetDir slowDown =
    try
        let transferWorker = TransferWorker<_>.CreateWorker transferProfile targetDir slowDown

        let eventCategory = if transferProfile.Options.Contains(TransferOption.Silent) then EventMonitor.errorsOnly else EventMonitor.allEvents
        transferWorker |> EventMonitor.subscribe eventCategory
        
        transferWorker.Run ()
        Ok ()
    with
    | exn -> Logging.emitError RuntimeError $"Error occurred: {exn.Message}"
