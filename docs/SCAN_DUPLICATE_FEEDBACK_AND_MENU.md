# Patch 43b - Menu link and duplicate scan feedback

## Menu

Adds the new First Inventory review page to the main menu:

```text
Απογραφή → Πρώτη απογραφή
```

Route:

```text
/InventoryAudits/FirstInventory
```

## Duplicate scan feedback

Updates the mobile scan API response so it can tell the Android app that an item/QR has already been scanned in the current room session.

The API now returns:

```json
{
  "alreadyScanned": true
}
```

when the scan log already exists.

## Messages

Examples:

```text
Το αντικείμενο έχει ήδη σαρωθεί σε αυτόν τον χώρο.
Το άγνωστο QR έχει ήδη σαρωθεί σε αυτόν τον χώρο.
```

## Files

- `Pages/Shared/_Layout.cshtml`
- `Pages/Api/Mobile/RoomSessionScan.cshtml.cs`

## No schema change

No database change.
