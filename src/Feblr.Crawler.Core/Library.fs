namespace Feblr.Crawler.Core

open System
open System.Net
open System.Reflection

open FSharp.Control.Tasks
open Orleans
open Orleans.Hosting
open Orleans.Configuration
open Orleans.Runtime
open Orleankka
open Orleankka.Cluster
open Orleankka.Client
open Orleankka.FSharp

module Engine =
    type Job =
        { uri: Uri
          depth: int }

    type Config =
        { clusterId: string
          localhostSiloPort: int
          localhostSiloGatewayPort: int
          localhostSiloAddress: IPAddress }

    type CrawlerSystem =
        { actorSystem: IClientActorSystem }

    let start (config: Config) = task {
        let sb = new SiloHostBuilder()
        sb.Configure<ClusterOptions>(fun (options:ClusterOptions) -> options.ClusterId <- config.clusterId) |> ignore
        sb.UseDevelopmentClustering(fun (options:DevelopmentClusterMembershipOptions) -> options.PrimarySiloEndpoint <- IPEndPoint(config.localhostSiloAddress, config.localhostSiloPort)) |> ignore
        sb.ConfigureEndpoints(config.localhostSiloAddress, config.localhostSiloPort, config.localhostSiloGatewayPort) |> ignore

        // register assembly containing your custom actor grain interfaces
        sb.ConfigureApplicationParts(fun x -> x.AddApplicationPart(Assembly.GetExecutingAssembly()).WithCodeGeneration() |> ignore) |> ignore

        // register Orleankka extension
        sb.UseOrleankka() |> ignore

        // configure localhost silo client
        let cb = new ClientBuilder()
        cb.Configure<ClusterOptions>(fun (options:ClusterOptions) -> options.ClusterId <- config.clusterId) |> ignore
        cb.UseStaticClustering(fun (options:StaticGatewayListProviderOptions) -> options.Gateways.Add(IPEndPoint(config.localhostSiloAddress, config.localhostSiloPort).ToGatewayUri())) |> ignore

        // register assembly containing your custom actor grain interfaces
        cb.ConfigureApplicationParts(fun x -> x.AddApplicationPart(Assembly.GetExecutingAssembly()).WithCodeGeneration() |> ignore) |> ignore

        // register Orleankka extension
        cb.UseOrleankka() |> ignore

        let host = sb.Build()
        do! host.StartAsync()

        let client = cb.Build()
        do! client.Connect()

        let actorSystem = client.ActorSystem()
        return { actorSystem = actorSystem }
    }

    let crawl (crawlerSystem: CrawlerSystem) (job: Job) =
         ignore job

