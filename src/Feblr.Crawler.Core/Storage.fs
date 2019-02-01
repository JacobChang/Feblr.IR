namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

open Message
open System.Threading.Tasks

module Storage =
    type IStorage =
        inherit IActorGrain<StorageMessage>

    type Storage() =
        inherit ActorGrain()

        let mutable currTask: CrawlTask option = None

        interface IStorage

        override this.Receive(message) = task {
            match message with
            | :? StorageMessage as msg ->
                match msg with
                | _ ->
                    return unhandled()
            | _ ->
                return unhandled()
        }
        