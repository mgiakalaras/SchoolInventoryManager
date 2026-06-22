# School Inventory Manager v0.7.2

## Release type

Server test / UI layout polishing release.

## Main purpose

This release prepares the central web app for server testing after the new adaptive layout direction.

It focuses on:

- reducing wasted horizontal space,
- improving the Scanner page,
- improving the About page,
- adding a global wide-layout CSS layer.

## Highlights

### Adaptive wide layout

- Adds `wwwroot/css/sim-wide-layout.css`.
- Loads it from `_Layout.cshtml`.
- Makes the main app container use available screen width.
- Keeps small-screen/mobile readability.
- Helps operational pages, tables, forms and dashboards avoid the old narrow centered layout.

### Scanner page polish

- Reworked `/Scanner`.
- Adds clear link to the Android Scanner GitHub Releases:

```text
https://github.com/mgiakalaras/SchoolInventoryScanner/releases
```

- Explains APK vs AAB:
  - APK for GitHub/sideload install.
  - AAB for Google Play Console.
- Shows suggested server URL and health-check endpoint.
- Shows the scanner workflow from web folder to field scan.

### About page polish

- Reworked `/About`.
- Adds version strip.
- Explains project purpose.
- Shows feature areas.
- Shows technology stack.
- Shows creator/repository links.
- Uses a more professional full-width layout.

## Changed files

```text
Pages/Shared/_Layout.cshtml
Pages/Scanner/Index.cshtml
Pages/About/Index.cshtml
wwwroot/css/sim-wide-layout.css
Utilities/AppVersion.cs
```

## No database changes

This release does not include:

- database schema changes,
- migrations,
- API changes,
- Android changes.

## Server test checklist

After redeploy:

- [ ] Open `/`.
- [ ] Open `/Scanner`.
- [ ] Scanner release link opens Android Scanner GitHub Releases.
- [ ] Open `/About`.
- [ ] Check wide screen layout.
- [ ] Check laptop layout.
- [ ] Check mobile/tablet width.
- [ ] Check common list/table pages for excessive centering.
- [ ] If old CSS appears, hard refresh browser/cache.
