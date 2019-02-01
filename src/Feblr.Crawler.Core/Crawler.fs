namespace Feblr.Crawler.Core

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

open Message
open Downloader
open Extractor

module Crawler =
    type ICrawler =
        inherit IActorGrain<CrawlerMessage>

    type Crawler() =
        inherit ActorGrain()

        let mutable currTask: CrawlTask option = None

        interface ICrawler

        override this.Receive(message) = task {
            match message with
            | :? CrawlerMessage as msg ->
                match msg with
                | StartCrawl crawlTask ->
                    currTask <- Some crawlTask
                    let crawler = ActorRef<CrawlerMessage>(this.Self)
                    do! this.startDownloader { crawlTask = crawlTask; crawler = crawler }
                    return none()
                | StopCrawl crawlTask ->
                    match currTask with
                    | Some currTask ->
                        if currTask.uri = crawlTask.uri then
                            do! crawlTask.coordinator <! TaskStopped crawlTask
                    | None -> ()
                    return none()
                | DownloadFinished (crawlTask, content) ->
                    let crawler = ActorRef<CrawlerMessage>(this.Self)
                    do! this.startExtractor { crawlTask = crawlTask; content = content; crawler = crawler }
                    return none()
                | ExtractFinished (crawlTask, content, links) ->
                    do! crawlTask.coordinator <! TaskFinished (crawlTask, content, links)
                    return none()
                | _ ->
                    return unhandled()
            | _ ->
                return unhandled()
        }

        member this.startDownloader (downloadTask: DownloadTask): Task<unit> = task {
            do! Downloader.start this.System  downloadTask
        }

        member this.stopDownloader (downloadTask: DownloadTask): Task<unit> = task {
            do! Downloader.stop this.System  downloadTask
        }

        member this.startExtractor (extractTask: ExtractTask): Task<unit> = task {
            do! Extractor.start this.System  extractTask
        }

        member this.stopExtractor (extractTask: ExtractTask): Task<unit> = task {
            do! Extractor.stop this.System  extractTask
        }

        static member start (actorSystem: IActorSystem) (crawlTask: CrawlTask) = task {
            let crawlerId = sprintf "crawler.%s" crawlTask.uri.Host
            let crawler = ActorSystem.typedActorOf<ICrawler, CrawlerMessage>(actorSystem, crawlerId)
            do! crawler <! StartCrawl crawlTask
        }

        static member stop (actorSystem) (crawlTask: CrawlTask) = task {
            let crawlerId = sprintf "crawler.%s" crawlTask.uri.Host
            let crawler = ActorSystem.typedActorOf<ICrawler, CrawlerMessage>(actorSystem, crawlerId)
            do! crawler <! StopCrawl crawlTask
        }