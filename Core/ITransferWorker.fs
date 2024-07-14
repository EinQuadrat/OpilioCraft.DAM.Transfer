namespace OpilioCraft.DAM.Transfer

type ProgressUpdateEventArgs =
    | Transferring of string
    | Transferred of string
    | Skipped of string

type TransferWorkerEvent =
    | TransferInitiated
    | ScanCompleted of NumberOfFilesFound:int
    | ProgressUpdate of Details:ProgressUpdateEventArgs
    | TransferCompleted
    | TransferError of System.Exception

type ITransferWorker =
    abstract member Run : unit -> unit
    abstract member WorkerEvent : IEvent<TransferWorkerEvent>
