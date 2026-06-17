# First Inventory Review Page

## Purpose

Adds a dedicated web page for reviewing a **Πρώτη απογραφή / από μηδενική βάση** folder.

Route:

```text
/InventoryAudits/FirstInventory
```

Optional selected folder:

```text
/InventoryAudits/FirstInventory?folderId=1
```

## What it shows

- First-inventory folders.
- Selected folder summary.
- Rooms created during first inventory.
- Mobile-created items per room.
- Pending review count.
- Missing serial count.
- Active items ready for QR.
- Empty rooms.
- Links to:
  - folder details
  - mobile findings
  - QR labels for new findings
  - item edit page

## Filtering

Items are linked to a folder using the existing notes marker:

```text
AuditFolderId: {folderId}
```

This marker is already written by the Android/mobile quick-add API.

## No schema change

This patch only adds a new Razor Page.

No database schema changes.
No Android changes.
