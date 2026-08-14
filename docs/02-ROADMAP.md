# 02 - Roadmap & Milestones

## Phase 1: Foundation & Design-Time Schema Inference (Milestone 1)
- [ ] Initialize repository structure with `FSharp.TypeProviders.SDK`.
- [ ] Implement sample file loading and resolution (relative paths, absolute paths).
- [ ] Implement Parquet schema inspection from `Parquet.Net` metadata.
- [ ] Generate basic `ProvidedTypeDefinition` types with scalar properties (primitives, strings, decimals).
- [ ] Wire basic quotation expressions for typed row instantiation.

## Phase 2: Runtime Engine & Columnar Execution (Milestone 2)
- [ ] Implement `ParquetRowContext` columnar batch representation.
- [ ] Implement synchronous `.Load(filePath)` and `.Load(stream)` row enumeration.
- [ ] Support `option<'T>` for optional / nullable Parquet columns.
- [ ] Implement `Parquet.TypeProvider.Tests` covering all core Parquet primitive data types.

## Phase 3: Advanced Types & Streaming (Milestone 3)
- [ ] Async & streaming APIs: `.AsyncLoad(...)` and `IAsyncEnumerable<'Row>`.
- [ ] Complex type support:
  - Timestamp units (Millis, Micros).
  - Enums and custom logical types.
  - Lists and nested structures.
- [ ] Static column accessor overloads (columnar arrays without creating row objects).

## Phase 4: Integration Testing, CI, & Packaging (Milestone 4)
- [ ] Multi-targeting and multi-IDE testing (Visual Studio, Rider, VS Code / Ionide).
- [ ] F# Interactive (`.fsx`) and Polyglot Notebook verification.
- [ ] Performance benchmarking vs naive reflection readers (`BenchmarkDotNet`).
- [ ] GitHub Actions CI workflow with NuGet packaging and SourceLink.
