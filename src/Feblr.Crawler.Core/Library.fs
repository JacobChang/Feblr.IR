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

open Message
open Commander

module Engine =
    type Config =
        { clusterId: string
          serviceId: string
          siloPort: int
          siloGatewayPort: int
          siloAddress: IPAddress }

    type Engine =
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

    let crawl (engine: Engine) (job: CrawlJob) = task {
        do! Commander.dispatch (engine.actorSystem) job
    }
