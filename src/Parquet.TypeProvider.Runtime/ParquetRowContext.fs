namespace Parquet.TypeProvider

open System
open System.Collections
open System.Collections.Generic

#nowarn "3261"

/// Represents the internal column batch backing a sequence of erased Parquet rows.
type ParquetColumnBatch =
    { ColumnNames: string[]
      ColumnTypes: Type[]
      Columns: Array[]
      RowCount: int }

/// Lightweight erased row representation wrapping a column batch and row index.
/// Implements structural equality and comparison for idiomatic F# behavior.
[<StructuredFormatDisplay("{Display}")>]
type ParquetRow(batch: ParquetColumnBatch, rowIndex: int) =
    member _.RowIndex = rowIndex
    member _.Batch = batch

    /// Retrieves an untyped column value by 0-based column index.
    member _.GetValue(colIndex: int) : obj =
        if colIndex < 0 || colIndex >= batch.Columns.Length then
            raise (IndexOutOfRangeException $"Column index {colIndex} is out of bounds.")

        let arr = batch.Columns.[colIndex]
        let v = arr.GetValue(rowIndex)

        if obj.ReferenceEquals(v, null) then
            null
        else
            let vType = v.GetType()

            if vType.IsGenericType && vType.GetGenericTypeDefinition() = typedefof<Nullable<_>> then
                let hasVal = vType.GetProperty("HasValue").GetValue(v) :?> bool

                if hasVal then
                    vType.GetProperty("Value").GetValue(v)
                else
                    null
            else
                v

    /// Retrieves a strongly-typed column value by 0-based column index.
    member this.GetTypedValue<'T>(colIndex: int) : 'T =
        if colIndex < 0 || colIndex >= batch.Columns.Length then
            raise (IndexOutOfRangeException $"Column index {colIndex} is out of bounds.")

        let arr = batch.Columns.[colIndex]

        match box arr with
        | :? ('T[]) as typedArr -> typedArr.[rowIndex]
        | _ ->
            let raw = arr.GetValue(rowIndex)

            if obj.ReferenceEquals(raw, null) then
                Unchecked.defaultof<'T>
            else
                let rawType = raw.GetType()

                if
                    rawType.IsGenericType
                    && rawType.GetGenericTypeDefinition() = typedefof<Nullable<_>>
                then
                    let hasVal = rawType.GetProperty("HasValue").GetValue(raw) :?> bool

                    if hasVal then
                        rawType.GetProperty("Value").GetValue(raw) :?> 'T
                    else
                        Unchecked.defaultof<'T>
                else
                    raw :?> 'T

    /// Retrieves an optional typed column value by 0-based column index.
    member this.GetOptionalValue<'T>(colIndex: int) : 'T option =
        if colIndex < 0 || colIndex >= batch.Columns.Length then
            raise (IndexOutOfRangeException $"Column index {colIndex} is out of bounds.")

        let arr = batch.Columns.[colIndex]

        match box arr with
        | :? ('T option[]) as optArr -> optArr.[rowIndex]
        | _ ->
            let raw = arr.GetValue(rowIndex)

            if obj.ReferenceEquals(raw, null) then
                None
            else
                let rawType = raw.GetType()

                if
                    rawType.IsGenericType
                    && rawType.GetGenericTypeDefinition() = typedefof<Nullable<_>>
                then
                    let hasVal = rawType.GetProperty("HasValue").GetValue(raw) :?> bool

                    if hasVal then
                        let v = rawType.GetProperty("Value").GetValue(raw) :?> 'T
                        Some v
                    else
                        None
                else
                    Some(raw :?> 'T)

    member this.Display =
        let fields =
            batch.ColumnNames
            |> Array.mapi (fun i name -> $"{name} = {this.GetValue(i)}")
            |> String.concat "; "

        $"{{ {fields} }}"

    override this.ToString() = this.Display

    override this.Equals(other: obj) =
        match other with
        | :? ParquetRow as r ->
            if this.Batch.Columns.Length <> r.Batch.Columns.Length then
                false
            else
                let rec check i =
                    if i >= this.Batch.Columns.Length then
                        true
                    else
                        let v1 = this.GetValue(i)
                        let v2 = r.GetValue(i)
                        if Object.Equals(v1, v2) then check (i + 1) else false

                check 0
        | _ -> false

    override this.GetHashCode() =
        let mutable hash = 17

        for i = 0 to batch.Columns.Length - 1 do
            let v = this.GetValue(i)
            hash <- hash * 23 + (if obj.ReferenceEquals(v, null) then 0 else v.GetHashCode())

        hash

    interface IComparable with
        member this.CompareTo(other: obj) =
            match other with
            | :? ParquetRow as r ->
                let rec compareCol i =
                    if i >= this.Batch.Columns.Length then
                        0
                    else
                        let v1 = this.GetValue(i) :?> IComparable
                        let v2 = r.GetValue(i)

                        let c =
                            if obj.ReferenceEquals(v1, null) && obj.ReferenceEquals(v2, null) then
                                0
                            elif obj.ReferenceEquals(v1, null) then
                                -1
                            elif obj.ReferenceEquals(v2, null) then
                                1
                            else
                                v1.CompareTo(v2)

                        if c <> 0 then c else compareCol (i + 1)

                compareCol 0
            | _ -> invalidArg "other" "Cannot compare ParquetRow with different type."

    interface IStructuralEquatable with
        member this.Equals(other: obj, comparer: IEqualityComparer) =
            match other with
            | :? ParquetRow as r ->
                if this.Batch.Columns.Length <> r.Batch.Columns.Length then
                    false
                else
                    let rec check i =
                        if i >= this.Batch.Columns.Length then
                            true
                        else
                            let v1 = this.GetValue(i)
                            let v2 = r.GetValue(i)
                            if comparer.Equals(v1, v2) then check (i + 1) else false

                    check 0
            | _ -> false

        member this.GetHashCode(comparer: IEqualityComparer) =
            let mutable hash = 17

            for i = 0 to batch.Columns.Length - 1 do
                let v = this.GetValue(i)
                hash <- hash * 23 + comparer.GetHashCode(v)

            hash
