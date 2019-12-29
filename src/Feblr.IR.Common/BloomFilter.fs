module Feblr.IR.Common.BloomFilter

open System.Collections
open MurmurHash3

type BloomFilter =
    { bits: BitArray
      hashFuncCount: int32
      capacity: int32
      falsePositiveRate: float }

    member this.add (data: string) =
        for i in 1 .. this.hashFuncCount + 1 do
            let hash = MurmurHash3.hash data (i |> uint32)
            let index = hash % (this.bits.Length |> uint32)
            this.bits.Set(index |> int, true)

    member this.has (data: string) =
        let mutable found = true
        for i in 1 .. this.hashFuncCount + 1 do
            let hash = MurmurHash3.hash data (i |> uint32)
            let index = hash % (this.bits.Length |> uint32)
            let flag = this.bits.Get(index |> int)
            found <- found && flag

        found

let internal computeM (n: int) (p: double) = -(double n) * (log p) / (pown (log 2.0) 2)

let internal computeK (m: double) (n: int) (p: double) =
    m / (double n) * (log 2.0)
    |> round
    |> int

let create (capacity: int) (falsePositiveRate: float): BloomFilter =
    let m = max 1.0 (computeM capacity falsePositiveRate)
    let k = max 1 (computeK m capacity falsePositiveRate)

    let bits =
        BitArray
            (m
             |> ceil
             |> int)

    { bits = bits
      hashFuncCount = k
      capacity = capacity
      falsePositiveRate = falsePositiveRate }
