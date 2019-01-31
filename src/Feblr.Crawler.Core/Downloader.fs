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
                    printfn "%A" downloadTask.uri
                    return none()
                | CancelDownload crawler ->
                    return none()
            | _ ->
                return unhandled()
        }

        static member start (actorSystem: IActorSystem) (downloadTask: DownloadTask) = task {
            let downloaderId = sprintf "downloader.%s" downloadTask.uri.Host
            let downloader = ActorSystem.typedActorOf<IDownloader, DownloaderMessage>(actorSystem, downloaderId)
            do! downloader <! StartDownload downloadTask
        }