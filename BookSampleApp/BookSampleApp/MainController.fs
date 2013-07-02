﻿namespace global

open System
open Microsoft.FSharp.Linq
open Linq.NullableOperators

type MainContoller(symbology : string -> Symbology.Instrument option) = 

    new() = MainContoller(Symbology.yahoo)

    interface IController<MainEvents, MainModel> with

        member this.InitModel model = 
            model.StopLossMargin <- Nullable -50M
            model.TakeProfitMargin <- Nullable 50M
            model.PositionChangeRatio <- Nullable 00.00M

        member this.EventHandler = function 
            | InstrumentInfo -> this.GetInstrumentInfo
            | LivePriceUpdates newPrice -> this.UpdateCurrentPrice newPrice
            | FlipPosition -> this.FlipPosition
            | Start -> this.Start

    member this.GetInstrumentInfo(model : MainModel) = 
        match symbology model.Symbol with
        | Some x -> 
            model.InstrumentName <- x.Name
            model.Price <- Nullable x.LastPrice
        | None -> 
            model |> Validation.addError <@ fun m -> m.Symbol @> "Invalid symbol."
        ()

    member this.UpdateCurrentPrice newPrice (model : MainModel) =
        model.Price <- Nullable newPrice
        if model.PositionOpenedAt.HasValue && not model.PositionClosedAt.HasValue
        then 
            model.PositionPnL <- model.PositionCurrentValue ?-? model.PositionOpenValue
            model.PositionChangeRatio <- ((model.PositionCurrentValue ?-? model.PositionOpenValue) ?/? model.PositionOpenValue) ?* 100M
            if model.ExitStrategyStarted 
                && (model.PositionChangeRatio.Value >= model.TakeProfitMargin.Value || model.PositionChangeRatio.Value <= model.StopLossMargin.Value)
            then    
                model.PositionClosedAt <- model.Price
                model.PositionAction <- PositionAction.Open

    member this.FlipPosition (model : MainModel) = 
        if model.PositionAction = PositionAction.Open
        then 
            model.PositionOpenedAt <- model.Price
            model.PositionAction <- PositionAction.Close
        else
            assert (model.PositionAction = PositionAction.Close)
            model.PositionClosedAt <- model.Price
            model.PositionAction <- PositionAction.Open

    member this.Start  (model : MainModel) = 
        if model.PositionOpenedAt.HasValue && not model.PositionClosedAt.HasValue
        then 
            model.ExitStrategyStarted <- true
