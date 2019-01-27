namespace Feblr.Browser

open System
open System.IO
open System.Diagnostics
open System.Net.WebSockets
open System.Threading
open Hopac
open HttpFs.Client
open Thoth.Json.Net

module Downloader =
    open System.IO.Compression
    open System.Runtime.InteropServices
    open Mono.Unix.Native

    type Platform =
         | Linux
         | OSX
         | Windows32
         | Windows64

    let platform =
        if RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            Linux
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            OSX
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            if Environment.Is64BitOperatingSystem then
                Windows64
            else
                Windows32
        else
            Linux

    let defaultDownloadHost = "https://storage.googleapis.com"
    let defaultRevision = 609904

    type DownloadOption =
        { revision: int
          platform: Platform
          downloadHost: string
          downloadFolder: string }

    let defaultDownloadOptioon =
        { revision = defaultRevision
          platform = platform
          downloadHost = defaultDownloadHost
          downloadFolder = "." }

    let downloadURL (option: DownloadOption) =
        match option.platform with
        | Linux ->
            sprintf "%s/chromium-browser-snapshots/Linux_x64/%d/chrome-linux.zip" option.downloadHost option.revision
        | OSX ->
            sprintf "%s/chromium-browser-snapshots/Mac/%d/chrome-mac.zip" option.downloadHost option.revision
        | Windows32 ->
            sprintf "%s/chromium-browser-snapshots/Win/%d/chrome-win.zip" option.downloadHost option.revision
        | Windows64 ->
            sprintf "%s/chromium-browser-snapshots/Win_x64/%d/chrome-win.zip" option.downloadHost option.revision

    let getExecPath (option: DownloadOption) =
        match option.platform with
        | Linux ->
            sprintf "%s/chrome-linux/chrome" option.downloadFolder
        | OSX ->
            sprintf "%s/chrome-mac/Chromium.app/Contents/MacOS/Chromium" option.downloadFolder
        | Windows32
        | Windows64 ->
            sprintf "%s/chrome-win/chrome.exe" option.downloadFolder

    let download (option: DownloadOption) = job {
        let url = downloadURL option
        printfn "download chromium from : %s" url
        use! resp = Request.createUrl Get url |> getResponse
        let zipFileName = "chromium.zip"
        let zipFilePath = Path.Combine (option.downloadFolder, zipFileName)
        use fileStream = new FileStream(zipFilePath, FileMode.Create)
        do! Job.awaitUnitTask (resp.body.CopyToAsync fileStream)
        printfn "downloaded chromium"

        printfn "start to unzip file"
        if option.platform = Platform.OSX then
            let proc = new Process();
            proc.StartInfo.FileName <- "unzip";
            proc.StartInfo.Arguments <- (sprintf "%s -d %s" zipFilePath option.downloadFolder)
            proc.Start() |> ignore
            proc.WaitForExit()
        else
            ZipFile.ExtractToDirectory (zipFilePath, option.downloadFolder)
        printfn "finish unzip file"

        if option.platform = Platform.Linux then
            let execPath = getExecPath option
            let permissions =
                FilePermissions.S_IRWXU ||| FilePermissions.S_IRGRP ||| FilePermissions.S_IXGRP ||| FilePermissions.S_IROTH ||| FilePermissions.S_IXOTH
            Syscall.chmod (execPath, permissions) |> ignore
    }

module DevToolsProtocol =

    // copied from https://github.com/GoogleChrome/puppeteer/blob/master/lib/Launcher.js#L37
    let defaultArgs = [
      "--disable-background-networking"
      "--enable-features=NetworkServiceNetworkServiceInProcess"
      "--disable-background-timer-throttling"
      "--disable-backgrounding-occluded-windows"
      "--disable-breakpad"
      "--disable-client-side-phishing-detection"
      "--disable-default-apps"
      "--disable-dev-shm-usage"
      "--disable-extensions"
      "--disable-features=site-per-process"
      "--disable-hang-monitor"
      "--disable-ipc-flooding-protection"
      "--disable-popup-blocking"
      "--disable-prompt-on-repost"
      "--disable-renderer-backgrounding"
      "--disable-sync"
      "--disable-translate"
      "--force-color-profile=srgb"
      "--metrics-recording-only"
      "--no-first-run"
      "--safebrowsing-disable-auto-update"
      "--enable-automation"
      "--password-store=basic"
      "--use-mock-keychain"
    ];

    type Context =
        { id: int option }

    type Browser =
        { proc: Process
          webSock: WebSocket
          contexts: Context list }

        with
            member this.waitForExit () =
                this.proc.WaitForExit()

    type VersionInfo =
        { webSocketDebuggerUrl: string }

    type AttachOption =
        { debugPort: int }

    let attach (option: AttachOption) = async {
        let versionUriString = sprintf "http://127.0.0.1:%d/json/version" option.debugPort
        let request =
            Request.createUrl Get versionUriString
        let bodyStr =
            job {
                use! response = getResponse request
                let! bodyStr = Response.readBodyAsString response

                return bodyStr
            }
            |> run
        let versionInfoResult = Decode.Auto.fromString<VersionInfo>(bodyStr)
        match versionInfoResult with
        | Ok versionInfo ->
            printfn "%s" versionInfo.webSocketDebuggerUrl
            let uri = new Uri(versionInfo.webSocketDebuggerUrl)
            let webSock = new ClientWebSocket()
            webSock.Options.KeepAliveInterval <- TimeSpan.Zero

            do! webSock.ConnectAsync (uri, CancellationToken.None)

            return (Ok webSock)
        | Error err ->
            return (Error err)
    }

    type LaunchOption =
        { execPath: string
          arguments: string list
          debugPort: int }

    let launch (option: LaunchOption) = async {
        let userDataDir = Path.Combine (Path.GetTempPath(), "chronium-user-data")
        let userDataDirArg = sprintf "--user-data-dir=%s" userDataDir
        let debugPortArg = sprintf "--remote-debugging-port=%d" option.debugPort

        let arguments =
            option.arguments
            |> List.append defaultArgs
            |> List.append [userDataDirArg; debugPortArg]
            |> String.concat " "

        let startInfo = new ProcessStartInfo()
        startInfo.FileName <- option.execPath
        startInfo.Arguments <- arguments
        startInfo.UseShellExecute <- false
        let proc = new Process()
        proc.StartInfo <- startInfo
        proc.Start() |> ignore

        Thread.Sleep(1000)

        let! webSock = attach { debugPort = option.debugPort }
        match webSock with
        | Ok webSock ->
            return Ok { proc = proc; webSock = webSock; contexts = [ { id = None }] }
        | Error err ->
            return Error err
    }
