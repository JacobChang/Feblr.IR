module Feblr.Browser.Tests

open Expecto

open Hopac
open Feblr.Browser
open Feblr.Browser.DevToolsProtocol
open System
open System.IO

[<EntryPoint>]
let main argv =
    let downloadFolder = Path.Combine(Environment.CurrentDirectory, "download", Downloader.defaultDownloadOptioon.revision.ToString())
    let options =
        { Downloader.defaultDownloadOptioon with platform = Downloader.Platform.OSX; downloadFolder = downloadFolder; }

    if File.Exists (Path.Combine (downloadFolder, "chromium.zip")) then
        printfn "file exist"
    else
        Downloader.download options
        |> run
        |> (fun _ -> printfn "downloaed chromium brower")

    let execPath = Downloader.getExecPath options

    printfn "%s" execPath

    let launchOption =
        { execPath = execPath
          arguments = [] }

    let browser = DevToolsProtocol.launch launchOption

    0