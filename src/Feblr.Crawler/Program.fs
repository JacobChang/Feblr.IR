// Learn more about F# at http://fsharp.org
namespace Feblr.Crawler

open System
open System.IO
open Feblr.Crawler.Core

module Main =
    [<EntryPoint>]
    let main argv =
        let configFile = Path.Combine (Environment.CurrentDirectory, "./src/Feblr.Crawler/crawler.hocon")
        let config = Config.parse configFile
        let crawler = Core.start "crawler" config

        let stopCrawler (sender: obj) (evt: ConsoleCancelEventArgs) =
            let stopTask = Core.stop crawler
            stopTask.Wait()
            evt.Cancel <- true

        Console.CancelKeyPress.AddHandler (new ConsoleCancelEventHandler(stopCrawler))

        Core.waitForTerminated crawler

        0 // return an integer exit code
