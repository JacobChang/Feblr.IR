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

    type CoordinatorMessage =
        | CreateJob of CrawlJob
        | CancelJob of CrawlJob
        | TaskFinished of CrawlTask * Uri list
        | TaskFailed of CrawlTask
        | TaskCancelled of CrawlTask

    type CrawlerMessage =
        | StartCrawl of CrawlTask
        | CancelCrawl of ActorRef<CoordinatorMessage>
        | DownloadFinished of Uri * string
        | DownloadFailed of Uri
        | DownloadCancelled of Uri
        | ExtractFinished of Uri * Uri list
        | ExtractFailed of Uri
        | ExtractCancelled of Uri

    type DownloadTask =
        { uri: Uri
          crawler: ActorRef<CrawlerMessage> }

    type DownloaderMessage =
        | StartDownload of DownloadTask
        | CancelDownload of ActorRef<CrawlerMessage>

    type ExtractTask =
        { uri: Uri
          content: string
          crawler: ActorRef<CrawlerMessage> }

    type ExtractorMessage =
        | StartExtract of ExtractTask
        | CancelExtract of ActorRef<CrawlerMessage>
