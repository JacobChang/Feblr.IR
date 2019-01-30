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

open Feblr.Crawler.Core.Coordinator

module Engine =
    type Config =
        { clusterId: string
          serviceId: string
          siloPort: int
          siloGatewayPort: int
          siloAddress: IPAddress }

    type CrawlerSystem =
        { actorSystem: IClientActorSystem }

    let setup (config: Config) =
        let sb = SiloHostBuilder()
        sb.Configure<ClusterOptions>(fun (options:ClusterOptions) -> options.ClusterId <- config.clusterId; options.ServiceId <- config.serviceId) |> ignore
        sb.UseDevelopmentClustering(fun (options:DevelopmentClusterMembershipOptions) -> options.PrimarySiloEndpoint <- IPEndPoint(config.siloAddress, config.siloPort)) |> ignore
        sb.ConfigureEndpoints(config.siloAddress, config.siloPort, config.siloGatewayPort) |> ignore
        sb.ConfigureApplicationParts(fun x -> x.AddApplicationPart(Assembly.GetExecutingAssembly()).WithCodeGeneration() |> ignore) |> ignore
        sb.UseOrleankka() |> ignore

        let cb = ClientBuilder()
        cb.Configure<ClusterOptions>(fun (options:ClusterOptions) -> options.ClusterId <- config.clusterId; options.ServiceId <- config.serviceId) |> ignore
        cb.UseStaticClustering(fun (options:StaticGatewayListProviderOptions) -> options.Gateways.Add(IPEndPoint(config.siloAddress, config.siloGatewayPort).ToGatewayUri())) |> ignore
        cb.ConfigureApplicationParts(fun x -> x.AddApplicationPart(Assembly.GetExecutingAssembly()).WithCodeGeneration() |> ignore) |> ignore
        cb.UseOrleankka() |> ignore

        (sb, cb)

    let start (hostBuilder: SiloHostBuilder) (clientBuilder: ClientBuilder) = task {
        let host = hostBuilder.Build()
        do! host.StartAsync()
        let client = clientBuilder.Build()
        do! client.Connect()

        let actorSystem = client.ActorSystem()
        return { actorSystem = actorSystem }
    }

    let crawl (crawlerSystem: CrawlerSystem) (job: Job) = task {
        let coordinatorId = sprintf "coordinator.%s" job.uri.Host
        let coordinator = ActorSystem.typedActorOf<ICoordinator, Message>(crawlerSystem.actorSystem, coordinatorId)
        do! coordinator <! CreateJob job
    }
