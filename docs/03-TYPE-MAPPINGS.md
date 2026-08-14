# 03 - Parquet to F# Type Mappings

This document defines how Apache Parquet physical and logical types are mapped to the F# type system.

---

## 1. Primitive & Scalar Mappings

| Parquet Physical / Logical Type | Required Column (F#) | Optional Column (`PreferOption=true`) |
| :--- | :--- | :--- |
| `BOOLEAN` | `bool` | `bool option` |
| `INT32` | `int32` | `int32 option` |
| `INT64` | `int64` | `int64 option` |
| `INT96` (Legacy Timestamp) | `System.DateTime` | `System.DateTime option` |
| `FLOAT` | `float32` | `float32 option` |
| `DOUBLE` | `float` | `float option` |
| `BYTE_ARRAY` (String / UTF8) | `string` | `string option` |
| `BYTE_ARRAY` (Raw Binary) | `byte[]` | `byte[] option` |
| `FIXED_LEN_BYTE_ARRAY` (Guid) | `System.Guid` | `System.Guid option` |
| `DECIMAL` | `decimal` | `decimal option` |
| `TIMESTAMP_MILLIS` / `MICROS` | `System.DateTime` | `System.DateTime option` |
| `DATE` | `System.DateOnly` / `System.DateTime` | `System.DateOnly option` |
| `TIME_MILLIS` / `MICROS` | `System.TimeSpan` | `System.TimeSpan option` |

---

## 2. Nullability & Option Handling

Parquet fields declare repetition levels:
- `REQUIRED`: Mapped directly to the non-nullable F# type `'T`.
- `OPTIONAL`:
  - When `PreferOption = true` (default): Mapped to `'T option`.
  - When `PreferOption = false`: Value types mapped to `Nullable<'T>`, reference types mapped to nullable references.

---

## 3. Nested Structures & Collections (Phase 3)

| Parquet Structure | F# Representation |
| :--- | :--- |
| `LIST<T>` | `'T list` / `'T[]` |
| `MAP<K, V>` | `Map<'K, 'V>` / `IDictionary<'K, 'V>` |
| `STRUCT` | Generated Nested Provided Type (`Parent.NestedStruct`) |
