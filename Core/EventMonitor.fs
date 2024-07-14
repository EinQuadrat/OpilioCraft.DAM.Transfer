module OpilioCraft.DAM.Transfer.EventMonitor

open System

let allEvents = function
    | TransferInitiated -> Console.WriteLine "Transfer initiated"
    | ScanCompleted numberOfFiles -> Console.WriteLine $"{numberOfFiles} files found"

    | ProgressUpdate details ->
        match details with
        | Transferring filename -> Console.Write $"Transferring {filename}... "
        | Transferred _         -> Console.WriteLine "OK"
        | Skipped filename     -> Console.WriteLine $"Skipped {filename}"
    
    | TransferCompleted -> Console.WriteLine "Transfer completed"
    | TransferError exn -> Console.WriteLine $"Transfer error: {exn.Message}"

let errorsOnly = function
    | TransferError exn -> Console.WriteLine $"Transfer error: {exn.Message}"
    | _ -> ignore ()
        
let subscribe processor (transferWorker : ITransferWorker) = transferWorker.WorkerEvent |> Event.add processor
