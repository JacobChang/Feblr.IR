// Learn more about F# at http://fsharp.org
namespace Feblr.Crawler

open System.Net
open Feblr.Crawler.Core

module Main =
    [<EntryPoint>]
    let main argv =
        let config: Engine.Config = {
            clusterId = "localhost-demo"
            localhostSiloPort = 11111
            localhostSiloGatewayPort = 30000
            localhostSiloAddress = IPAddress.Loopback
        }
        let crawler = Engine.start config

        0 // return an integer exit code
