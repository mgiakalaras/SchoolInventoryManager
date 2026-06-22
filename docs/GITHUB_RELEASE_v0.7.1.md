# School Inventory Manager v0.7.1 - Full-width dashboard command center preview

This is a server-test release for the new full-width dashboard direction.

## What's new

- Redesigned home dashboard as a full-width operational command center.
- Better use of large screens.
- Workflow-focused layout instead of centered dashboard cards.
- Left workflow rail for audit/discovery actions.
- Central area with statistics, status map and launchpad.
- Right-side feed for technical issues, recent items and stock warnings.
- Basic automatic icons for equipment categories.
- Responsive layout for smaller screens.

## Notes

No database migration is required.

This release is mainly for UI/UX testing on a real server before more polishing continues.

## Upgrade

Pull/redeploy the latest image/stack from the `main` branch or tag `v0.7.1`.

Recommended before update:

- backup database/volume
- redeploy stack
- test `/`
- test first-inventory and QR navigation links
