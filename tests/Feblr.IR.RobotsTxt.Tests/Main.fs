module Feblr.IR.RobotsTxt.Tests

open Expecto
open Feblr.IR
open Feblr.IR.RobotsTxt

let robotsTxtMock = """
User-agent: Googlebot
Allow: /hello
Disallow: /world
Allow: /comment #comment
Sitemap: /sitemap
"""



[<Tests>]
let tests =
    testList "RobotsTxt"
        [ testCase "empty string should return RobotsTxt.empty" <| fun _ ->
            let robotsTxt = RobotsTxt.parse ""
            Expect.equal robotsTxt RobotsTxt.empty "should return empty robostxt"
          testCase "mock string should return expected RobotsTxt" <| fun _ ->
              let robotsTxt = RobotsTxt.parse robotsTxtMock

              let entry =
                  { userAgents = [ GoogleBot ]
                    directives =
                        [ Allow "/hello"
                          Disallow "/world"
                          Allow "/comment" ] }

              let expected =
                  { entries = [ entry ]
                    sitemap = [ "/sitemap" ] }

              Expect.equal robotsTxt expected "should return expected state"
          testCase "match should return expected state" <| fun _ ->
              let robotsTxt = RobotsTxt.parse robotsTxtMock
              let allowHello = RobotsTxt.isAllowed robotsTxt GoogleBot "/hello"
              let allowWorld2 = RobotsTxt.isAllowed robotsTxt GoogleBot "/world2"
              let allowWorld = RobotsTxt.isAllowed robotsTxt GoogleBot "/world"
              let allowComment = RobotsTxt.isAllowed robotsTxt GoogleBot "/comment"
              Expect.equal allowHello true "should return expected state"
              Expect.equal allowWorld2 true "should return expected state"
              Expect.equal allowWorld false "should return expected state"
              Expect.equal allowComment true "should return expected state" ]


[<EntryPoint>]
let main argv = Tests.runTestsInAssembly defaultConfig argv
