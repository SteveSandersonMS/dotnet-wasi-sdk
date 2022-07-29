open System
open System.IO

let mimeType path =
    try
        MimeTypes.MimeTypeMap.GetMimeType(path)
    with
    | _ -> "application/octet-stream"

[<EntryPoint>]
let main _ =
    let path = Environment.GetEnvironmentVariable("PATH_INFO")
    let buffer = File.ReadAllBytes(path)

    printfn "Content-Type: %s" (mimeType path)
    printfn "Content-Length: %d" buffer.Length
    printfn ""

    use stm = Console.OpenStandardOutput()
    stm.Write(buffer, 0, buffer.Length)
    stm.Flush()

    0
