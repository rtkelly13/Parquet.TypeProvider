#!/usr/bin/env python3
"""
Generates reference Apache Parquet test datasets using PyArrow.
Used for binary compatibility and cross-engine testing in Parquet.TypeProvider.
"""

import os
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
        ("tags", pa.list_(pa.string())),
    ])

    data = {
        "id": [1, 2, 3, 4, 5],
        "name": ["Alice", "Bob", "Charlie", "David", "Eve"],
        "score": [98.5, 87.0, 92.3, 76.8, 99.1],
        "is_active": [True, True, False, True, False],
        "tags": [["eng", "lead"], ["dev"], ["qa"], ["design"], ["product"]],
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

if __name__ == "__main__":
    main()
