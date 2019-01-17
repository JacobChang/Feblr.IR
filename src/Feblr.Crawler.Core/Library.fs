namespace Feblr.Crawler

open Akka.Actor
open Akka.Configuration

module Core =
    type CrawlerSystem =
        { actorSystem: ActorSystem }

    let start (systemName: string) (config: Config): CrawlerSystem =
        let actorSystem = ActorSystem.Create (systemName, config)
        { actorSystem = actorSystem }

    let waitForTerminated (crawlerSystem: CrawlerSystem) =
        crawlerSystem.actorSystem.WhenTerminated.Wait()

    let stop (crawlerSystem: CrawlerSystem) =
        crawlerSystem.actorSystem.Terminate ()
