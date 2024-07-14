namespace OpilioCraft.DAM.Transfer

open System.IO

[<RequireQualifiedAccess>]
module Settings =
    // configuration file
    let ConfigFilename = Path.Combine(OpilioCraft.Settings.AppDataLocation, "DAM.json")
    let ProfilesCatalogueFilename = Path.Combine(OpilioCraft.Settings.AppDataLocation, "DAM.Transfer.json")