// Learn more about F# at http://fsharp.org
namespace Feblr.Crawler

open System
open System.Net
open System.Threading.Tasks
open FSharp.Control.Tasks
open Feblr.Crawler.Core

module Main =
    [<EntryPoint>]
    let main argv =
        let config: Engine.Config = {
            clusterId = "crawler-cluster"
            serviceId = "crawler-cluster-service"
            siloPort = 11111
            siloGatewayPort = 30000
            siloAddress = IPAddress.Loopback
        }

        let hostBuilder, clientBuilder = Engine.setup config

        let task: Task<unit> = task {
            let! engine = Engine.start hostBuilder clientBuilder
            let uri = Uri("https://example.com")
            let depth = 1
            do! Engine.crawl engine { uri = uri; depth = depth }
            let uri = Uri("https://example2.com")
            let depth = 1
            do! Engine.crawl engine { uri = uri; depth = depth }
        }
        task.Wait()

        0 // return an integer exit code
