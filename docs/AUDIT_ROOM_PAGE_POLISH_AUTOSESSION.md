# Audit Room page polish and auto-session binding

## Problem fixed

When `/Audit/Room` was opened as a standalone room check page, it showed the expected items as missing even if the same room had already been scanned from Android inside an active audit folder.

This happened because standalone mode did not know which audit folder / room session to read.

## Change

If the user selects a room without a folder/session, the page now tries to attach automatically to the latest active audit room session for that room.

Matching is done by:

1. RoomId
2. RoomNameSnapshot fallback

## UI polish

The page now has a scoped layout:

- clearer hero card
- two-column desktop layout
- clean stats cards
- better manual entry panel
- better expected/scanned/issues lists
- clearer warning when no active folder/session exists

## Important

The official route remains:

Audit Folder -> Room -> manual check/finalization

The standalone room selector is only a convenience and now tries to find the latest active folder automatically.
