# Web 41f - SchemaUpgrade EF1002 warning cleanup

## Purpose

Clean the 3 EF1002 warnings in:

```text
Data/SchemaUpgrade.cs
```

## Warnings fixed

- `SqlQueryRaw` with interpolated SQL
- `SqlQueryRaw` with interpolated SQL
- `ExecuteSqlRaw` with interpolated SQL

## Fix strategy

The helper `AddColumnIfMissing(...)` now:

1. Validates table names and column names as safe SQL identifiers.
2. Uses `SqlQuery(...)` for parameterized queries where values are data.
3. Builds the `ALTER TABLE` SQL only after identifier validation.
4. Quotes identifiers for the final SQLite `ALTER TABLE`.
5. Avoids interpolated strings directly inside `ExecuteSqlRaw(...)`.

## Functional behavior

No schema behavior change.

The same missing columns are still added safely when needed.

## File

- `Data/SchemaUpgrade.cs`
