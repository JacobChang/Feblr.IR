namespace Feblr.Crawler

open System.IO
open Akka.Configuration

module Config =
    let parse (path: string): Config =
        let content = File.ReadAllText(path);
        ConfigurationFactory.ParseString(content)
