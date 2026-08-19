namespace Parquet.TypeProvider.Tests

open System
open System.IO
open System.Threading.Tasks
open Xunit
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
                Assert.Equal(4, fields.Length)
                Assert.Equal("Id", fields.[0].Name)
                Assert.Equal(typeof<int32>, fields.[0].ClrType)
                Assert.Equal("Name", fields.[1].Name)
                Assert.Equal(typeof<string>, fields.[1].ClrType)
                Assert.Equal("Price", fields.[2].Name)
                Assert.Equal(typeof<decimal>, fields.[2].ClrType)
                Assert.Equal("Description", fields.[3].Name)
                Assert.True(fields.[3].IsNullable)
            finally
                if File.Exists tempFile then File.Delete tempFile
        }

    [<Fact>]
    member _.``ParquetReaderCore reads rows and respects options correctly``() =
        task {
            let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")
            try
                do! TestHelper.createTestParquetFile tempFile

                let rows = ParquetReaderCore.loadFromFile tempFile true |> Seq.toArray
                Assert.Equal(5, rows.Length)

                // Row 0 checks
                let r0 = rows.[0]
                Assert.Equal(1, r0.GetTypedValue<int32>(0))
                Assert.Equal("Apple", r0.GetTypedValue<string>(1))
                Assert.Equal(1.50m, r0.GetTypedValue<decimal>(2))
                Assert.Equal(Some "Fresh fruit", r0.GetOptionalValue<string>(3))

                // Row 1 checks (null description -> None)
                let r1 = rows.[1]
                Assert.Equal(2, r1.GetTypedValue<int32>(0))
                Assert.Equal("Banana", r1.GetTypedValue<string>(1))
                Assert.Equal(None, r1.GetOptionalValue<string>(3))
            finally
                if File.Exists tempFile then File.Delete tempFile
        }

    [<Fact>]
    member _.``ParquetReaderCore extracts contiguous column arrays``() =
        task {
            let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")
            try
                do! TestHelper.createTestParquetFile tempFile

                let ids = ParquetReaderCore.readColumnArray<int32> tempFile "Id"
                Assert.Equal<int32[]>([| 1; 2; 3; 4; 5 |], ids)

                let names = ParquetReaderCore.readColumnArray<string> tempFile "Name"
                Assert.Equal<string[]>([| "Apple"; "Banana"; "Cherry"; "Date"; "Elderberry" |], names)

                let prices = ParquetReaderCore.readColumnArray<decimal> tempFile "Price"
                Assert.Equal<decimal[]>([| 1.50m; 0.80m; 3.25m; 4.00m; 2.10m |], prices)
            finally
                if File.Exists tempFile then File.Delete tempFile
        }

    [<Fact>]
    member _.``ParquetReaderCore can ingest PyArrow generated datasets``() =
        let dataDir = Path.Combine(__SOURCE_DIRECTORY__, "..", "data")
        let pyarrowFile = Path.Combine(dataDir, "pyarrow_nullables.parquet")
        if File.Exists pyarrowFile then
            let rows = ParquetReaderCore.loadFromFile pyarrowFile true |> Seq.toArray
            Assert.Equal(4, rows.Length)
            let r0 = rows.[0]
            Assert.Equal(Some 10, r0.GetOptionalValue<int32>(0))
            Assert.Equal(Some "ace", r0.GetOptionalValue<string>(1))
            Assert.Equal(Some 500.0, r0.GetOptionalValue<double>(2))

            let r1 = rows.[1]
            Assert.Equal(Some 20, r1.GetOptionalValue<int32>(0))
            Assert.Equal(None, r1.GetOptionalValue<string>(1))
            Assert.Equal(None, r1.GetOptionalValue<double>(2))

    [<Fact>]
    member _.``ParquetRow structural equality and comparison works for FSharp collections``() =
        let batch1 =
            {
                ColumnNames = [| "Id"; "Name" |]
                ColumnTypes = [| typeof<int>; typeof<string> |]
                Columns = [| [| 1; 2 |] :> Array; [| "A"; "B" |] :> Array |]
                RowCount = 2
            }
        let batch2 =
            {
                ColumnNames = [| "Id"; "Name" |]
                ColumnTypes = [| typeof<int>; typeof<string> |]
                Columns = [| [| 1; 2 |] :> Array; [| "A"; "B" |] :> Array |]
                RowCount = 2
            }

        let row1A = ParquetRow(batch1, 0)
        let row1B = ParquetRow(batch2, 0)
        let row2 = ParquetRow(batch1, 1)

        Assert.Equal(row1A, row1B)
        Assert.NotEqual(row1A, row2)

        // Set / distinct compatibility
        let set = Set.ofList [ row1A; row1B; row2 ]
        Assert.Equal(2, set.Count)
