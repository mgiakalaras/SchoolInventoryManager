## v0.2.0 - Inventory pagination and compact table UI

- Added pagination to the equipment list.
- Added page size options: 25, 50, 100 results per page.
- Added pagination controls above and below the equipment table.
- Compact row number column in the equipment table.
- Replaced equipment condition text with compact status icons and tooltips.
- Replaced text action buttons with compact icon buttons.
- Improved usability for large inventories with hundreds of devices.
- 
## v0.1.7 - Mobile menu overlay fix

- Διορθώθηκε το mobile hamburger menu ώστε να μην κρύβονται τα τελευταία links πίσω από το κάτω mobile navigation.
- Το ανοιχτό mobile menu πλέον κάθεται πάνω από το bottom nav και έχει δικό του scroll.
- Προστέθηκε δυναμικός υπολογισμός ύψους του bottom navigation στο `mobile-viewport-lock.js`.

## v0.1.6 - Mobile navigation real fix

- Restored the full `site.css` from v0.1.4 after the previous mobile patch accidentally replaced the file with only a small override.
- Added a mobile scroll-shell layout so the page content scrolls inside `.container` on small screens.
- Stabilized the bottom mobile navigation on Android/Chrome where the browser address bar changes viewport height during scroll.
- Added `mobile-viewport-lock.js` to measure the mobile header height and keep the content area correctly positioned.
- Updated application version display to v0.1.6.

## v0.1.5 - Mobile bottom navigation stabilization

- Σταθεροποιήθηκε το κάτω mobile navigation bar ώστε να μη φαίνεται σαν να “χορεύει” κατά το scroll σε κινητά/tablets.
- Αφαιρέθηκε το έντονο backdrop blur από το κάτω mobile menu για καλύτερη απόδοση σε mobile browsers.
- Προστέθηκαν mobile CSS optimizations για fixed navigation, safe-area και touch rendering.

## v0.1.4 - Creator bio in About

- Added a creator section to the About page.
- Added short bio for Marios Giakalaras.
- Clarified that the application was created from a real school inventory workflow.

## v0.1.3 - About / Contact / Update Center

- Added About page with application purpose, version and usage notes.
- Added Contact page with support checklist and issue-report template.
- Added Update Center page with safe update workflow for Docker/Portainer and local Visual Studio use.
- Added shared AppVersion utility for displaying the current application version.
- Added navigation links for About, Contact and Update pages.
- Added responsive styling for informational/admin pages.

## v0.1.2 - Portainer volume fix

- Replaced the relative bind mount `./App_Data:/app/App_Data` with a Docker named volume.
- Fixed Portainer Repository deployment on systems where `/data/compose/...` cannot be created because the root filesystem is read-only.
- The SQLite database remains persistent inside the Docker named volume `school_inventory_app_data`.


## v0.1.1 - Docker/server preparation

- Added Dockerfile for .NET 8 container builds.
- Added docker-compose.yml for local school server deployment.
- Added .dockerignore to keep build context clean.
- Moved default SQLite database path to App_Data/school_inventory.db.
- Added automatic App_Data/imports folder creation on startup.
- Added configuration switch for HTTPS redirection, disabled by default for school LAN/Docker testing.
- Added DEPLOYMENT.md with server, Docker, firewall, update, and safety instructions.

## v13.1 - Home logo lockup fix
- Replaced the full SVG-with-text logo in the home dashboard panel with a safer lockup: SVG icon + real HTML text.
- This prevents the word `Inventory` from being clipped as `Invento` on narrower panels or cached SVG scaling.
- Kept the visual style, dark theme and responsive layout for PC, tablet and mobile.


## v13 - Home branding/logo fix
- Fixed the full home logo SVG viewBox so the text is no longer clipped.
- Adjusted the dashboard brand panel so the logo scales cleanly on desktop, tablet and mobile.
- Kept the compact mark in the top navigation and the full logo only in the home dashboard panel.

# CHANGELOG

## v12 - Database maintenance and automatic numbering

- Added **Συντήρηση βάσης** page for safe testing/cleanup.
- Added option to clear only inventory items while keeping rooms, categories and school settings.
- Added option for full database reset and reseeding of default school settings, rooms and categories.
- Temporary uploaded Excel import files are cleaned during maintenance actions.
- Added confirmation text `ΚΑΘΑΡΙΣΜΟΣ` before destructive actions.
- Removed manual `Σειρά εμφάνισης` field from the room form.
- Room ordering is now assigned automatically when a new room is created.
- Editing a room now preserves the internal automatic ordering value.
- Added automatic `Α/Α` numbering in the Rooms list.
- Added automatic `Α/Α` numbering in the grouped Equipment list and detail numbering inside expanded groups.
- Added small UI styling for maintenance cards, confirmation input and row numbering.

## v11 - Practical inventory workflow

- Changed the default inventory manager title from hardcoded `Εκπαιδευτικός ΠΕ86` to generic `Υπεύθυνος/η απογραφής`.
- Added helper text in School Settings so each school can enter the correct role/title.
- Reworked **Χώροι** from card layout to a compact responsive table/list.
- Reworked **Εξοπλισμός** from card layout to a grouped responsive table/list.
- Equipment now appears grouped by room/category/item/condition, with expandable details per group.
- Added import preview step before final Excel import.
- Excel import now asks what to do with unknown rooms/categories:
  - create a new room/category, or
  - map it to an existing one.
- Added category matching suggestions for common singular/plural differences such as `Tablet` / `Tablets`.
- Added optional splitting of bulk entries for tracked equipment types such as Η/Υ, laptop, tablet and monitors.
- Print report now groups detailed equipment records back into summarized official rows.
