namespace OpilioCraft.DAM.Transfer

open System
open System.IO
open Microsoft.VisualBasic.CompilerServices

open OpilioCraft.PortableDevices

[<AbstractClass>]
type TransferWorker<'ItemType> (transferProfile : TransferProfile, targetDir : string) =
    // event queues
    let workerEvent = new Event<_>()

    // .NET events
    [<CLIEvent>]
    member _.WorkerEvent = workerEvent.Publish

    // trigger methods
    member _.TriggerEvent eventType = workerEvent.Trigger(eventType)

    // properties
    member _.TransferProfile = transferProfile
    member _.TargetDir = targetDir

    member _.OverrideOption = transferProfile.Options.Contains TransferOption.Override

    // item type specific methods
    abstract member ScanSource : SourceDefinition -> 'ItemType list
    abstract member TransferFile : 'ItemType -> unit

    member x.TriggerProgressUpdate details = details |> ProgressUpdate |> x.TriggerEvent

    // factory method
    static member CreateWorker (transferProfile : TransferProfile) (targetDir : string) (slowDown : bool) : ITransferWorker =
        match transferProfile.Device with
        | Drive ->
            new DriveTransferWorker(transferProfile, targetDir, slowDown) :> ITransferWorker
        | Portable deviceName ->
            let portableDevice = PortableDeviceManager.GetDeviceByName(deviceName, refresh = true)
            new PortableTransferWorker(portableDevice, transferProfile, targetDir) :> ITransferWorker

    // interfaces
    interface ITransferWorker with
        member x.WorkerEvent = x.WorkerEvent

        member x.Run () = 
            try
                x.TriggerEvent TransferInitiated

                x.TransferProfile.Sources
                |> List.collect x.ScanSource
                |> (fun itemList -> x.TriggerEvent (ScanCompleted itemList.Length); itemList)
                |> List.iter x.TransferFile

                x.TriggerEvent TransferCompleted
            with
            | exn -> x.TriggerEvent (TransferError exn)


and DriveTransferWorker (transferProfile, targetDir, slowDown) =
    inherit TransferWorker<string>(transferProfile, targetDir)

    member private x.TransferAction =
        match transferProfile.Action with
        | Copy -> fun sourcePath targetPath -> File.Copy(sourcePath, targetPath, x.OverrideOption)
        | Move -> fun sourcePath targetPath -> File.Move(sourcePath, targetPath, x.OverrideOption)

    override x.ScanSource sourceDef =
        let rootFolder = Path.GetPathRoot(sourceDef.Path)
        let pathParts = Path.GetRelativePath(rootFolder, sourceDef.Path).Split('/', '\\') |> Array.toList

        let getMatchingSubFolders pattern parent =
            Directory.GetDirectories(parent, pattern)
            |> Array.toList
            |> function | [] -> raise <| new Exception($"source path not found: {sourceDef.Path}") | folderList -> folderList

        let rec treeWalker (parent : string) (pathParts : string list) : string list =
            match pathParts with
            | [] -> [ parent ]
            | part :: tail ->
                parent
                |> getMatchingSubFolders part
                |> List.collect (fun subfolder -> treeWalker subfolder tail)

        treeWalker rootFolder pathParts
        |> List.collect (fun folder -> sourceDef.Selectors |> List.collect (fun selector -> Directory.GetFiles(folder, selector) |> Array.toList))

    override x.TransferFile file =
        let fileName = Path.GetFileName(file)
        let targetPath = Path.Combine(x.TargetDir, fileName) in

        match (not <| File.Exists(targetPath)) || x.OverrideOption with
        | true ->
            x.TriggerProgressUpdate <| Transferring file
            x.TransferAction file targetPath
            x.TriggerProgressUpdate <| Transferred file
        | _ -> x.TriggerProgressUpdate <| Skipped fileName

        if slowDown then System.Threading.Thread.Sleep(500) // for testing purposes only

and PortableTransferWorker (portableDevice, transferProfile, targetDir) =
    inherit TransferWorker<PortableDevice.File> (transferProfile, targetDir)

    member _.PortableDevice = portableDevice

    member private x.TransferAction =
        match transferProfile.Action with
        | Copy -> fun file -> x.PortableDevice.DownloadFile(file, x.TargetDir)
        | Move -> fun file -> x.PortableDevice.MoveFile(file, x.TargetDir)

    override x.ScanSource sourceDef =
        let rootFolder = x.PortableDevice.GetRootFolder()
        let pathParts = sourceDef.Path.Split('/', '\\') |> Array.toList
        
        let like pattern text =
            LikeOperator.LikeString(text, pattern, Microsoft.VisualBasic.CompareMethod.Text)

        let getMatchingSubFolders (pattern : string) (parent : PortableDevice.Folder) : PortableDevice.Folder list =
            parent.GetSubFolders()
            |> Seq.choose (fun folder -> match folder.Name |> like pattern with | true -> Some folder | _ -> None)
            |> Seq.toList
            |> function | [] -> raise <| new Exception($"source path not found: {sourceDef.Path}") | folderList -> folderList

        let rec treeWalker (parent : PortableDevice.Folder) (pathParts : string list) : PortableDevice.Folder list =
            match pathParts with
            | [] -> [ parent ]
            | part :: tail ->
                parent
                |> getMatchingSubFolders part
                |> List.collect (fun subfolder -> treeWalker subfolder tail)

        treeWalker rootFolder pathParts
        |> List.collect (fun (folder : PortableDevice.Folder) ->
                sourceDef.Selectors
                |> List.collect (fun (selector : string) ->
                    folder.GetFiles()
                    |> Seq.choose (fun file -> match file.Name |> like selector with | true -> Some file | _ -> None)
                    |> Seq.toList
                    )
                )

    override x.TransferFile file =
        let targetPath = Path.Combine(x.TargetDir, file.Name) in

        match (not <| File.Exists(targetPath)) || x.OverrideOption with
        | true ->
            x.TriggerProgressUpdate <| Transferring file.Name
            x.TransferAction file
            x.TriggerProgressUpdate <| Transferred file.Name
        | _ -> x.TriggerProgressUpdate <| Skipped file.Name
