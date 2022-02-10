namespace OpilioCraft.DAM.Transfer.App

open System.Windows

open OpilioCraft.DAM.Transfer.App.Resources

[<RequireQualifiedAccess>]
module Shared =
    let errorMessageTemplate rawMessage =
        sprintf "%s\n\n%s" I18N["ErrorOccurred"] rawMessage

    let showError errorMessage =
        MessageBox.Show(
            errorMessage,
            I18N["Error"],
            MessageBoxButton.OK,
            MessageBoxImage.Error
        ) |> ignore

    let closeApp () =
        Application.Current.MainWindow.Close()