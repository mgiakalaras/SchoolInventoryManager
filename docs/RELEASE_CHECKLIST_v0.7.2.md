# Release checklist - v0.7.2

## Build

```powershell
dotnet build
```

## Local test

```powershell
dotnet run
```

Check:

```text
/
 /Scanner
 /About
```

## Git

```powershell
git status

git add Pages/Shared/_Layout.cshtml
git add Pages/Scanner/Index.cshtml
git add Pages/About/Index.cshtml
git add wwwroot/css/sim-wide-layout.css
git add Utilities/AppVersion.cs
git add docs/PATCH48A_LAYOUT_SCANNER_ABOUT_POLISH.md
git add PATCH48A_LAYOUT_SCANNER_ABOUT_POLISH.txt
git add docs/RELEASE_NOTES_v0.7.2.md
git add docs/GITHUB_RELEASE_v0.7.2.md
git add docs/RELEASE_CHECKLIST_v0.7.2.md
git add PATCH49A_WEB_RELEASE_PREP_v0.7.2.txt

git commit -m "Prepare web release v0.7.2"
git push origin main

git tag v0.7.2
git push origin v0.7.2
```

## Server / Portainer

- [ ] Backup database/volume.
- [ ] Redeploy stack.
- [ ] Open dashboard.
- [ ] Open Scanner page.
- [ ] Open About page.
- [ ] Confirm Scanner release link works.
- [ ] Hard refresh browser if old layout appears.
