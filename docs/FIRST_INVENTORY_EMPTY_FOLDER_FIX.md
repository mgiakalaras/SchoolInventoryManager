# First inventory empty folder fix

## Problem

The first version of the "Πρώτη απογραφή / από μηδενική βάση" folder mode still copied existing rooms/items into the new audit folder.

That is wrong for a true first-inventory / discovery workflow.

## Correct behavior

When the user selects:

```text
Πρώτη απογραφή / από μηδενική βάση
```

the folder must start empty.

It should not copy:

- existing rooms
- existing expected item counts
- existing room sessions

Rooms and items will be added later from mobile/tablet or from the web app.

## Fixed behavior

### Annual audit

Still creates room sessions from the existing database.

### First inventory

Creates only the folder.

No room sessions are created.

The folder notes still include:

```text
[AuditMode:FirstInventory]
```

## Files

- `Pages/InventoryAudits/Create.cshtml`
- `Pages/InventoryAudits/Create.cshtml.cs`

## Next

```text
41b — Mobile API to create rooms from Android
```
