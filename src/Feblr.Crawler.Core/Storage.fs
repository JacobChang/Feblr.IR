namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

open Message
open System.Threading.Tasks

module Storage =
    type IStorage =
        inherit IActorGrain<CrawlerMessage>

    type Storage() =
        inherit ActorGrain()

        let mutable currTask: CrawlTask option = None

        interface IStorage

        override this.Receive(message) = task {
            match message with
            | :? CrawlerMessage as msg ->
                match msg with
                | StartCrawl crawlTask ->
                    currTask <- Some crawlTask
                    do! this.download crawlTask
                    return none()
                | CancelCrawl coordinator ->
                    match currTask with
                    | Some crawlTask ->
                        do! coordinator <! TaskCancelled crawlTask
                    | None -> ()
                    return none()
                | DownloadFinished (uri, content) ->
                    return none()
                | DownloadFailed uri ->
                    return none()
                | DownloadCancelled uri ->
                    return none()
                | ExtractFinished (uri, links) ->
                    return none()
                | ExtractFailed uri ->
                    return none()
                | ExtractCancelled uri ->
                    return none()
            | _ ->
                return unhandled()
        }

        member this.download (crawlTask: CrawlTask): Task<unit> = task {
            do! Async.Sleep 1000
        }

        member this.extract (crawlTask: CrawlTask): Task<unit> = task {
            do! Async.Sleep 1000
        }