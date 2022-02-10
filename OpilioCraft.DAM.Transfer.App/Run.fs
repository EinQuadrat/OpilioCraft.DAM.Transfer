namespace OpilioCraft.DAM.Transfer.App

open Elmish
open OpilioCraft.DAM.Transfer
open OpilioCraft.DAM.Transfer.App.Resources

[<RequireQualifiedAccess>]
module Run =
    type State = {
        Activity : string
        NumberOfItems : int option
        ItemsCompleted : int
        Details  : string option
        TransferCompleted : bool
    }

    type Msg =
    | WorkerEvent of EventType:TransferWorkerEvent

    let init () = {
        Activity = I18N.["Initializing"]
        NumberOfItems = None
        ItemsCompleted = 0
        Details = None
        TransferCompleted = false
    }
    
    let update (runMsg : Msg) (runState : State) =
        match runMsg with
        | WorkerEvent eventType ->
            match eventType with
            | TransferInitiated ->
                { runState with
                    Activity = I18N.["TransferInitiated"]
                    NumberOfItems = None
                    ItemsCompleted = 0
                    Details = None
                }, Cmd.none

            | ScanCompleted numberOfFiles ->
                { runState with
                    Activity = I18N.["Transferring"]
                    NumberOfItems = Some (numberOfFiles + 1)
                    ItemsCompleted = 0
                    Details = None
                }, Cmd.none

            | ProgressUpdate details ->
                let updatedModel =
                    match details with
                    | Transferring file -> { runState with Details = Some file }
                    | Transferred file
                    | Skipped file -> { runState with Details = Some file; ItemsCompleted = runState.ItemsCompleted + 1 }

                updatedModel, Cmd.none

            | TransferCompleted ->
                { runState with
                    Activity = I18N.["TransferCompleted"]
                    NumberOfItems = Some 1
                    ItemsCompleted = 1
                    Details = None
                    TransferCompleted = true
                }, Cmd.none

            | TransferError exn ->
                Shared.showError exn.Message
                Shared.closeApp ()
                runState, Cmd.none
