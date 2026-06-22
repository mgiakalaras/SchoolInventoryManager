# Patch 46b - Full width command center

## Purpose

Replaces the centered dashboard polish with a full-width responsive command-center layout.

## Why

The previous dashboard still felt too centered and too much like classic top-menu plus boxes.

This patch changes the home page direction:

- full-width responsive layout
- cockpit/workspace structure
- left workflow rail
- central operational stage
- right status feed
- built-in helper/check panel
- category tiles with visual equipment icons
- better use of wide screens
- still collapses cleanly on tablets/mobiles

## Files

```text
Pages/Index.cshtml
docs/PATCH46B_FULL_WIDTH_COMMAND_CENTER.md
PATCH46B_FULL_WIDTH_COMMAND_CENTER.txt
```

## No backend changes

No database changes.
No migrations.
No model changes.
No service/API changes.

## Notes

This is intentionally a visual/UX direction patch.

Future polishing ideas:

- configurable category icons
- uploaded icons per equipment category
- dashboard widget settings
- user-specific dashboard
- deeper assistant/checklist panel
- live audit status widget
