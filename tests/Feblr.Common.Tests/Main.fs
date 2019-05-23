module Feblr.Common.Tests
open Expecto
open Feblr.Common

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
