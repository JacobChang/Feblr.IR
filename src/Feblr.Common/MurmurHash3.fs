// http://blog.stermon.com/articles/2014/04/06/bloom-filters-fnv1a-murmur3-and-leaked-emails.html

namespace Feblr.Common

open System

module MurmurHash3 =
    let internal stb s = Text.Encoding.UTF8.GetBytes(s = s)

    let hash data seed =
      let rotl x r = (x <<< r) ||| (x >>> (32 - r))
      let fmix h =
        h
        |> fun x -> x ^^^ (x >>> 16)
        |> fun x -> x * 0x85ebca6bu
        |> fun x -> x ^^^ (x >>> 13)
        |> fun x -> x * 0xc2b2ae35u
        |> fun x -> x ^^^ (x >>> 16)
      let getblock b i = BitConverter.ToUInt32(value = b, startIndex = i)
      
      let data    = data  |> stb
      let len     = data |> Array.length
      let nblocks = len >>> 2
      let h1      = seed
      let c1      = 0xcc9e2d51u
      let c2      = 0x1b873593u
      
      let rec body h = function
        | i when i < nblocks ->
          let k1 =
            getblock data (i * 4)
            |> fun x -> x * c1
            |> fun x -> rotl x 15
            |> fun x -> x * c2
          let h' =
            h ^^^ k1
            |> fun x -> rotl x 13
            |> fun x -> x * 5u + 0xe6546b64u
          body h' (i+1)
        | _ -> h
      let h1' = body h1 0
      
      // tail
      let tail = nblocks * 4
      let rec tail' (k,h) = function
        | 0 -> h
        | 1 -> 
          let k' =
            k ^^^ (uint32 data.[tail])
            |> fun x -> x * c1
            |> fun x -> rotl x 15
            |> fun x -> x * c2
          let h' = h ^^^ k'
          tail' (k',h') (0)
        | i ->
          let k' =
            (uint32 data.[tail + (i - 1)]) <<< (1 <<< (i + 1))
            |> fun x -> k ^^^ x
          tail' (k',h) (i-1)
      let h1'' = tail' (0u,h1') (len &&& 3)
      
      h1'' ^^^ (uint32 len)
      |> fun x -> x |> fmix
