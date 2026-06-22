# School Inventory Manager v0.7.1

## Release type

Server test / UI polishing release.

## Main change

This release prepares the web app for testing the new **full-width command center dashboard** on the server.

## Highlights

- Full-width responsive dashboard.
- New operational cockpit layout.
- Left workflow rail.
- Central dashboard/stats area.
- Right status feed.
- More visible first-inventory workflow.
- More direct links to:
  - Πρώτη απογραφή
  - Νέα ευρήματα
  - QR νέων ευρημάτων
  - Φάκελοι απογραφής
  - Android Scanner
- Visual category tiles with basic automatic equipment icons.
- Better use of wide screens.
- Collapses to simpler layout on tablets/mobiles.

## Technical notes

- No database schema changes.
- No migrations.
- No API changes.
- No Android changes.
- Main changed file:
  - `Pages/Index.cshtml`
- Version file:
  - `Utilities/AppVersion.cs`

## Upgrade / server test checklist

1. Commit current dashboard patch if not already committed.
2. Apply this release-prep patch.
3. Build locally:

```powershell
dotnet build
```

4. Commit release prep:

```powershell
git add Utilities/AppVersion.cs docs/RELEASE_NOTES_v0.7.1.md docs/GITHUB_RELEASE_v0.7.1.md docs/RELEASE_CHECKLIST_v0.7.1.md PATCH47A_WEB_RELEASE_PREP_v0.7.1.txt
git commit -m "Prepare web release v0.7.1"
git push origin main
```

5. Tag:

```powershell
git tag v0.7.1
git push origin v0.7.1
```

6. Update/redeploy the Portainer stack.
7. Test the dashboard on:
   - desktop monitor
   - laptop
   - tablet/mobile width
