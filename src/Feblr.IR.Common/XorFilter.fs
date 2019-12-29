module Feblr.IR.Common.XorFilter

let murmur64 (h: uint64) =
    let mutable h = h
    h <- h ^^^ h >>> 33
    h <- h * 0xff51afd7ed558ccdUL
    h <- h ^^^ h >>> 33
    h <- h * 0xc4ceb9fe1a85ec53UL
    h <- h ^^^ h >>> 33
    h
