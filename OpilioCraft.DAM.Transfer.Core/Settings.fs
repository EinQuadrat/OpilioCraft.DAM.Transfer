namespace OpilioCraft.DAM.Transfer

open System
open System.IO

[<RequireQualifiedAccess>]
module Settings =
    // location of app specific data
    let AppDataLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DAM")

    // configuration file
    let ConfigFilename = Path.Combine(AppDataLocation, "config.json")
    let ProfilesCatalogueFilename = Path.Combine(AppDataLocation, "transfer.json")
