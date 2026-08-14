# 04 - API Design & Usage Patterns

This document describes the public surface area and common usage patterns for `Parquet.TypeProvider`.

---

## 1. Type Provider Declaration

```fsharp
open Parquet.TypeProvider

// Type definition with sample file
type Orders = ParquetProvider<"../data/samples/orders.parquet">
```

---

## 2. Reading Data

### A. Synchronous File / Stream Loading
```fsharp
// From local path
let data: seq<Orders.Row> = Orders.Load("path/to/orders_2026.parquet")

// From open stream
use fileStream = File.OpenRead("path/to/orders_2026.parquet")
let dataFromStream = Orders.Load(fileStream)

for row in data do
    printfn $"Order ID: {row.OrderId}, Total: {row.Amount}"
```

### B. Async / Streaming Loading
```fsharp
task {
    let! rows = Orders.AsyncLoad("https://storage.blob.core.windows.net/data/orders.parquet")
    for row in rows do
        // Process rows asynchronously
        do! processRowAsync row
}
```

### C. Direct Columnar Access (Zero-Allocation Batch Reading)
For high-performance analytical scenarios that don't need row abstractions:
```fsharp
// Read columns directly as contiguous typed arrays
let orderIds: int64[] = Orders.Columns.OrderId("path/to/orders.parquet")
let amounts: decimal[] = Orders.Columns.Amount("path/to/orders.parquet")
```

---

## 3. Interactive Data Exploration (F# Interactive / Notebooks)

In `.fsx` or Jupyter / Polyglot Notebooks:
```fsharp
#r "nuget: Parquet.TypeProvider"

open Parquet.TypeProvider

type Trades = ParquetProvider<"trades_sample.parquet">
let df = Trades.Load("trades_sample.parquet")

df
|> Seq.filter (fun t -> t.Symbol = "AAPL")
|> Seq.averageBy (fun t -> float t.Price)
|> printfn "Average AAPL Price: %f"
```
