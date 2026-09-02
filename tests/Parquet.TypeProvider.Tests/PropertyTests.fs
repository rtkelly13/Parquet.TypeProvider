namespace Parquet.TypeProvider.Tests

open System
open System.IO
open FsCheck
open FsCheck.Xunit
open Swensen.Unquote
open Parquet
open Parquet.Schema
open Parquet.Data
open Parquet.TypeProvider

#nowarn "3261"

module PropertyTests =

    [<Property(MaxTest = 100)>]
    let ``ParquetRow structural equality is reflexive for arbitrary batches`` (values: NonEmptyArray<int>) =
        let arr = values.Get

        let batch =
            { ColumnNames = [| "Value" |]
              ColumnTypes = [| typeof<int> |]
              Columns = [| arr :> Array |]
              RowCount = arr.Length }

        let rowA = ParquetRow(batch, 0)
        let rowB = ParquetRow(batch, 0)
        rowA = rowB

    [<Property(MaxTest = 50)>]
    let ``Integer array roundtrip preserves values, length, and ordering exactly`` (data: NonEmptyArray<int32>) =
        let input = data.Get
        let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")

        try
            let idField = DataField<int32>("Id", Nullable(false))
            let schema = ParquetSchema(idField)

            do
                use fileStream = File.Create(tempFile)

                let writer =
                    (ParquetWriter.CreateAsync(schema, fileStream)).GetAwaiter().GetResult()

                try
                    use groupWriter = writer.CreateRowGroup()
                    (groupWriter.WriteAsync<int32>(idField, input.AsMemory())).GetAwaiter().GetResult()
                finally
                    (writer.DisposeAsync().AsTask()).GetAwaiter().GetResult()

            let loaded =
                (ParquetReaderCore.readColumnArrayAsync<int32> tempFile "Id").GetAwaiter().GetResult()

            loaded = input
        finally
            if File.Exists tempFile then
                File.Delete tempFile

    [<Property(MaxTest = 50)>]
    let ``Decimal array roundtrip preserves precision and ordering`` (data: NonEmptyArray<decimal>) =
        let input = data.Get
        let tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.parquet")

        try
            let priceField = DataField<decimal>("Price", Nullable(false))
            let schema = ParquetSchema(priceField)

            do
                use fileStream = File.Create(tempFile)

                let writer =
                    (ParquetWriter.CreateAsync(schema, fileStream)).GetAwaiter().GetResult()

                try
                    use groupWriter = writer.CreateRowGroup()
                    (groupWriter.WriteAsync<decimal>(priceField, input.AsMemory())).GetAwaiter().GetResult()
                finally
                    (writer.DisposeAsync().AsTask()).GetAwaiter().GetResult()

            let loaded =
                (ParquetReaderCore.readColumnArrayAsync<decimal> tempFile "Price").GetAwaiter().GetResult()

            loaded = input
        finally
            if File.Exists tempFile then
                File.Delete tempFile
