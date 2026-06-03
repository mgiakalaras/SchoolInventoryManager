# Audit index action buttons polish

## Problem

The action buttons in the audit folder list were visually loose and wrapped unevenly:

- Άνοιγμα
- Καθαρισμός scans
- Διαγραφή

The red delete button appeared alone on a new line and the whole action area looked messy.

## Change

The action area now uses a compact button grid:

- Άνοιγμα φακέλου spans the full width.
- Καθαρισμός and Διαγραφή sit underneath as two equal buttons.
- On smaller widths, the buttons stack vertically.
- The action column has a stable width so the table does not look broken.

## File

- `Pages/InventoryAudits/Index.cshtml`
