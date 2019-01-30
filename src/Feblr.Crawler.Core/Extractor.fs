namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

module Extractor =
    type CrawlTask =
        { uri: Uri
          depth: int }

    type Message =
        | StartCrawl of CrawlTask

    type IExtractor =
        inherit IActorGrain<Message>

    type Extractor() =
        inherit ActorGrain()
        interface IExtractor

        override this.Receive(message) = task {
            match message with
            | :? Message as msg ->
                match msg with
                | StartCrawl task ->
                    printfn "%A" task
                    return none()
            | _ ->
                return unhandled()
        }