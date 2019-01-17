module Feblr.Browser.Tests

open Expecto

open Hopac
open Feblr.Browser
open Feblr.Browser.DevToolsProtocol
open System
open System.IO

[<EntryPoint>]
let main argv =
    let outputFolder = Path.Combine(Environment.CurrentDirectory, "download", Downloader.defaultDownloadOptioon.revision.ToString())
    if File.Exists (Path.Combine (outputFolder, "chromium.zip")) then
        printfn "file exist"
    else
        let options = 
            { Downloader.defaultDownloadOptioon with platform = Downloader.Platform.OSX; downloadFolder = outputFolder; outputFolder = outputFolder }
        Downloader.download options
        |> run
        |> (fun _ -> printfn "downloaed chromium brower")

    let execPath =
        Path.Combine (outputFolder, "chrome-mac/Chromium.app/Contents/MacOS/Chromium")

    let launchOption =
        { execPath = execPath
          arguments = [] }

    let browser = DevToolsProtocol.launch launchOption

    0