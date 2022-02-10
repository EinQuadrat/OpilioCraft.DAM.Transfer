namespace OpilioCraft.DAM.Transfer.App

open Elmish
open OpilioCraft.DAM.Transfer

[<RequireQualifiedAccess>]
module Prepare =
    type State = {
        Profiles : string list
        SelectedProfile : string option
    }

    type Msg =
    | SelectProfile of string option

    let init () = {
        Profiles = UserSettings.profilesCatalogue().Keys |> Seq.toList
        SelectedProfile = None
    }
    
    let update (prepareMsg : Msg) (prepareState : State) =
        match prepareMsg with
        | SelectProfile selection -> { prepareState with SelectedProfile = selection }, Cmd.none
