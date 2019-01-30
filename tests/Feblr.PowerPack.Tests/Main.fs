module Feblr.PowerPack.Tests
open Expecto
open Feblr.PowerPack

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
