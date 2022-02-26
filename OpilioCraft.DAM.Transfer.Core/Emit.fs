module OpilioCraft.DAM.Transfer.Emit

let emitError (reason : ErrorReason) (errorMessage : string) =
    System.Console.Error.WriteLine errorMessage
    Error reason
