namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

open Message

module Extractor =
    type IExtractor =
        inherit IActorGrain<ExtractorMessage>

    type Extractor() =
        inherit ActorGrain()
        interface IExtractor

        override this.Receive(message) = task {
            match message with
            | :? ExtractorMessage as msg ->
                match msg with
                | StartExtract extractTask ->
                    printfn "%A %s" extractTask.uri extractTask.content
                    return none()
                | CancelExtract crawler ->
                    return none()
            | _ ->
                return unhandled()
        }

        static member start (actorSystem: IActorSystem) (extractTask: ExtractTask) = task {
            let extractorId = sprintf "extractor.%s" extractTask.uri.Host
            let extractor = ActorSystem.typedActorOf<IExtractor, ExtractorMessage>(actorSystem, extractorId)
            do! extractor <! StartExtract extractTask
        }