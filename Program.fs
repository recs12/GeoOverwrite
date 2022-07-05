open System.IO
open System
open Deedle


type Gravure =
    { PathGeo: string
      Line: int
      Text: string }


module ManageGEOFiles =
    open System.Diagnostics

    let GetGravuresGeoFiles (csv_path: string) : array<string * list<int * string>> =
        let df = Frame.ReadCsv(csv_path, separators = ",")

        let combinedPath =
            [ for row in df.Rows.Values do
                  let folderPath = (row.Get "FolderPath") :?> string
                  let fileName = (row.Get "Filename") :?> string
                  yield Path.Combine(folderPath, fileName) ]

        df.AddColumn("GeoPathCombined", combinedPath)

        let slicedDF =
            df.Columns.[[ "GeoPathCombined"
                          "LineNumber"
                          "OverwritingText" ]]

        let collectionOfRecords =
            (slicedDF.Rows.Values)
            |> Seq.map (fun f ->
                { PathGeo = (f.Get "GeoPathCombined") :?> string
                  Line = (f.Get "LineNumber") :?> int
                  Text = (f.Get "OverwritingText") :?> string })

        let groupedColGeo =
            query {
                for p in collectionOfRecords do
                    groupBy p.PathGeo into g
                    select (g.Key, [ for i in g -> i.Line, i.Text ])
            }
            |> Seq.toArray
        groupedColGeo


    let createAlteredContent (listOfElements: string []) (lineAndMark: list<int * string>) : string =

        let mutable modifiedArray: array<string> = listOfElements

        for line, mark in lineAndMark do
            let index = line - 1
            let beforeGravure: string [] = modifiedArray.[0 .. index - 1]
            let afterGravure: string [] = modifiedArray.[index + 1 ..]
            let newList: string [] = Array.append [| mark |] afterGravure
            modifiedArray <- Array.append beforeGravure newList
            printfn "%s" (String.replicate  10 "-")
            printfn "[+] Line %i -> %s" line mark

        String.concat "\n" modifiedArray


    let replaceGravuresGeoFile (linesAndGravures: string * list<int * string>):string*string =
        let (_geoFile:string), (LinesAndMarks:list<int*string>) = linesAndGravures
        match File.Exists(_geoFile) with
        | false -> 
            printfn "[!FILENOTFOUND] File: %*s" 10 _geoFile
            ("","")
        | true ->
            let (listOfElements: string[]) = File.ReadAllLines(_geoFile)
            let ncontent = createAlteredContent listOfElements LinesAndMarks
            printfn "[MODIFIED] File: %*s" 10 _geoFile
            (_geoFile, ncontent)


    let OverWriteGeo (file:String) (content:string):unit =
        if (file,content) <> ("","") then File.WriteAllText(file, content)


    [<EntryPoint>]
    let main args =
        printfn "[INPUT] Enter the path of the *.csv file with the new gravures: (to Cancel/Stop: Ctrl+C)"
        let stopwatch = Stopwatch()
        stopwatch.Start();
        Console.ReadLine()
        |> GetGravuresGeoFiles
        |> Array.map replaceGravuresGeoFile
        |> Array.Parallel.iter (fun (file,content) -> OverWriteGeo file content)
        stopwatch.Stop()
        printfn "Process Duration: %d ms" stopwatch.ElapsedMilliseconds
        0

