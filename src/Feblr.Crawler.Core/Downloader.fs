namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp

open Message

module Downloader =
    type IDownloader =
        inherit IActorGrain<DownloaderMessage>

    type Downloader() =
        inherit ActorGrain()
        interface IDownloader

        override this.Receive(message) = task {
            match message with
            | :? DownloaderMessage as msg ->
                match msg with
                | StartDownload downloadTask ->
                    downloadTask.crawler <! DownloadFinished (downloadTask.crawlTask, "") |> ignore
                    return none()
                | StopDownload crawler ->
                    return none()
            | _ ->
                return unhandled()
        }

        static member start (actorSystem: IActorSystem) (downloadTask: DownloadTask) = task {
            let downloaderId = sprintf "downloader.%s" downloadTask.crawlTask.uri.Host
            let downloader = ActorSystem.typedActorOf<IDownloader, DownloaderMessage>(actorSystem, downloaderId)
            do! downloader <! StartDownload downloadTask
        }

        static member stop (actorSystem: IActorSystem) (downloadTask: DownloadTask) = task {
            let downloaderId = sprintf "downloader.%s" downloadTask.crawlTask.uri.Host
            let downloader = ActorSystem.typedActorOf<IDownloader, DownloaderMessage>(actorSystem, downloaderId)
            do! downloader <! StopDownload downloadTask
        }