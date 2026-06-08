# Release docs version fix

## Purpose

Correct the release documentation version from `v0.2.0` to `v0.6.0`.

## Reason

The project was already at `v0.5.0`, so the next feature release should be `v0.6.0`.

The previous `v0.2.0` references were incorrect.

## Files added/updated

- `README.md`
- `docs/RELEASE_NOTES_v0.6.0.md`
- `docs/RELEASE_CHECKLIST_v0.6.0.md`
- `docs/ROADMAP.md`

## Manual cleanup

If these old files exist from the previous documentation patch, delete them:

```powershell
Remove-Item .\docs\RELEASE_NOTES_v0.2.0.md -Force -ErrorAction SilentlyContinue
Remove-Item .\docs\RELEASE_CHECKLIST_v0.2.0.md -Force -ErrorAction SilentlyContinue
```

## Correct tag

```powershell
git tag -a v0.6.0 -m "School Inventory Manager v0.6.0"
git push origin v0.6.0
```
