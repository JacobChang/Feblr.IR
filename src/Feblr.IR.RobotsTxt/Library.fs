module Feblr.IR.RobotsTxt

open System

type UserAgent =
    | Allbot
    | GoogleBot
    | BingBot
    | BaiduSpider
    | Unrecognize of string

type Directive =
    | Allow of string
    | Disallow of string
    | Unsupport of string

type Entry =
    { userAgents: UserAgent list
      directives: Directive list }

    static member empty =
        { userAgents = List.empty
          directives = List.empty }

    static member isEmpty (entry: Entry) = List.isEmpty entry.userAgents && List.isEmpty entry.directives

type Sitemap = string

type RobotsTxt =
    { entries: Entry list
      sitemap: Sitemap list }

    static member empty =
        { entries = List.empty
          sitemap = List.empty }

    static member isAllowed (robotsTxt: RobotsTxt) (userAgent: UserAgent) (pathname: string): bool =
        let isMatch (basePath: string) (targetPath: string): bool =
            if basePath = targetPath
            then true
            else targetPath.StartsWith(basePath) && targetPath.[basePath.Length] = '/'

        let checkDirective (allowed: bool) (directive: Directive) =
            match directive with
            | Allow allowPathname ->
                if isMatch allowPathname pathname then true else allowed
            | Disallow disallowPathname ->
                if isMatch disallowPathname pathname then false else allowed
            | _ -> allowed

        let checkEntry allowed entry =
            match List.tryFind (fun _userAgent -> _userAgent = Allbot || _userAgent = userAgent) entry.userAgents with
            | Some _ -> List.fold checkDirective allowed entry.directives
            | _ -> allowed

        List.fold checkEntry true robotsTxt.entries

type Line =
    | Empty of string
    | Comment of string
    | UserAgent of UserAgent
    | Directive of Directive
    | Sitemap of Sitemap
    | Illegal of string

let parseLine (line: string): Line =
    if String.IsNullOrWhiteSpace line then
        Empty line
    else if line.StartsWith("#") then
        Comment line
    else
        let parts = line.Split([| ':' |])
        let firstPart = parts.[0].ToLower()
        let commentStartPosition = parts.[1].IndexOf("#")

        let secondPart =
            if commentStartPosition = -1
            then parts.[1].Trim()
            else parts.[1].Substring(0, commentStartPosition).Trim()
        if parts.Length = 2 then
            match firstPart with
            | "user-agent" ->
                let userAgent =
                    match secondPart.ToLower() with
                    | "*" -> Allbot
                    | "googlebot" -> GoogleBot
                    | "bingbot" -> BingBot
                    | "baiduspider" -> BaiduSpider
                    | _ -> Unrecognize secondPart
                UserAgent userAgent
            | "allow" ->
                let directive = Allow secondPart
                Directive directive
            | "disallow" ->
                let directive = Disallow secondPart
                Directive directive
            | "sitemap" -> Sitemap secondPart
            | _ ->
                let directive = Unsupport line
                Directive directive
        else
            Illegal line

let groupLine (robotsTxt: RobotsTxt) (line: Line) =
    let firstEntry = List.head robotsTxt.entries
    let restEntries = List.tail robotsTxt.entries
    match line with
    | Illegal line
    | Empty line
    | Comment line -> robotsTxt
    | UserAgent userAgent ->
        if List.isEmpty firstEntry.directives then
            let newEntry = { firstEntry with userAgents = List.append firstEntry.userAgents [ userAgent ] }
            { robotsTxt with entries = newEntry :: restEntries }
        else
            let newEntry = { Entry.empty with userAgents = [ userAgent ] }
            { robotsTxt with entries = newEntry :: robotsTxt.entries }
    | Directive directive ->
        let newEntry = { firstEntry with directives = List.append firstEntry.directives [ directive ] }
        { robotsTxt with entries = newEntry :: restEntries }
    | Sitemap sitemap -> { robotsTxt with sitemap = List.append robotsTxt.sitemap [ sitemap ] }

let removeEmptyEntry (robotsTxt: RobotsTxt): RobotsTxt =
    let newEntries = List.filter (not << Entry.isEmpty) robotsTxt.entries

    { robotsTxt with entries = List.rev newEntries }

let parse (txt: string): RobotsTxt =
    txt.Split([| '\n' |])
    |> Seq.filter (not << String.IsNullOrWhiteSpace)
    |> Seq.map parseLine
    |> Seq.fold groupLine { RobotsTxt.empty with entries = [ Entry.empty ] }
    |> removeEmptyEntry
