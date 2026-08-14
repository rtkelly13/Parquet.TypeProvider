![Parquet.TypeProvider](docs/assets/logo.svg)

# Parquet.TypeProvider

[![Build & Test Status](https://github.com/rtkelly13/Parquet.TypeProvider/actions/workflows/ci.yml/badge.svg)](https://github.com/rtkelly13/Parquet.TypeProvider/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Parquet.TypeProvider.svg)](https://www.nuget.org/packages/Parquet.TypeProvider)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/rtkelly13/Parquet.TypeProvider/blob/main/LICENSE)

A high-performance **F# Type Provider** for Apache Parquet files, powered by [Parquet.Net](https://github.com/aloneguid/parquet-dotnet) low-level primitives.

Provides compile-time strongly typed schema inference and zero-boilerplate data exploration for F# scripts (`.fsx`), Jupyter notebooks, and production data pipelines.

---

## 🚀 Quick Look

```fsharp
#r "nuget: Parquet.TypeProvider"

open Parquet.TypeProvider

// Infer schema at compile time from a sample Parquet file
type Telemetry = ParquetProvider<"data/samples/telemetry_sample.parquet">

// Load actual data from file, stream, or cloud URL
let events = Telemetry.Load("data/production/telemetry_2026_08.parquet")

for event in events do
    printfn $"Device: {event.DeviceId}, Temp: {event.Temperature}, Timestamp: {event.Timestamp}"
```

---

<!-- BENCHMARK_TABLE_START -->
## ⚡ Performance & Benchmarks

Multi-scale benchmarks against reflection-based deserializers (1K, 10K, 100K, 1M rows) will be published here upon test suite execution. See [05 - Testing & Benchmarks](docs/05-TESTING-AND-BENCHMARKS.md) for methodology.
<!-- BENCHMARK_TABLE_END -->

---

## 🎯 Key Design Goals

1. **Schema-First Type Safety**: Instant IntelliSense and compile-time verification directly from `.parquet` sample files or schema definitions.
2. **Zero-Reflection Performance**: Backed by `Parquet.Net` v6 low-level column chunk readers and efficient array buffers.
3. **Idiomatic F# Experience**: First-class handling of `option<'T>`, standard F# primitives, timestamps, decimals, and records.
4. **Streaming & Large Data Support**: Lazy row group streaming and `IAsyncEnumerable<'Row>` support to process multi-gigabyte datasets without materializing all rows into memory.

---

## 📚 Documentation & Specifications

Detailed design documents are located in [`docs/`](docs/):

- 📖 [**Documentation Index**](docs/INDEX.md): Full documentation overview.
- 📐 [**01 - Architecture & Plan**](docs/01-ARCHITECTURE-PLAN.md): Design-time vs runtime structure, Type Provider SDK integration, and execution pipeline.
- 🗺️ [**02 - Roadmap & Milestones**](docs/02-ROADMAP.md): Step-by-step milestones from MVP to production release.
- 🔠 [**03 - Parquet to F# Type Mappings**](docs/03-TYPE-MAPPINGS.md): Parquet physical and logical types mapped to the F# type system.
- 🛠️ [**04 - API Design**](docs/04-API-DESIGN.md): Surface area design for synchronous, async, streaming, and column-oriented access.
- ⚡ [**05 - Testing & Benchmarking Strategy**](docs/05-TESTING-AND-BENCHMARKS.md): Multi-scale BenchmarkDotNet suite and validation harness.

---

## 🏗️ Repository Layout

```
Parquet.TypeProvider/
├── docs/                                  # Architectural specifications and plans
│   └── assets/                            # Brand assets and SVG logo
├── src/
│   ├── Parquet.TypeProvider.Runtime/      # Runtime library referenced by user apps
│   └── Parquet.TypeProvider.DesignTime/   # Design-time type generation component
├── tests/
│   ├── Parquet.TypeProvider.Tests/        # Schema inference and reader unit tests
│   └── Parquet.TypeProvider.Integration/  # Type provider end-to-end integration tests
├── benchmarks/
│   └── Parquet.TypeProvider.Benchmarks/   # Multi-scale BenchmarkDotNet suite
└── samples/
    └── Exploration.fsx                    # Interactive F# script sample
```

---

## 📄 License

MIT License. Copyright (c) 2026 Ryan Kelly.
