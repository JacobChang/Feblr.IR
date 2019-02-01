namespace Feblr.Crawler.Core

open System
open System.Threading.Tasks
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
                    let coordinatorId = sprintf "coordinator.%s" job.domain.Host
                    let coordinator = ActorSystem.typedActorOf<ICoordinator, CoordinatorMessage>(this.System, coordinatorId)
                    do! coordinator <! StartJob job
                    return none()
            | _ ->
                return unhandled()
        }

        static member dispatch (actorSystem: IActorSystem) (job: CrawlJob) = task {
            let commander = ActorSystem.typedActorOf<ICommander, CommanderMessage>(actorSystem, "commander")
            do! commander <! DispatchJob job
        }
