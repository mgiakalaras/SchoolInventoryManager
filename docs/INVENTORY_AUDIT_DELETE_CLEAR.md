# Inventory audit delete and clear scans

## Purpose

Allow cleanup of wrong draft audit folders.

## Added actions

On audit folder list and details:

- Clear scans
- Delete draft folder

## Rules

- Finalized folders cannot be deleted.
- Finalized folders cannot be cleared.
- Deleting a draft folder deletes:
  - the audit folder
  - its room sessions
  - its scan logs

Inventory items are NOT deleted.

## Why hard delete and not cancel yet?

This patch avoids adding new database columns.
It is intended for mistake cleanup while the folder is still draft.

A later patch can add a proper Cancelled state with audit metadata after users/login exist.
