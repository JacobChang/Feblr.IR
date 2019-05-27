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

        // head is the currently running job
        let mutable jobs: CrawlJob list = List.empty
        // head is the currently running task
        let mutable tasks: CrawlTask list = List.empty
        let mutable jobRunning = false

        interface ICoordinator

        override this.Receive(message) = task {
            match message with
            | :? Message.CoordinatorMessage as msg ->
                match msg with
                | StartJob job ->
                    jobs <- List.append jobs [job]
                    if not jobRunning then
                        match List.tryHead jobs with
                        | Some firstJob ->
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            let firstTask = { uri = firstJob.domain; depth = 0; coordinator = coordinator }
                            tasks <- [firstTask]
                            do! this.startCrawler firstTask
                        | None -> ()
                    return none()
                | StopJob crawlJob ->
                    if jobRunning then
                        match List.tryHead jobs with
                        | Some currJob when currJob.domain = crawlJob.domain ->
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            do! this.stopCrawler { uri = crawlJob.domain; coordinator = coordinator; depth = 0 }
                            tasks <- List.empty
                        | _ -> ()
                    jobs <- List.filter (fun job -> job.domain <> crawlJob.domain) jobs
                    return none()
                | TaskFinished (crawlTask, content, links) ->
                    match List.tryHead tasks with
                    | Some currTask ->
                        if currTask.uri = crawlTask.uri then
                            printfn "task finished: %A" crawlTask
                            let (taskUris, jobUris) =
                                links
                                |> List.partition (fun uri -> uri.Host = crawlTask.uri.Host)
                            let newTasks =
                                taskUris
                                |> List.map (fun uri -> { uri = uri; depth = crawlTask.depth + 1; coordinator = crawlTask.coordinator})
                            let allTasks = List.append tasks newTasks
                            tasks <- List.tail allTasks

                            let newJobs =
                                jobUris
                                |> List.map (fun uri ->
                                    match List.tryHead jobs with
                                    | Some currJob ->
                                        let newJob = { domain = uri; strategy = currJob.strategy;  commander = currJob.commander }
                                        currJob.commander <! DispatchJob newJob |> ignore
                                    | _ ->
                                        ()
                                )

                            match List.tryHead tasks with
                            | Some nextTask ->
                                do! this.startCrawler nextTask
                            | _ ->
                                jobs <- List.tail jobs
                                match List.tryHead jobs with
                                | Some nextJob ->
                                    let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                                    let firstTask = { uri = nextJob.domain; depth = 0; coordinator = coordinator }
                                    tasks <- [firstTask]
                                    do! this.startCrawler firstTask
                                | _ -> ()
                        else
                            ()
                    | None ->
                        jobRunning <- false
                        match List.tryHead jobs with
                        | Some nextJob ->
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            let crawlTask = { uri = nextJob.domain; depth = 0; coordinator = coordinator }
                            do! this.startCrawler crawlTask
                        | None -> ()
                    return none()
                | TaskFailed crawlTask ->
                    match List.tryHead tasks with
                    | Some currTask ->
                        if currTask.uri = crawlTask.uri then
                            tasks <- List.tail tasks
                            match List.tryHead tasks with
                            | Some nextTask ->
                                do! this.startCrawler nextTask
                            | _ ->
                                jobs <- List.tail jobs
                                match List.tryHead jobs with
                                | Some nextJob ->
                                    let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                                    let firstTask = { uri = nextJob.domain; depth = 0; coordinator = coordinator }
                                    tasks <- [firstTask]
                                    do! this.startCrawler firstTask
                                | _ -> ()
                        else
                            ()
                    | None ->
                        jobRunning <- false
                        match List.tryHead jobs with
                        | Some nextJob ->
                            let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                            let crawlTask = { uri = nextJob.domain; depth = 0; coordinator = coordinator }
                            do! this.startCrawler crawlTask
                        | None -> ()
                    return none()
                | TaskStopped crawlTask ->
                    match List.tryHead jobs with
                    | Some firstJob ->
                        let coordinator = ActorRef<CoordinatorMessage>(this.Self)
                        let firstTask = { uri = firstJob.domain; depth = 0; coordinator = coordinator }
                        do! this.startCrawler firstTask
                    | None -> ()
                    return none()
            | _ ->
                return unhandled()
        }

        member this.startCrawler (crawlTask: CrawlTask) : Task<unit> = task {
            do! Crawler.start this.System crawlTask
            jobRunning <- true
        }

        member this.stopCrawler (crawlTask: CrawlTask) : Task<unit> = task {
            do! Crawler.stop this.System crawlTask
            jobRunning <- false
        }

        static member start (actorSystem: IActorSystem) (job: CrawlJob) = task {
            let coordinatorId = sprintf "coordinator.%s" job.domain.Host
            let coordinator = ActorSystem.typedActorOf<ICoordinator, CoordinatorMessage>(actorSystem, coordinatorId)
            do! coordinator <! StartJob job
        }
