namespace Feblr.Crawler.Core

open System
open FSharp.Control.Tasks
open Orleankka
open Orleankka.FSharp
open HtmlAgilityPack

open Message

module Extractor =
    type IExtractor =
        inherit IActorGrain<ExtractorMessage>

    type Extractor() =
        inherit ActorGrain()
        interface IExtractor

        override this.Receive(message) = task {
            match message with
            | :? ExtractorMessage as msg ->
                match msg with
                | StartExtract extractTask ->
                    this.extract extractTask |> ignore
                    return none()
                | StopExtract crawler ->
                    return none()
            | _ ->
                return unhandled()
        }

        member this.extract (extractTask: ExtractTask) = task {
            let doc = HtmlDocument()
            doc.LoadHtml(extractTask.content)
            let linkNodes = query {
                for node in doc.DocumentNode.Descendants("a") do
                    select node
            }
            let extractLink (linkNode: HtmlNode) =
                let href = linkNode.Attributes.["href"].Value
                Uri(extractTask.crawlTask.uri, href)

            let links =
                linkNodes
                |> Seq.map extractLink
                |> Seq.toList
            extractTask.crawler <! ExtractFinished (extractTask.crawlTask, extractTask.content, links) |> ignore
        }

        static member start (actorSystem: IActorSystem) (extractTask: ExtractTask) = task {
            let extractorId = sprintf "extractor.%s" extractTask.crawlTask.uri.Host
            let extractor = ActorSystem.typedActorOf<IExtractor, ExtractorMessage>(actorSystem, extractorId)
            do! extractor <! StartExtract extractTask
        }

        static member stop (actorSystem: IActorSystem) (extractTask: ExtractTask) = task {
            let extractorId = sprintf "extractor.%s" extractTask.crawlTask.uri.Host
            let extractor = ActorSystem.typedActorOf<IExtractor, ExtractorMessage>(actorSystem, extractorId)
            do! extractor <! StopExtract extractTask
        }