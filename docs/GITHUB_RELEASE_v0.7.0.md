# GitHub Release Text - v0.7.0

## Title

```text
School Inventory Manager v0.7.0 - First inventory mobile discovery workflow
```

## Tag

```text
v0.7.0
```

## Body

This release adds and stabilizes the **First Inventory / Discovery Mode** workflow.

The application can now support schools that start inventory from zero:

- create an empty first-inventory folder
- create rooms from the Android/mobile scanner
- add newly discovered items from mobile
- review new findings from the web app
- print QR labels for new findings
- use the approved inventory as a future baseline

### Added

- First inventory folder mode.
- Empty-folder behavior for first inventory.
- Mobile API endpoint to create rooms inside audit folders.
- Support for Android-driven room discovery.
- Documentation and checklist for v0.7.0 release.

### Fixed

- First inventory mode no longer copies existing rooms/items.
- Mobile create-room API route now follows existing convention:
  - `/api/mobile/audit-folders/...`
- EF1002 warnings in `SchemaUpgrade.cs` cleaned up safely.

### Release state

```text
Web app: 0 errors / 0 warnings
Android Scanner integration: tested with first inventory flow
```

### Recommended usage

This remains a school-testing release intended for local school networks.
Do not expose publicly without authentication, HTTPS and role-based access control.
