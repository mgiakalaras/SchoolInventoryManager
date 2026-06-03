# Audit Room selection/session fix

## Problem

On `/Audit/Room`, changing the selected room from the dropdown could still show data from the previous room/session.

Example:

- Dropdown selected: Αίθουσα Μεθοδολογίας
- Page loaded: Αίθουσα Γερμανικών Α' Ορόφου

This happened because the GET form kept a hidden `RoomSessionId`.
When the user selected a new `RoomId`, the old `RoomSessionId` still won and reloaded the old room session.

## Fix

- Removed hidden `RoomSessionId` from the room selection GET form.
- Kept `FolderId` so changing rooms inside a folder still works.
- Updated backend binding logic:
  - if `RoomSessionId` is stale and does not match selected `RoomId`, ignore it
  - bind by `FolderId + RoomId`
  - fallback by room name
  - fallback to latest active session for selected room
- Prevents `RoomSession.RoomId` from overwriting a newly selected `RoomId`.

## Files

- `Pages/Audit/Room.cshtml`
- `Pages/Audit/Room.cshtml.cs`
