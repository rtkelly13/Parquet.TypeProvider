namespace Parquet.TypeProvider

open System
open System.IO
open System.Collections.Generic
open System.Threading.Tasks
open FSharp.Control
open Parquet
open Parquet.Schema
open Parquet.Data

#nowarn "3261"
#nowarn "3511"

/// Strongly typed schema field representation for F# Type Provider consumption.
type ParquetSchemaField =
    {
        Name: string
        ClrType: Type
        IsNullable: bool
    }

module ParquetReaderCore =

    /// Normalizes Parquet.Net 6 internal ClrTypes to standard .NET types for F# consumption.
    let normalizeClrType (t: Type) : Type =
        if t = typeof<ReadOnlyMemory<char>> then typeof<string>
        else t

    /// Reads metadata and extracts schema field definitions from a Parquet stream asynchronously.
    let readSchemaFieldsAsync (stream: Stream) : Task<ParquetSchemaField[]> =
        task {
            let! reader = ParquetReader.CreateAsync(stream, null, true)
            return
                reader.Schema.GetDataFields()
                |> Array.map (fun f ->
                    {
                        Name = f.Name
                        ClrType = normalizeClrType f.ClrType
                        IsNullable = f.IsNullable
                    }
                )
        }

    /// Reads metadata and extracts schema field definitions from a file path.
    let readSchemaFieldsFromFile (filePath: string) : ParquetSchemaField[] =
        use fileStream = File.OpenRead(filePath)
        let t = readSchemaFieldsAsync fileStream
        t.GetAwaiter().GetResult()

    /// Reads a single column chunk from a row group asynchronously into a boxed typed array.
    let readColumnDataAsync (groupReader: ParquetRowGroupReader) (field: DataField) (rowCount: int) : Task<Array> =
        task {
            let clrType = normalizeClrType field.ClrType
            if clrType = typeof<string> then
                let memory = Array.zeroCreate<string> rowCount
                let! _ = groupReader.ReadAsync(field, memory.AsMemory())
                return memory :> Array
            elif clrType = typeof<byte[]> then
                let memory = Array.zeroCreate<byte[]> rowCount
                let! _ = groupReader.ReadAsync(field, memory.AsMemory())
                return memory :> Array
            elif clrType = typeof<Guid> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<Guid>> rowCount
                    let! _ = groupReader.ReadAsync<Guid>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<Guid> rowCount
                    let! _ = groupReader.ReadAsync<Guid>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<int32> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<int32>> rowCount
                    let! _ = groupReader.ReadAsync<int32>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<int32> rowCount
                    let! _ = groupReader.ReadAsync<int32>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<int64> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<int64>> rowCount
                    let! _ = groupReader.ReadAsync<int64>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<int64> rowCount
                    let! _ = groupReader.ReadAsync<int64>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<double> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<double>> rowCount
                    let! _ = groupReader.ReadAsync<double>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<double> rowCount
                    let! _ = groupReader.ReadAsync<double>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<float32> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<float32>> rowCount
                    let! _ = groupReader.ReadAsync<float32>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<float32> rowCount
                    let! _ = groupReader.ReadAsync<float32>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<bool> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<bool>> rowCount
                    let! _ = groupReader.ReadAsync<bool>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<bool> rowCount
                    let! _ = groupReader.ReadAsync<bool>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<decimal> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<decimal>> rowCount
                    let! _ = groupReader.ReadAsync<decimal>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<decimal> rowCount
                    let! _ = groupReader.ReadAsync<decimal>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<DateTime> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<DateTime>> rowCount
                    let! _ = groupReader.ReadAsync<DateTime>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<DateTime> rowCount
                    let! _ = groupReader.ReadAsync<DateTime>(field, memory.AsMemory())
                    return memory :> Array
            elif clrType = typeof<TimeSpan> then
                if field.IsNullable then
                    let memory = Array.zeroCreate<Nullable<TimeSpan>> rowCount
                    let! _ = groupReader.ReadAsync<TimeSpan>(field, memory.AsMemory())
                    return memory :> Array
                else
                    let memory = Array.zeroCreate<TimeSpan> rowCount
                    let! _ = groupReader.ReadAsync<TimeSpan>(field, memory.AsMemory())
                    return memory :> Array
            else
                let fallbackArray = Array.CreateInstance(clrType, rowCount)
                return fallbackArray
        }

    /// Converts a raw column chunk into an F# option array if nullable and preferred.
    let private transformColumnArray (rawArray: Array) (field: DataField) (preferOption: bool) (rowCount: int) : Array =
        if preferOption && field.IsNullable then
            let normType = normalizeClrType field.ClrType
            let optArray = Array.CreateInstance(typedefof<option<_>>.MakeGenericType(normType), rowCount)
            for r = 0 to rowCount - 1 do
                let v = rawArray.GetValue(r)
                if obj.ReferenceEquals(v, null) then
                    optArray.SetValue(null, r)
                else
                    let vType = v.GetType()
                    if vType.IsGenericType && vType.GetGenericTypeDefinition() = typedefof<Nullable<_>> then
                        let hasVal = vType.GetProperty("HasValue").GetValue(v) :?> bool
                        if hasVal then
                            let underlying = vType.GetProperty("Value").GetValue(v)
                            let someObj = typedefof<option<_>>.MakeGenericType(normType).GetMethod("Some").Invoke(null, [| underlying |])
                            optArray.SetValue(someObj, r)
                        else
                            optArray.SetValue(null, r)
                    else
                        let someObj = typedefof<option<_>>.MakeGenericType(normType).GetMethod("Some").Invoke(null, [| v |])
                        optArray.SetValue(someObj, r)
            optArray
        else
            rawArray

    /// Asynchronously streams row groups from a Stream using the FSharp.Control taskSeq computation expression.
    let readRowsStream (stream: Stream) (preferOption: bool) : IAsyncEnumerable<ParquetRow> =
        taskSeq {
            let! reader = ParquetReader.CreateAsync(stream, null, true)
            let rawFields = reader.Schema.GetDataFields()
            let columnNames = rawFields |> Array.map (fun f -> f.Name)
            let columnTypes = rawFields |> Array.map (fun f -> normalizeClrType f.ClrType)

            for i = 0 to reader.RowGroupCount - 1 do
                use groupReader = reader.OpenRowGroupReader(i)
                let rowCount = int groupReader.RowCount

                let columnArrays = Array.zeroCreate<Array> rawFields.Length
                for fIdx = 0 to rawFields.Length - 1 do
                    let field = rawFields.[fIdx]
                    let! rawArray = readColumnDataAsync groupReader field rowCount
                    columnArrays.[fIdx] <- transformColumnArray rawArray field preferOption rowCount

                let batch =
                    {
                        ColumnNames = columnNames
                        ColumnTypes = columnTypes
                        Columns = columnArrays
                        RowCount = rowCount
                    }

                for r = 0 to rowCount - 1 do
                    yield ParquetRow(batch, r)
        }

    /// Asynchronously streams rows from a file path using taskSeq.
    let loadFromFileAsync (filePath: string) (preferOption: bool) : IAsyncEnumerable<ParquetRow> =
        taskSeq {
            use fileStream = File.OpenRead(filePath)
            for row in readRowsStream fileStream preferOption do
                yield row
        }

    /// Reads all row groups into a sequence of ParquetRow elements (synchronous wrapper over taskSeq).
    let readRows (stream: Stream) (preferOption: bool) : seq<ParquetRow> =
        readRowsStream stream preferOption |> TaskSeq.toSeq

    /// Loads a Parquet file from path as a sequence of rows.
    let loadFromFile (filePath: string) (preferOption: bool) : seq<ParquetRow> =
        let fileStream = File.OpenRead(filePath)
        readRows fileStream preferOption

    /// Reads a specific column as a contiguous typed array from a file asynchronously.
    let readColumnArrayAsync<'T> (filePath: string) (columnName: string) : Task<'T[]> =
        task {
            use fileStream = File.OpenRead(filePath)
            let! reader = ParquetReader.CreateAsync(fileStream, null, true)
            let field =
                reader.Schema.GetDataFields()
                |> Array.find (fun f -> String.Equals(f.Name, columnName, StringComparison.OrdinalIgnoreCase))

            let mutable totalRows = 0
            for i = 0 to reader.RowGroupCount - 1 do
                use gr = reader.OpenRowGroupReader(i)
                totalRows <- totalRows + int gr.RowCount

            let result = Array.zeroCreate<'T> totalRows
            let mutable offset = 0

            for i = 0 to reader.RowGroupCount - 1 do
                use groupReader = reader.OpenRowGroupReader(i)
                let rowCount = int groupReader.RowCount
                let! colData = readColumnDataAsync groupReader field rowCount
                let typedData = colData :?> 'T[]
                Array.Copy(typedData, 0, result, offset, rowCount)
                offset <- offset + rowCount

            return result
        }

    /// Reads a specific column as a contiguous typed array from a file.
    let readColumnArray<'T> (filePath: string) (columnName: string) : 'T[] =
        let t = readColumnArrayAsync<'T> filePath columnName
        t.GetAwaiter().GetResult()
