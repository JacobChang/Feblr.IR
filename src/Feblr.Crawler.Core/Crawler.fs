namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

module Crawler =
    type CrawlTask =
        { uri: Uri
          depth: int }

    type Message =
        | StartCrawl of CrawlTask

    type ICrawler =
        inherit IActorGrain<Message>

    type Crawler() =
        inherit ActorGrain()
        interface ICrawler

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