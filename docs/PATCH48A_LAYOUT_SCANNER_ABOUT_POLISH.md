# Patch 48a - Layout, Scanner and About polish

## Purpose

Addresses three UI/UX issues:

1. The Scanner page should send users directly to the Android Scanner GitHub Releases.
2. The About page needed a cleaner, more professional layout.
3. The application was wasting too much horizontal space because the global layout remained centered/narrow.

## Files

```text
Pages/Shared/_Layout.cshtml
Pages/Scanner/Index.cshtml
Pages/About/Index.cshtml
wwwroot/css/sim-wide-layout.css
docs/PATCH48A_LAYOUT_SCANNER_ABOUT_POLISH.md
PATCH48A_LAYOUT_SCANNER_ABOUT_POLISH.txt
```

## What changed

### Global layout

Adds:

```text
wwwroot/css/sim-wide-layout.css
```

and includes it after `site.css`.

This CSS makes the main container adaptive/full-width:

- wide screens use the full operational surface
- small screens remain readable
- common page shells stop being locked in a centered narrow layout

### Scanner page

The Scanner page now has a clear release/download call-to-action:

```text
https://github.com/mgiakalaras/SchoolInventoryScanner/releases
```

It explains:

- GitHub Release APK is for sideload/install
- AAB is for Google Play Console
- server URL
- health check
- scanner workflow

### About page

Reworked as a full-width project presentation:

- version strip
- project purpose
- school-network context
- operational direction
- features
- technologies
- creator/repository links

## No database changes

No migrations.
No models.
No services.
No API changes.

## Test

```powershell
dotnet build
dotnet run
```

Check:

```text
/
 /Scanner
 /About
```
