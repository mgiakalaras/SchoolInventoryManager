
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
