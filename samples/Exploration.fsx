#r "nuget: Parquet.Net, 6.1.0"
#r "../src/Parquet.TypeProvider.Runtime/bin/Debug/net9.0/Parquet.TypeProvider.Runtime.dll"
#r "../src/Parquet.TypeProvider.DesignTime/bin/Debug/net9.0/Parquet.TypeProvider.DesignTime.dll"

open System
open System.IO
open Parquet
open Parquet.Schema
open Parquet.TypeProvider

// Generate a sample Parquet file for interactive exploration
let sampleFile = Path.Combine(__SOURCE_DIRECTORY__, "sample_telemetry.parquet")

if not (File.Exists sampleFile) then
    let idField = DataField<int32>("DeviceId", Nullable(false))
    let sensorField = DataField<string>("SensorType", Nullable(false))
    let tempField = DataField<double>("Temperature", Nullable(false))
    let schema = ParquetSchema(idField, sensorField, tempField)

    let ids = [| 101; 102; 103; 104; 105 |]
    let sensors = [| "Thermocouple"; "Infrared"; "Thermocouple"; "RTD"; "Infrared" |]
    let temps = [| 23.5; 45.2; 21.8; 67.4; 52.1 |]

    use stream = File.Create(sampleFile)
    let writer = ParquetWriter.CreateAsync(schema, stream).GetAwaiter().GetResult()
    use groupWriter = writer.CreateRowGroup()
    groupWriter.WriteAsync<int32>(idField, ids.AsMemory()).GetAwaiter().GetResult()
    groupWriter.WriteAsync(sensorField, ArraySegment<string>(sensors)).GetAwaiter().GetResult()
    groupWriter.WriteAsync<double>(tempField, temps.AsMemory()).GetAwaiter().GetResult()
    printfn $"Created sample file: {sampleFile}"

// Strongly-typed Type Provider definition
type Telemetry = ParquetProvider<"sample_telemetry.parquet">

// Read and explore using idiomatic F# pipeline
let data = Telemetry.Load(sampleFile)

printfn "=== Reading Telemetry Rows ==="

for row in data do
    printfn $"Device: {row.DeviceId} | Sensor: {row.SensorType} | Temp: {row.Temperature}°C"

// High-speed direct columnar extraction
printfn "=== Column Extraction ==="
let allTemperatures: double[] = Telemetry.Columns.Temperature(sampleFile)
let avgTemp = Array.average allTemperatures
printfn $"Average Temperature across all devices: {avgTemp:F2}°C"
