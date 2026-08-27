namespace Parquet.TypeProvider.Benchmarks

open System
open System.IO
open System.Collections.Generic
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Order
open Parquet
open Parquet.Schema
open Parquet.Data
open Parquet.Serialization
open Parquet.TypeProvider

[<CLIMutable>]
type CustomerRecord =
    { mutable Id: int32
      mutable Name: string
      mutable Balance: decimal
      mutable IsActive: bool }

[<MemoryDiagnoser>]
[<Orderer(SummaryOrderPolicy.FastestToSlowest)>]
type DeserializationBenchmark() =

    let mutable tempFile = ""

    [<Params(1000, 100000)>]
    member val RowCount = 0 with get, set

    [<GlobalSetup>]
    member this.Setup() =
        tempFile <- Path.Combine(Path.GetTempPath(), $"bench_{this.RowCount}_{Guid.NewGuid()}.parquet")
        let idField = DataField<int32>("Id", Nullable(false))
        let nameField = DataField<string>("Name", Nullable(false))
        let balanceField = DataField<decimal>("Balance", Nullable(false))
        let isActiveField = DataField<bool>("IsActive", Nullable(false))
        let schema = ParquetSchema(idField, nameField, balanceField, isActiveField)

        let count = this.RowCount
        let ids = Array.init count id
        let names = Array.init count (fun i -> $"Customer_{i}")
        let balances = Array.init count (fun i -> decimal i * 1.25m)
        let isActives = Array.init count (fun i -> i % 2 = 0)

        use fileStream = File.Create(tempFile)
        let writerTask = ParquetWriter.CreateAsync(schema, fileStream)
        let writer = writerTask.GetAwaiter().GetResult()
        use groupWriter = writer.CreateRowGroup()

        groupWriter.WriteAsync<int32>(idField, ids.AsMemory()).GetAwaiter().GetResult()
        groupWriter.WriteAsync(nameField, ArraySegment<string>(names)).GetAwaiter().GetResult()
        groupWriter.WriteAsync<decimal>(balanceField, balances.AsMemory()).GetAwaiter().GetResult()
        groupWriter.WriteAsync<bool>(isActiveField, isActives.AsMemory()).GetAwaiter().GetResult()

    [<GlobalCleanup>]
    member this.Cleanup() =
        if File.Exists(tempFile) then
            File.Delete(tempFile)

    [<Benchmark(Baseline = true)>]
    member this.ParquetSerializer_Reflection_Deserialize() =
        use fileStream = File.OpenRead(tempFile)
        let t = ParquetSerializer.DeserializeAsync<CustomerRecord>(fileStream :> Stream)
        let res = t.GetAwaiter().GetResult()
        res.Data.Count

    [<Benchmark>]
    member this.ParquetTypeProvider_RowSeq() =
        let rows = ParquetReaderCore.loadFromFile tempFile false
        let mutable count = 0

        for r in rows do
            count <- count + 1

        count

    [<Benchmark>]
    member this.ParquetTypeProvider_DirectColumnArray() =
        let ids = ParquetReaderCore.readColumnArray<int32> tempFile "Id"
        let balances = ParquetReaderCore.readColumnArray<decimal> tempFile "Balance"
        ids.Length + balances.Length

module Program =
    [<EntryPoint>]
    let main args =
        let summary = BenchmarkRunner.Run<DeserializationBenchmark>()
        0
