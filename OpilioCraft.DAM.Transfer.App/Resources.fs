module OpilioCraft.DAM.Transfer.App.Resources

open System.Threading

// I18N
type MessageCatalogue = Map<string,string>

let I18N =
    let currentCulture = Thread.CurrentThread.CurrentCulture

    match currentCulture.TwoLetterISOLanguageName with
    | "de" ->
        Map [
            "Error", "Fehler"
            "ErrorOccurred", "Es ist ein Fehler aufgetreten!"
            "IncompleteSetup", "Die Anwendung ist nicht vollständig konfiguriert."
            "MissingFile", "Datei fehlt"
            "CorruptSetup", "Die Anwendungskonfiguration ist fehlerhaft."

            "UsageHint", "Bitte ein Profil auswählen und 'Transfer starten' anklicken."
            "CancelBtn", "Abbrechen"
            "TransferBtn", "Transfer starten"
            "FinishBtn", "Fertig"

            "Initializing", "Initialisiere..."
            "TransferInitiated", "Dateien werden gesucht..."
            "Transferring", "Dateien werden übertragen..."
            "TransferCompleted", "Transfer abgeschlossen"
        ]
    | _ ->
        Map [
            "Error", "Error"
            "ErrorOccurred", "Error occurred!"
            "IncompleteSetup", "App setup is incomplete."
            "MissingFile", "Missing file"
            "CorruptSetup", "App setup is corrupt."

            "UsageHint", "Please select a profile and hit 'Run Transfer'."
            "CancelBtn", "Cancel"
            "TransferBtn", "Run Transfer"
            "FinishBtn", "Done"

            "Initializing", "Initializing..."
            "TransferInitiated", "Searching files..."
            "Transferring", "Transferring files..."
            "TransferCompleted", "Transfer completed"
        ]
