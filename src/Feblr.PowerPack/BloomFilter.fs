namespace Feblr.PowerPack

open System.Collections

module BloomFilter =
    type BloomFilter =
        { bits: BitArray
          capacity: int
          falsePositiveRate: float }

    with
        member this.Add () =
            ignore ()

        member this.Check () =
            ignore ()

    let create (capacity: int) (falsePositiveRate: float) : BloomFilter =
        let values: bool[] = [||]
        let bits = new BitArray(values)

        { bits = bits
          capacity = capacity
          falsePositiveRate = falsePositiveRate }
