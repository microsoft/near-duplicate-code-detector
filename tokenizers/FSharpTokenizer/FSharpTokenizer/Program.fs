module FSharpTokenizer

open System
open System.IO
open System.IO.Compression
open System.Linq

open FSharp.Compiler.SourceCodeServices

open Newtonsoft.Json

let rec tokenizeLine (tokenizer : FSharpLineTokenizer) state (line : string) (tokens : _ list) =
    match tokenizer.ScanToken(state) with
    | Some tok, state ->
        let value = line.Substring(tok.LeftColumn, tok.RightColumn - tok.LeftColumn + 1)
        tokenizeLine tokenizer state line ({| Token = tok; Value = value |}::tokens)
    | None, state -> state, tokens

let rec tokenizeLines (sourceTok : FSharpSourceTokenizer) state count tokens lines =
    match lines with
    | line::lines ->
        let tokenizer = sourceTok.CreateLineTokenizer(line)
        let state, tokens = tokenizeLine tokenizer state line tokens
        tokenizeLines sourceTok state (count + 1) tokens lines
    | [] -> List.rev tokens

let tokenizeFile (filePath : string) =
    [
        use rawSource = new StreamReader(filePath)

        let mutable line = rawSource.ReadLine()
        while not (isNull line) do
            yield line
            line <- rawSource.ReadLine()
    ]
    |> tokenizeLines (FSharpSourceTokenizer([], Some filePath)) FSharpTokenizerLexState.Initial 1 []

let getFileIdentifierTokens (filepath : string) (onlyIdentifiers : bool) =
    let tokens = tokenizeFile filepath
    if onlyIdentifiers then
        tokens
        |> List.filter (fun t -> t.Token.CharClass = FSharpTokenCharKind.Identifier)
    else
        tokens
    |> List.map (fun t -> t.Value)

let getJsonForFile (filepath : string) (onlyIdentifiers : bool) (baseDir : string) : string =
    {|
        tokens   = getFileIdentifierTokens filepath onlyIdentifiers
        filename = Path.GetRelativePath(baseDir, filepath)
    |}
    |> JsonConvert.SerializeObject

let extractForProjectFolder (baseDir : string) (outputDir : string) (onlyIdentifiers : bool) (projectDir : string) =

    let projectDirName = Path.GetFileName(projectDir)

    use fileStream = File.Create(Path.Combine(outputDir, projectDirName + ".jsonl.gz"))
    use gzipStream = new GZipStream(fileStream, CompressionMode.Compress, false)
    use textStream = new StreamWriter(gzipStream)

    let allFiles = Directory.EnumerateFiles(projectDir, "*.fs", SearchOption.AllDirectories)

    for fileJson in allFiles.AsParallel().Select(fun f -> getJsonForFile f onlyIdentifiers baseDir) do
        textStream.WriteLine(fileJson)

[<EntryPoint>]
let main argv =
    if argv.Length <> 3 then
        Console.WriteLine("Usage <projectsFolder> <outputFolder> true|false");
        -1
    else
        Directory.EnumerateDirectories(argv.[0]).AsParallel()
        |> Seq.iter (extractForProjectFolder argv.[0] argv.[1] (Boolean.Parse(argv.[2])))

        0
