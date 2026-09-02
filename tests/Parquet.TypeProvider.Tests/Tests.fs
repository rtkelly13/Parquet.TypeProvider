namespace Parquet.TypeProvider.Tests

open System
open System.IO
open System.Threading.Tasks
open FSharp.Control
open Xunit
open Swensen.Unquote
open Parquet
open Parquet.Schema
open Parquet.Data
open Parquet.TypeProvider

#nowarn "3261"

module TestHelper =

    let createTestParquetFile (filePath: string) =
        task {
            let idField = DataField<int32>("Id", Nullable(false))
            let nameField = DataField<string>("Name", Nullable(false))
            let priceField = DataField<decimal>("Price", Nullable(false))
            let descField = DataField<string>("Description", Nullable(true))
            let schema = ParquetSchema(idField, nameField, priceField, descField)

            let ids = [| 1; 2; 3; 4; 5 |]
            let names = [| "Apple"; "Banana"; "Cherry"; "Date"; "Elderberry" |]
            let prices = [| 1.50m; 0.80m; 3.25m; 4.00m; 2.10m |]
            let descriptions = [| "Fresh fruit"; null; "Sweet cherry"; null; "Berry fruit" |]

            use fileStream = File.Create(filePath)
            let! writer = ParquetWriter.CreateAsync(schema, fileStream)
            use w = writer
            use groupWriter = w.CreateRowGroup()

            do! groupWriter.WriteAsync<int32>(idField, ids.AsMemory())
            do! groupWriter.WriteAsync(nameField, ArraySegment<string>(names))
            do! groupWriter.WriteAsync<decimal>(priceField, prices.AsMemory())
            do! groupWriter.WriteAsync(descField, ArraySegment<string>(descriptions))
        }

type SchemaAndReaderTests() =

    [<Fact>]
    member _.``ParquetReaderCore can read schema fields accurately``() =
        task {
            let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")

            try
                do! TestHelper.createTestParquetFile tempFile

                let fields = ParquetReaderCore.readSchemaFieldsFromFile tempFile
                test <@ fields.Length = 4 @>
                test <@ fields.[0].Name = "Id" @>
                test <@ fields.[0].ClrType = typeof<int32> @>
                test <@ fields.[1].Name = "Name" @>
                test <@ fields.[1].ClrType = typeof<string> @>
                test <@ fields.[2].Name = "Price" @>
                test <@ fields.[2].ClrType = typeof<decimal> @>
                test <@ fields.[3].Name = "Description" @>
                test <@ fields.[3].IsNullable = true @>
            finally
                if File.Exists tempFile then
                    File.Delete tempFile
        }

    [<Fact>]
    member _.``ParquetReaderCore reads rows and respects options correctly``() =
        task {
            let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")

            try
                do! TestHelper.createTestParquetFile tempFile

                let rows = ParquetReaderCore.loadFromFile tempFile true |> Seq.toArray
                test <@ rows.Length = 5 @>

                // Row 0 checks
                let r0 = rows.[0]
                test <@ r0.GetTypedValue<int32>(0) = 1 @>
                test <@ r0.GetTypedValue<string>(1) = "Apple" @>
                test <@ r0.GetTypedValue<decimal>(2) = 1.50m @>
                test <@ r0.GetOptionalValue<string>(3) = Some "Fresh fruit" @>

                // Row 1 checks (null description -> None)
                let r1 = rows.[1]
                test <@ r1.GetTypedValue<int32>(0) = 2 @>
                test <@ r1.GetTypedValue<string>(1) = "Banana" @>
                test <@ r1.GetOptionalValue<string>(3) = None @>
            finally
                if File.Exists tempFile then
                    File.Delete tempFile
        }

    [<Fact>]
    member _.``ParquetReaderCore streams rows asynchronously via taskSeq``() =
        task {
            let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")

            try
                do! TestHelper.createTestParquetFile tempFile

                let stream = ParquetReaderCore.loadFromFileAsync tempFile true
                let! rows = TaskSeq.toArrayAsync stream
                test <@ rows.Length = 5 @>
                test <@ rows.[0].GetTypedValue<string>(1) = "Apple" @>
                test <@ rows.[4].GetTypedValue<string>(1) = "Elderberry" @>
            finally
                if File.Exists tempFile then
                    File.Delete tempFile
        }

    [<Fact>]
    member _.``ParquetReaderCore extracts contiguous column arrays asynchronously``() =
        task {
            let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")

            try
                do! TestHelper.createTestParquetFile tempFile

                let! ids = ParquetReaderCore.readColumnArrayAsync<int32> tempFile "Id"
                test <@ ids = [| 1; 2; 3; 4; 5 |] @>

                let! names = ParquetReaderCore.readColumnArrayAsync<string> tempFile "Name"
                test <@ names = [| "Apple"; "Banana"; "Cherry"; "Date"; "Elderberry" |] @>

                let! prices = ParquetReaderCore.readColumnArrayAsync<decimal> tempFile "Price"
                test <@ prices = [| 1.50m; 0.80m; 3.25m; 4.00m; 2.10m |] @>
            finally
                if File.Exists tempFile then
                    File.Delete tempFile
        }

    [<Fact>]
    member _.``ParquetReaderCore decodes all rich types from PyArrow``() =
        let dataDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "data")
        let pyarrowFile = Path.Combine(dataDir, "pyarrow_all_types.parquet")

        if File.Exists pyarrowFile then
            let rows = ParquetReaderCore.loadFromFile pyarrowFile false |> Seq.toArray
            test <@ rows.Length = 3 @>

            let r0 = rows.[0]
            test <@ r0.GetTypedValue<int32>(0) = 100 @>
            test <@ r0.GetTypedValue<int64>(1) = 10000000000L @>
            test <@ r0.GetTypedValue<float32>(2) = 1.5f @>
            test <@ r0.GetTypedValue<double>(3) = 10.12345 @>
            test <@ r0.GetTypedValue<bool>(4) = true @>
            test <@ r0.GetTypedValue<string>(5) = "Red" @>
            test <@ r0.GetTypedValue<decimal>(6) = 123.45m @>

    [<Fact>]
    member _.``ParquetReaderCore streams multi-rowgroup dataset sequentially via taskSeq``() =
        task {
            let dataDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "data")
            let pyarrowFile = Path.Combine(dataDir, "pyarrow_multi_rowgroup.parquet")

            if File.Exists pyarrowFile then
                let stream = ParquetReaderCore.loadFromFileAsync pyarrowFile false
                let! rows = TaskSeq.toArrayAsync stream
                test <@ rows.Length = 1000 @>
                test <@ rows.[0].GetTypedValue<int32>(0) = 0 @>
                test <@ rows.[999].GetTypedValue<int32>(0) = 999 @>

                let! metricCol = ParquetReaderCore.readColumnArrayAsync<double> pyarrowFile "metric"
                test <@ metricCol.Length = 1000 @>
                test <@ metricCol.[0] = 0.0 @>
                test <@ metricCol.[999] = 999.0 * 1.5 @>
        }

    [<Fact>]
    member _.``ParquetReaderCore handles empty dataset gracefully``() =
        let dataDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "data")
        let pyarrowFile = Path.Combine(dataDir, "pyarrow_empty.parquet")

        if File.Exists pyarrowFile then
            let rows = ParquetReaderCore.loadFromFile pyarrowFile false |> Seq.toArray
            test <@ Array.isEmpty rows @>

    [<Fact>]
    member _.``ParquetReaderCore can ingest PyArrow generated datasets``() =
        let dataDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "data")
        let pyarrowFile = Path.Combine(dataDir, "pyarrow_nullables.parquet")

        if File.Exists pyarrowFile then
            let rows = ParquetReaderCore.loadFromFile pyarrowFile true |> Seq.toArray
            test <@ rows.Length = 4 @>
            let r0 = rows.[0]
            test <@ r0.GetOptionalValue<int32>(0) = Some 10 @>
            test <@ r0.GetOptionalValue<string>(1) = Some "ace" @>
            test <@ r0.GetOptionalValue<double>(2) = Some 500.0 @>

            let r1 = rows.[1]
            test <@ r1.GetOptionalValue<int32>(0) = Some 20 @>
            test <@ r1.GetOptionalValue<string>(1) = None @>
            test <@ r1.GetOptionalValue<double>(2) = None @>

    [<Fact>]
    member _.``ParquetRow structural equality and comparison works for FSharp collections``() =
        let batch1 =
            { ColumnNames = [| "Id"; "Name" |]
              ColumnTypes = [| typeof<int>; typeof<string> |]
              Columns = [| [| 1; 2 |] :> Array; [| "A"; "B" |] :> Array |]
              RowCount = 2 }

        let batch2 =
            { ColumnNames = [| "Id"; "Name" |]
              ColumnTypes = [| typeof<int>; typeof<string> |]
              Columns = [| [| 1; 2 |] :> Array; [| "A"; "B" |] :> Array |]
              RowCount = 2 }

        let row1A = ParquetRow(batch1, 0)
        let row1B = ParquetRow(batch2, 0)
        let row2 = ParquetRow(batch1, 1)

        test <@ row1A = row1B @>
        test <@ row1A <> row2 @>

        // Set / distinct compatibility
        let set = Set.ofList [ row1A; row1B; row2 ]
        test <@ set.Count = 2 @>
