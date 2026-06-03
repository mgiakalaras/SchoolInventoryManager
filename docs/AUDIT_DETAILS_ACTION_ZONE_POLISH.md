# Audit details action zone polish

## Problem

The audit folder details page had too many important/destructive actions mixed into the top action bar.

That made the page feel unsafe and visually noisy.

## Change

The page now separates actions into clear zones:

1. Top navigation/actions:
   - Φάκελοι
   - Χειροκίνητος έλεγχος
   - Ενημέρωση συνόλων

2. Οριστικοποίηση φακέλου panel:
   - Only for the final lock action.

3. Διορθώσεις πρόχειρου φακέλου danger panel:
   - Καθαρισμός scans
   - Διαγραφή φακέλου

## File

- `Pages/InventoryAudits/Details.cshtml`

## No backend changes

This patch only changes layout/CSS/text on the details page.
