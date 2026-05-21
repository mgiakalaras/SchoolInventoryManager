# CHANGELOG

All notable changes to **School Inventory Manager** are documented here.

The project now follows semantic versioning:

- **Major**: breaking changes or major architecture changes
- **Minor**: new feature modules or significant workflow additions
- **Patch**: fixes, UI polish, small improvements

---

## v0.3.0 - Destruction workflow and inventory dashboard refresh

### Added
- Added new destruction workflow for non-functional / damaged inventory items.
- Added destruction batch/folder workflow for selected inventory items.
- Added item selection page for materials marked as non-functional or pending destruction.
- Added printable report for selected items pending destruction.
- Added printable “Πράξη Καταστροφής Υλικού” based on the provided school template.
- Added printable “Πρωτόκολλο Καταστροφής Υλικού” based on the provided school template.
- Added committee member fields for the destruction workflow.
- Added destroyed items history page.
- Added links from destroyed items history to the related destruction folder, act and protocol.
- Added destroyed / withdrawn items to the dashboard pie chart so the user can see what percentage of inventory has been removed from active use.

### Changed
- Reorganized the top navigation menu into clearer groups.
- Improved action button layout across destruction-related pages.
- Added a more distinctive visual theme for the School Inventory Manager.
- Updated branding text to use “Ψηφιακή απογραφή σχολικού υλικού” and “Nyx Systems”.
- Improved dropdown menu behavior so menus stay open while moving the mouse to menu items.
- Updated application version display to `v0.3.0`.

### Data / Safety
- Destroyed items are not hard-deleted from the database.
- Destroyed items are marked as inactive / destroyed and removed from active inventory views.
- Destruction records preserve historical context for future reference.

### Notes
- This version is intended for development and school testing.
- Existing inventory data is preserved.

---

## v0.2.1 - Inventory row expand caret polish

### Changed
- Replaced the repeated “Άνοιγμα” text in the equipment table with a compact caret icon.
- Moved the expand/collapse indicator before the item name.
- Improved readability of grouped equipment rows.

---

## v0.2.0 - Inventory pagination and compact table UI

### Added
- Added pagination to the equipment list.
- Added page size options: 25, 50 and 100 results per page.
- Added pagination controls above and below the equipment table.

### Changed
- Added compact row number column in the equipment table.
- Replaced equipment condition text with compact status icons and tooltips.
- Replaced text action buttons with compact icon buttons.
- Improved usability for large inventories with hundreds of devices.

---

## v0.1.7 - Mobile menu overlay fix

### Fixed
- Fixed the mobile hamburger menu so the last links are no longer hidden behind the bottom mobile navigation.
- The open mobile menu now appears above the bottom navigation and has its own scrolling area.
- Added dynamic bottom navigation height calculation in `mobile-viewport-lock.js`.

---

## v0.1.6 - Mobile navigation real fix

### Fixed
- Restored the full `site.css` from `v0.1.4` after the previous mobile patch accidentally replaced the file with only a small override.
- Added a mobile scroll-shell layout so page content scrolls inside `.container` on small screens.
- Stabilized the bottom mobile navigation on Android / Chrome where the browser address bar changes viewport height during scroll.
- Added `mobile-viewport-lock.js` to measure the mobile header height and keep the content area correctly positioned.
- Updated application version display to `v0.1.6`.

---

## v0.1.5 - Mobile bottom navigation stabilization

### Fixed
- Stabilized the bottom mobile navigation bar so it no longer appears to “jump” while scrolling on phones and tablets.
- Removed heavy backdrop blur from the bottom mobile menu for better mobile browser performance.
- Added mobile CSS optimizations for fixed navigation, safe-area support and touch rendering.

---

## v0.1.4 - Creator bio in About

### Added
- Added a creator section to the About page.
- Added short bio for Marios Giakalaras.
- Clarified that the application was created from a real school inventory workflow.

---

## v0.1.3 - About / Contact / Update Center

### Added
- Added About page with application purpose, version and usage notes.
- Added Contact page with support checklist and issue-report template.
- Added Update Center page with safe update workflow for Docker / Portainer and local Visual Studio use.
- Added shared `AppVersion` utility for displaying the current application version.
- Added navigation links for About, Contact and Update pages.
- Added responsive styling for informational and admin pages.

---

## v0.1.2 - Portainer volume fix

### Fixed
- Replaced the relative bind mount `./App_Data:/app/App_Data` with a Docker named volume.
- Fixed Portainer Repository deployment on systems where `/data/compose/...` cannot be created because the root filesystem is read-only.
- The SQLite database remains persistent inside the Docker named volume `school_inventory_app_data`.

---

## v0.1.1 - Docker / server preparation

### Added
- Added Dockerfile for .NET 8 container builds.
- Added `docker-compose.yml` for local school server deployment.
- Added `.dockerignore` to keep build context clean.
- Added `DEPLOYMENT.md` with server, Docker, firewall, update and safety instructions.

### Changed
- Moved default SQLite database path to `App_Data/school_inventory.db`.
- Added automatic `App_Data/imports` folder creation on startup.
- Added configuration switch for HTTPS redirection, disabled by default for school LAN / Docker testing.

---

## v0.1.0 - Initial public school inventory MVP

### Added
- Added core school inventory management workflow.
- Added equipment registration and editing.
- Added rooms and categories.
- Added school settings.
- Added inventory print report.
- Added Excel import workflow.
- Added grouped inventory views for practical school use.
- Added dark responsive UI for desktop, tablet and mobile use.

### Notes
- This version represents the first cleaned public baseline after the early internal development snapshots.

---

# Legacy development milestones

The following entries were created before the project adopted `v0.x.x` semantic versioning. They are preserved for historical reference.

---

## Legacy v13.1 - Home logo lockup fix

### Fixed
- Replaced the full SVG-with-text logo in the home dashboard panel with a safer lockup: SVG icon plus real HTML text.
- Prevented the word `Inventory` from being clipped as `Invento` on narrower panels or cached SVG scaling.
- Kept the visual style, dark theme and responsive layout for PC, tablet and mobile.

---

## Legacy v13 - Home branding / logo fix

### Fixed
- Fixed the full home logo SVG viewBox so the text is no longer clipped.
- Adjusted the dashboard brand panel so the logo scales cleanly on desktop, tablet and mobile.
- Kept the compact mark in the top navigation and the full logo only in the home dashboard panel.

---

## Legacy v12 - Database maintenance and automatic numbering

### Added
- Added **Συντήρηση βάσης** page for safe testing and cleanup.
- Added option to clear only inventory items while keeping rooms, categories and school settings.
- Added option for full database reset and reseeding of default school settings, rooms and categories.
- Added confirmation text `ΚΑΘΑΡΙΣΜΟΣ` before destructive actions.
- Added automatic `Α/Α` numbering in the Rooms list.
- Added automatic `Α/Α` numbering in the grouped Equipment list and detail numbering inside expanded groups.
- Added small UI styling for maintenance cards, confirmation input and row numbering.

### Changed
- Temporary uploaded Excel import files are cleaned during maintenance actions.
- Removed manual `Σειρά εμφάνισης` field from the room form.
- Room ordering is now assigned automatically when a new room is created.
- Editing a room now preserves the internal automatic ordering value.

---

## Legacy v11 - Practical inventory workflow

### Added
- Added import preview step before final Excel import.
- Excel import now asks what to do with unknown rooms and categories:
  - create a new room/category, or
  - map it to an existing one.
- Added category matching suggestions for common singular/plural differences such as `Tablet` / `Tablets`.
- Added optional splitting of bulk entries for tracked equipment types such as Η/Υ, laptop, tablet and monitors.

### Changed
- Changed the default inventory manager title from hardcoded `Εκπαιδευτικός ΠΕ86` to generic `Υπεύθυνος/η απογραφής`.
- Added helper text in School Settings so each school can enter the correct role/title.
- Reworked **Χώροι** from card layout to a compact responsive table/list.
- Reworked **Εξοπλισμός** from card layout to a grouped responsive table/list.
- Equipment now appears grouped by room/category/item/condition, with expandable details per group.
- Print report now groups detailed equipment records back into summarized official rows.