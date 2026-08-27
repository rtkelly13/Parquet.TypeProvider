namespace PackageConsumption

open System
open System.IO
open Parquet
open Parquet.Schema
open Parquet.Data
open Parquet.TypeProvider

module Program =

    let generateSampleFile (filePath: string) =
        let idField = DataField<int32>("Id", Nullable(false))
        let nameField = DataField<string>("Name", Nullable(false))
        let schema = ParquetSchema(idField, nameField)

        use stream = File.Create(filePath)
        let writer = ParquetWriter.CreateAsync(schema, stream).GetAwaiter().GetResult()
        use groupWriter = writer.CreateRowGroup()

        groupWriter.WriteAsync<int32>(idField, [| 1; 2; 3 |].AsMemory()).GetAwaiter().GetResult()
        groupWriter.WriteAsync(nameField, ArraySegment<string>([| "Alpha"; "Beta"; "Gamma" |])).GetAwaiter().GetResult()
        groupWriter.Dispose()
        writer.DisposeAsync().AsTask().GetAwaiter().GetResult()

    [<EntryPoint>]
    let main argv =
        let sampleFile = Path.Combine(AppContext.BaseDirectory, "sample.parquet")
        generateSampleFile sampleFile

        let rows = ParquetReaderCore.loadFromFile sampleFile false |> Seq.toArray

        if rows.Length <> 3 then
            failwithf "Expected 3 rows but got %d" rows.Length

        let names = ParquetReaderCore.readColumnArray<string> sampleFile "Name"

        if names.Length <> 3 || names.[0] <> "Alpha" then
            failwithf "Unexpected names array: %A" names

        printfn "PackageConsumption E2E verification passed successfully!"
        0
