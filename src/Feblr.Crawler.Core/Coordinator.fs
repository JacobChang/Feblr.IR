namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

module Coordinator =
    type Job =
        { uri: Uri
          depth: int }

    type Message =
        | CreateJob of Job

    type ICoordinator =
        inherit IActorGrain<Message>

    type Coordinator() =
        inherit ActorGrain()

        let mutable jobs = List.empty

        interface ICoordinator

        override this.Receive(message) = task {
            match message with
            | :? Message as msg ->
                match msg with
                | CreateJob job ->
                    jobs <- List.append jobs [job]
                    return none()
            | _ ->
                return unhandled()
        }
