namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp
open HttpFs
open HttpFs.Client
open Hopac

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
                    this.download downloadTask |> ignore
                    return none()
                | StopDownload crawler ->
                    return none()
            | _ ->
                return unhandled()
        }

        member this.download (downloadTask: DownloadTask) = task {
            let uri = downloadTask.crawlTask.uri.ToString()
            let bodyStr =
                Request.createUrl Get uri
                |> Request.responseAsString
                |> run
            downloadTask.crawler <! DownloadFinished (downloadTask.crawlTask, bodyStr) |> ignore
            return ()
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