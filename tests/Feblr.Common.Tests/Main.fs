module Feblr.Common.Tests
open Expecto
open Feblr.Common


[<Tests>]
let tests =
  let bloomFilter = BloomFilter.create 10000 0.1
  testList "BloomFilter" [
    testCase "should return false for inexist item" <| fun _ ->
      Expect.equal (bloomFilter.has "hello") false "should return false"
    testCase "should return true for exist item" <| fun _ ->
      bloomFilter.add "world"
      Expect.equal (bloomFilter.has "world") true "should return true"
  ]


[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly defaultConfig argv
