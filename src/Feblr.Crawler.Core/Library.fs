namespace Feblr.Crawler.Core

open Akka.Actor
open Akka.Configuration

module Engine =
    type CrawlerSystem =
        { actorSystem: ActorSystem }

    let start (systemName: string) (config: Config): CrawlerSystem =
        let actorSystem = ActorSystem.Create (systemName, config)
        { actorSystem = actorSystem }

    let waitForTerminated (crawlerSystem: CrawlerSystem) =
        crawlerSystem.actorSystem.WhenTerminated.Wait()

    let stop (crawlerSystem: CrawlerSystem) =
        crawlerSystem.actorSystem.Terminate ()
