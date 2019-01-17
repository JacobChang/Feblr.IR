// Learn more about F# at http://fsharp.org

open System
open System.IO
open Akka
open Akka.Configuration
open Akka.Actor

let parse (path: string): Config =
    let configFile = Path.Combine (Environment.CurrentDirectory, path)
    let content = File.ReadAllText(configFile);
    ConfigurationFactory.ParseString(content)


[<EntryPoint>]
let main argv =
    let config = parse "./src/Feblr.Crawler.ClusterSeeds/cluster.hocon"
    let actorSystem = ActorSystem.Create ("crawler", config)

    let stopCrawler (sender: obj) (evt: ConsoleCancelEventArgs) =
        actorSystem.Terminate().Wait()
        evt.Cancel <- true

    Console.CancelKeyPress.AddHandler (new ConsoleCancelEventHandler(stopCrawler))

    actorSystem.WhenTerminated.Wait()

    0 // return an integer exit code
