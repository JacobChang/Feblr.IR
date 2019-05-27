namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

open Message
open Coordinator

module Commander =
    type ICommander =
        inherit IActorGrain<CommanderMessage>

    type Commander() =
        inherit ActorGrain()

        interface ICommander

        override this.Receive(message) = task {
            match message with
            | :? Message.CommanderMessage as msg ->
                match msg with
                | DispatchJob job ->
                    do! Coordinator.start this.System job
                    return none()
            | _ ->
                return unhandled()
        }

        static member dispatch (actorSystem: IActorSystem) (domain: Uri) (strategy: Strategy) = task {
            let commander = ActorSystem.typedActorOf<ICommander, CommanderMessage>(actorSystem, "commander")
            let crawlJob = { domain = domain; strategy = strategy; commander = commander }
            do! commander <! DispatchJob crawlJob
        }
