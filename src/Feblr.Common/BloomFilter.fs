namespace Feblr.Common

open System.Collections

module BloomFilter =
    type BloomFilter =
        { bits: BitArray
          hashFuncCount: int
          capacity: int
          falsePositiveRate: float }

        member this.Add () =
            ignore ()

        member this.Check () =
            ignore ()

    let internal computeM (expectedCount: int) (falsePositiveRate: double) =
        let numerator = (double expectedCount) * (log (falsePositiveRate))
        let denominator = double (log 1.0) / (pown (log 2.0) 2)
        ceil(numerator / denominator) |> int

    let internal computeK (expectedCount: int) (falsePositiveProbability: double) =
        let m = computeM expectedCount falsePositiveProbability |> double
 
        let temp = (log 2.0) * m / (double expectedCount)
        round temp |> int
 
    let create (capacity: int) (falsePositiveRate: float) : BloomFilter =
        let m = max 1 (computeM capacity falsePositiveRate)
        let k = max 1 (computeK capacity falsePositiveRate)
        let bits = BitArray m

        { bits = bits
          hashFuncCount = k
          capacity = capacity
          falsePositiveRate = falsePositiveRate }
