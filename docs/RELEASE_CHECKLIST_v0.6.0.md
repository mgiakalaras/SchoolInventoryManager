# Release Checklist — School Inventory Manager v0.6.0

## Before release

Run:

```powershell
dotnet build
```

Check locally:

```text
/
/InventoryAudits
/InventoryAudits/MobileFindings
/Labels/MobileFindings
/Scanner
/Help
```

Test workflow:

- Create/open audit folder.
- Open room.
- Scan from Android.
- Add new item from Android.
- Confirm new item appears in `/InventoryAudits/MobileFindings`.
- Edit new item and confirm Save returns to source page.
- Print QR for new findings from `/Labels/MobileFindings`.

## Commit

```powershell
git status
git add .
git commit -m "Prepare v0.6.0 release documentation"
git push origin main
```

## Tag

```powershell
git tag -a v0.6.0 -m "School Inventory Manager v0.6.0"
git push origin v0.6.0
```

## GitHub release

Release title:

```text
School Inventory Manager v0.6.0 — Android scanner and mobile findings workflow
```

Recommended assets:

- Source code zip/tar.gz from GitHub.
- Optional: deployment notes.
- Optional: screenshots later.

## After release

Redeploy production/Portainer stack from the tagged version or latest main after tag.

## Android second phase

After web release:

- Add README to Android repo.
- Add Android release notes.
- Tag current Android scanner version.
- Prepare APK release flow separately.
