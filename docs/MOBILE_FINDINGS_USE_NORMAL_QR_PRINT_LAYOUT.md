# Mobile findings QR labels use the normal QR layout

## Problem

The QR labels for new/mobile findings used a separate print layout.

That layout produced bad output:
- wrong placement
- sparse/odd label flow
- duplicate-looking pages
- not matching the already calibrated normal QR labels page

## Fix

`Pages/Labels/MobileFindings.cshtml` now uses the same print layout/classes as:

```text
Pages/Labels/Qr.cshtml
```

Shared/calibrated behavior:
- Typotrust TL2111
- A4
- 21 labels per page
- 3 columns x 7 rows
- 70mm x 42.3mm
- column-first filling
- QR 30mm x 30mm
- top print offset -18mm

## Files

- `Pages/Labels/MobileFindings.cshtml`

## No backend change

No database change.
No model change.
No Android change.
