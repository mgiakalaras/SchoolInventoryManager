# First Inventory Mode foundation

## Purpose

Add the first UI foundation for supporting schools that have never done a proper inventory before.

This introduces the idea of two audit folder modes:

1. `AnnualAudit`
2. `FirstInventory`

## User-facing labels

### Ετήσιος έλεγχος υπάρχουσας βάσης

Used when the school already has an inventory database and wants to verify:

- found items
- missing items
- wrong-room items
- new findings

### Πρώτη απογραφή / από μηδενική βάση

Used when the school starts from zero.

The goal is to allow:

- creating rooms from mobile
- adding items from mobile
- reviewing/validating from PC
- printing QR labels
- printing the final inventory folder
- using the result as the baseline for the next school year

## Storage approach in this patch

No database schema change yet.

The selected mode is stored inside `InventoryAuditFolder.Notes` using a marker:

```text
[AuditMode:AnnualAudit]
```

or:

```text
[AuditMode:FirstInventory]
```

This is intentionally safe for the first step.

A future schema patch can add a real `AuditMode` column after the workflow stabilizes.

## Behavior

If `FirstInventory` is selected:

- `IncludeEmptyRooms` is forced to `true`
- The folder is allowed to start with few or no rooms
- The folder is marked as first inventory through the notes marker

## Next steps

```text
41b — Web/mobile API: create room from Android
41c — Android UI: + Νέος χώρος
41d — Web review page for first inventory
41e — QR labels by room for first inventory
41f — Finalize as baseline
```
