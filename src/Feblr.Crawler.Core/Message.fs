namespace Feblr.Crawler.Core

open System
open Orleankka.FSharp

module rec Message =
    type CrawlJob =
        { domain: Uri
          depth: int }

    type CrawlTask =
        { uri: Uri
          depth: int
          coordinator: ActorRef<CoordinatorMessage> }

    type CommanderMessage =
        | DispatchJob of CrawlJob

    type CoordinatorMessage =
        | StartJob of CrawlJob
        | StopJob of CrawlJob
        | TaskFinished of CrawlTask * string * Uri list
        | TaskFailed of CrawlTask
        | TaskStopped of CrawlTask

    type CrawlerMessage =
        | StartCrawl of CrawlTask
        | StopCrawl of CrawlTask
        | DownloadFinished of CrawlTask * string
        | DownloadFailed of CrawlTask
        | DownloadStopped of CrawlTask
        | ExtractFinished of CrawlTask * string * Uri list
        | ExtractFailed of CrawlTask * string
        | ExtractStopped of CrawlTask

    type DownloadTask =
        { crawlTask: CrawlTask
          crawler: ActorRef<CrawlerMessage> }

    type DownloaderMessage =
        | StartDownload of DownloadTask
        | StopDownload of DownloadTask

    type ExtractTask =
        { crawlTask: CrawlTask
          content: string
          crawler: ActorRef<CrawlerMessage> }

    type ExtractorMessage =
        | StartExtract of ExtractTask
        | StopExtract of ExtractTask

    type StorageMessage = string