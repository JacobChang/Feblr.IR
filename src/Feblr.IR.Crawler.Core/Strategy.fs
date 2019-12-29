module Feblr.IR.Crawler.Core.Strategy

type Strategy =
  | DomainLimit of int

let check (strategy: Strategy) = ignore
