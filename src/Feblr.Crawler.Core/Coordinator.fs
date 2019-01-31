namespace Feblr.Crawler.Core

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

open Message
open Crawler

module Coordinator =
    type ICoordinator =
        inherit IActorGrain<CoordinatorMessage>

    type Coordinator() =
        inherit ActorGrain()

        let mutable jobs: CrawlJob list = List.empty
        let mutable tasks: CrawlTask list = List.empty
        let mutable jobRunning = false

        interface ICoordinator

        override this.Receive(message) = task {
            match message with
            | :? Message.CoordinatorMessage as msg ->
                match msg with
                | CreateJob job ->
                    jobs <- List.append jobs [job]
                    if not jobRunning then
                        match List.tryHead jobs with
                        | Some firstJob ->
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            let firstTask = { uri = firstJob.domain; depth = 0; coordinator = coordinator }
                            do! this.startTask firstTask
                        | None -> ()

                    return none()
                | CancelJob crawlJob ->
                    if jobRunning then
                        match List.tryHead jobs with
                        | Some currJob when currJob.domain = crawlJob.domain ->
                            let crawlerId = sprintf "crawler.%s" crawlJob.domain.Host
                            let crawler = ActorSystem.typedActorOf<ICrawler, CrawlerMessage>(this.System, crawlerId)
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            do! crawler <! CancelCrawl coordinator
                        | _ -> ()
                    jobs <- List.filter (fun job -> job.domain <> crawlJob.domain) jobs
                    return none()
                | TaskFinished (crawlTask, links) ->
                    match List.tryHead jobs with
                    | Some currJob ->
                        if currJob.domain.Host = crawlTask.uri.Host && crawlTask.depth < currJob.depth then
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            let crawlTasks =
                                List.map (fun link -> { uri = link; depth = crawlTask.depth + 1; coordinator = coordinator }) links
                            tasks <- List.append tasks crawlTasks
                    | None -> ()

                    match List.tryHead tasks with
                    | Some nextTask ->
                        do! this.startTask nextTask
                    | None ->
                        jobRunning <- false
                        match List.tryHead jobs with
                        | Some nextJob ->
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            let crawlTask = { uri = nextJob.domain; depth = 0; coordinator = coordinator }
                            do! this.startTask crawlTask
                        | None -> ()
                    return none()
                | TaskFailed crawlTask ->
                    jobRunning <- false
                    return none()
                | TaskCancelled crawlTask ->
                    match List.tryHead jobs with
                    | Some firstJob ->
                        let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                        let firstTask = { uri = firstJob.domain; depth = 0; coordinator = coordinator }
                        do! this.startTask firstTask
                    | None -> ()
                    return none()
            | _ ->
                return unhandled()
        }

        member this.startTask (crawlTask: CrawlTask) : Task<unit> = task {
            do! Crawler.start this.System crawlTask
            jobRunning <- true
        }
