#!/usr/bin/env python3
"""
Generates reference Apache Parquet test datasets using PyArrow.
Used for binary compatibility and cross-engine testing in Parquet.TypeProvider.
"""

import os
from decimal import Decimal
import pyarrow as pa
import pyarrow.parquet as pq

def main():
    output_dir = os.path.join(os.path.dirname(__file__), "..", "tests", "data")
    os.makedirs(output_dir, exist_ok=True)

    # 1. Standard Primitive Data
    schema = pa.schema([
        ("id", pa.int32()),
        ("name", pa.string()),
        ("score", pa.float64()),
        ("is_active", pa.bool_()),
    ])

    data = {
        "id": [1, 2, 3, 4, 5],
        "name": ["Alice", "Bob", "Charlie", "David", "Eve"],
        "score": [98.5, 87.0, 92.3, 76.8, 99.1],
        "is_active": [True, True, False, True, False],
    }

    table = pa.Table.from_pydict(data, schema=schema)
    primitive_path = os.path.join(output_dir, "pyarrow_primitives.parquet")
    pq.write_table(table, primitive_path, compression="snappy")
    print(f"Generated {primitive_path}")

    # 2. Nullable / Optional Data
    null_schema = pa.schema([
        ("user_id", pa.int32()),
        ("nickname", pa.string()),
        ("bonus", pa.float64()),
    ])

    null_data = {
        "user_id": [10, 20, 30, 40],
        "nickname": ["ace", None, "chuck", None],
        "bonus": [500.0, None, 750.5, None],
    }

    null_table = pa.Table.from_pydict(null_data, schema=null_schema)
    null_path = os.path.join(output_dir, "pyarrow_nullables.parquet")
    pq.write_table(null_table, null_path, compression="snappy")
    print(f"Generated {null_path}")

    # 3. Comprehensive Physical/Logical Types
    all_schema = pa.schema([
        ("col_int32", pa.int32()),
        ("col_int64", pa.int64()),
        ("col_float32", pa.float32()),
        ("col_float64", pa.float64()),
        ("col_bool", pa.bool_()),
        ("col_string", pa.string()),
        ("col_decimal", pa.decimal128(10, 2)),
    ])

    all_data = {
        "col_int32": [100, 200, 300],
        "col_int64": [10000000000, 20000000000, 30000000000],
        "col_float32": [1.5, 2.5, 3.5],
        "col_float64": [10.12345, 20.23456, 30.34567],
        "col_bool": [True, False, True],
        "col_string": ["Red", "Green", "Blue"],
        "col_decimal": [Decimal("123.45"), Decimal("678.90"), Decimal("999.99")],
    }

    all_table = pa.Table.from_pydict(all_data, schema=all_schema)
    all_path = os.path.join(output_dir, "pyarrow_all_types.parquet")
    pq.write_table(all_table, all_path, compression="snappy")
    print(f"Generated {all_path}")

    # 4. Multi-RowGroup Dataset (5 row groups x 200 rows = 1,000 rows)
    rg_schema = pa.schema([
        ("record_id", pa.int32()),
        ("metric", pa.float64()),
    ])

    rg_data = {
        "record_id": list(range(1000)),
        "metric": [float(i) * 1.5 for i in range(1000)],
    }
    rg_table = pa.Table.from_pydict(rg_data, schema=rg_schema)
    rg_path = os.path.join(output_dir, "pyarrow_multi_rowgroup.parquet")
    pq.write_table(rg_table, rg_path, row_group_size=200, compression="snappy")
    print(f"Generated {rg_path}")

    # 5. Empty Dataset (0 rows)
    empty_table = pa.Table.from_pydict({"id": [], "name": []}, schema=pa.schema([("id", pa.int32()), ("name", pa.string())]))
    empty_path = os.path.join(output_dir, "pyarrow_empty.parquet")
    pq.write_table(empty_table, empty_path)
    print(f"Generated {empty_path}")

if __name__ == "__main__":
    main()
