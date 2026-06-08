# Mobile create room API

## Purpose

Add the first API needed for **Πρώτη απογραφή / Discovery Mode**.

When a school starts from zero, the Android app must be able to create rooms during the first inventory process.

## Endpoint

```http
POST /api/mobile/folders/{folderId}/rooms/create
```

## JSON request

```json
{
  "name": "Αίθουσα Α1"
}
```

Optional:

```json
{
  "name": "Αίθουσα Α1",
  "sortOrder": 10
}
```

## Behavior

The endpoint:

1. Checks that the audit folder exists.
2. Blocks finalized folders.
3. Looks for an existing room with the same name.
4. Creates the room if it does not exist.
5. Adds the room to the folder as an `InventoryAuditRoomSession`.
6. Starts with zero expected/found/missing items.
7. Returns room and session details.

## Duplicate protection

If the room already exists inside the same folder, it does not create a duplicate session.

## First inventory use

This endpoint is designed mainly for folders marked with:

```text
[AuditMode:FirstInventory]
```

but it currently works for any non-finalized folder, because adding a new room during an audit can also be useful.

## No database schema change

This patch uses existing:

- `Rooms`
- `InventoryAuditFolders`
- `InventoryAuditRoomSessions`

## Next step

```text
41c — Android UI: + Νέος χώρος
```
