# School Inventory Manager assets

Ο φάκελος `wwwroot/assets` κρατά τα βασικά γραφικά assets του project.

## Δομή

```text
wwwroot/assets/
  brand/
    sim-logo-lockup-dark.svg
    sim-logo-lockup-light.svg
    sim-mark-outline.svg

  icons/
    folder-audit.svg
    room-pin.svg
    qr-scan.svg
    sync-cloud.svg
    help-book.svg
    android-phone.svg

  mobile-scanner/
    scanner-app-icon.svg
    scanner-logo-dark.svg

  help/
    mobile-scanner-download-card.svg
    android-workflow-dark.svg
```

## Χρήση

- `brand/` για web app header, about, help, release notes.
- `icons/` για menu/cards/help pages.
- `mobile-scanner/` για την native Android scanner εφαρμογή.
- `help/` για σελίδες βοήθειας και download εφαρμογής.

## Σημείωση

Τα SVG είναι source assets και μπορούν να γίνουν export σε PNG όταν χρειαστεί.
Για native Android project θα χρειαστεί αργότερα παραγωγή adaptive icon assets.
