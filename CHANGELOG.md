# Changelog

All notable changes to School Inventory Manager are documented in this file.

---

## v0.5.0 - QR inventory labels and audit folders

### Added

- Added QR identity foundation for inventory items.
- Added automatic `AssetCode` generation for inventory items.
- Added `QrToken` support.
- Added automatic backfill for existing inventory items.
- Added Application Base URL setting for QR code generation.
- Added Settings action to use the current application URL as QR base URL.
- Added read-only QR item card page:
  - `/Items/Qr/{code}`
- Added short QR redirect route:
  - `/q/{code}`
- Added QR label center:
  - `/Labels/Qr`
- Added QR label filtering by:
  - search
  - room
  - category
  - active/inactive status
- Added QR label pagination.
- Added selected QR label printing.
- Added Typotrust TL2111 label layout:
  - A4
  - 21 labels per page
  - 3 columns x 7 rows
  - 70mm x 42.3mm
- Added PNG QR rendering for more reliable browser print scaling.
- Added QR scan page:
  - `/Audit/Scan`
- Added manual QR lookup by AssetCode or full QR URL.
- Added camera scanning using browser BarcodeDetector where supported.
- Added recent scans list.
- Added room-based QR audit page:
  - `/Audit/Room`
- Added live counters for room QR audit:
  - expected
  - found
  - missing
  - wrong room
  - unknown QR
- Added inventory audit folder module.
- Added inventory audit folder creation.
- Added automatic room sessions for rooms with active items.
- Added saved QR room audit progress.
- Added `InventoryAuditScanLogs` table.
- Added scan log persistence per room session.
- Added reload of saved room scan progress after refresh.
- Added room finalization.
- Added reopening room audit while folder is still draft.
- Added clearing room scans while not finalized.
- Added folder finalization.
- Added blocking of folder finalization while room sessions remain open.
- Added school settings snapshot for audit folders.
- Added `SchoolType` snapshot on inventory audit folders.
- Added menu reorganization for clearer workflows.

### Changed

- QR codes now use short `/q/{code}` URLs for better scan reliability.
- QR label print layout was calibrated for Typotrust TL2111 sheets.
- QR labels include inventory date from School Settings.
- Inventory audit folder creation now pulls school data from School Settings instead of asking the user to type it manually.
- Audit folder creation now shows school details as read-only snapshot data.
- Audit folder details now show school type.
- Dashboard links were updated to include QR labels, QR scan and room audit workflow.
- Menu structure was reorganized into clearer groups:
  - βασικά
  - απογραφή QR
  - τεχνικά / απόθεμα
  - διαχείριση

### Fixed

- Fixed QR label rendering issue where inline SVG could overlap the label text.
- Fixed QR label print layout showing 2 columns instead of 3 in browser print preview.
- Fixed QR label column flow for Typotrust TL2111 labels.
- Fixed print top offset calibration for the selected label sheet.
- Fixed missing `SchoolType` column issue on existing databases through schema upgrade.
- Fixed QR item page Razor CSS escaping issues.
- Fixed QR PNG color overload build issue with the installed QRCoder version.

### Notes

- Mobile/tablet QR URL opening works when the device is on the same network/VPN and `ApplicationBaseUrl` is configured correctly.
- In-browser camera scanning on mobile may require HTTPS depending on browser/security policy.
- External QR scanner / camera apps can open the QR URL directly.
- The app remains in school testing phase.
- Recommended usage remains local network only.
- Backup is recommended before updating existing deployments.

---

## v0.4.0 - Technical identity, spare parts stock and dashboard refresh

### Added

- Added optional technical specifications for equipment.
- Added conditional technical specs section for:
  - Η/Υ
  - PC
  - Laptop
  - Mini PC
  - Server
  - Διαδραστικός πίνακας / διαδραστική οθόνη
  - OPS / embedded mini PC
- Added technical specs fields:
  - CPU
  - RAM
  - memory type
  - storage
  - storage type
  - GPU
  - operating system
  - license / COA
  - network
  - OPS / mini PC module
  - technical notes
- Added technical reference library.
- Added reference entries for processors, RAM, storage, disk types, GPUs, operating systems and power supplies.
- Added manual entry support for missing technical references.
- Added spare parts / consumables stock module.
- Added stock quantity tracking.
- Added minimum stock threshold.
- Added low stock indication.
- Added archive/reactivate support for stock entries.
- Added spare part usage workflow.
- Added usage logging when a spare part is used on a device.
- Added optional link between stock usage and an inventory item.
- Added snapshot of the target device at the time of use.
- Added automatic stock quantity decrease after usage.
- Added spare part usage history page.
- Added technical audit report.
- Added detection for technical devices based on equipment/category names.
- Added clean printable technical audit report.
- Added Excel export for technical audit.
- Added clean printable spare parts stock report.
- Added Excel export for spare parts stock.
- Added dashboard widgets for:
  - spare parts stock
  - low stock items
  - technical audit
  - technical issue preview
  - documents and workflows
- Added Scoro-style mosaic dashboard layout.
- Added visual dashboard refresh with compact KPI cards and independent widgets.

### Changed

- Reworked dashboard layout from large sections to a mosaic/card dashboard.
- Improved dashboard readability and reduced long-page feeling.
- Improved technical audit UI.
- Improved technical audit Excel formatting.
- Improved spare parts Excel formatting.
- Improved print layouts for technical and stock reports.
- Improved dashboard access to important workflows.
- Improved layout consistency with the rest of the application.
- Updated README documentation for the new modules.
- Updated application version metadata.

### Fixed

- Fixed import duplicate handling for repeated Excel imports.
- Fixed import conflict review for items with/without serial numbers.
- Fixed model binding issue in import conflict review.
- Fixed maintenance cleanup foreign key issue caused by destruction records.
- Fixed technical audit print layout so it does not print the raw dashboard HTML.
- Fixed dashboard donut layout issues from previous experimental layouts.

---

## v0.3.0 - Destruction workflow and inventory dashboard refresh

### Added

- Added destruction workflow for non-functional / damaged inventory items.
- Added destruction folders/batches.
- Added selection of items for destruction.
- Added printable destruction report.
- Added printable Πράξη Καταστροφής Υλικού.
- Added printable Πρωτόκολλο Καταστροφής Υλικού.
- Added committee member fields.
- Added destroyed items history page.
- Added destroyed / withdrawn items to the dashboard pie chart.

### Changed

- Refreshed the visual theme.
- Reorganized the top navigation menu.
- Updated branding with Nyx Systems references.
- Improved dropdown menu behavior.
- Improved action button layout in destruction-related pages.

### Data / Safety

- Destroyed items are not hard-deleted from the system logic.
- Destroyed items are removed from active inventory views but remain available in history.
- Existing inventory data is preserved.

---

## v0.2.1 - Inventory row expand caret polish

### Added

- Added row expand/collapse behavior for inventory item details.
- Added clearer visual affordance for opening item details.
- Added improved compact display for item rows.

### Changed

- Replaced unclear “open” style behavior with a caret/expand interaction.
- Improved table usability for dense inventory lists.
- Improved visual consistency of inventory actions.

---

## v0.2.0 - Import/export and reporting improvements

### Added

- Added Excel import preview.
- Added mapping workflow for unknown rooms/categories.
- Added support for splitting bulk imported entries where needed.
- Added CSV export.
- Added improved printable inventory report.
- Added About, Contact and Update Center pages.

### Changed

- Improved Excel import/export reliability.
- Improved print/PDF report styling.
- Improved responsive behavior for mobile/tablet use.
- Improved database maintenance page.

---

## v0.1.0 - Initial school inventory MVP

### Added

- Initial ASP.NET Core Razor Pages application.
- SQLite database.
- School settings.
- Room management.
- Inventory category management.
- Inventory item management.
- Dashboard with basic statistics.
- Excel export.
- Basic print report.
- Docker / Portainer deployment support.

---

## Development notes

The project is under active development and school testing.

Recommended before every release:

```powershell
dotnet build
git status
git add .
git commit -m "Prepare vX.Y.Z release"
git push origin main
git tag -a vX.Y.Z -m "Release title"
git push origin vX.Y.Z
```
