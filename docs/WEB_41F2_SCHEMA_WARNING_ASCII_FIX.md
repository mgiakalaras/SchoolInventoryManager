# Web 41f2 - SchemaUpgrade EF1002 warning fix

This patch avoids the previous encoding problem.

## Files

- `Apply_Patch41f2.cmd`
- `tools/Apply_Patch41f2_SchemaUpgradeWarnings_ASCII.ps1`

## Run

From the web app project root:

```powershell
.\Apply_Patch41f2.cmd
dotnet build
```

## What it changes

It edits only the `AddColumnIfMissing(...)` method in `Data/SchemaUpgrade.cs`.

It creates a backup before editing:

```text
Data\SchemaUpgrade.cs.bak_41f2_YYYYMMDD_HHMMSS
```
