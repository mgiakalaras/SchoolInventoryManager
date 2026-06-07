# Navigation cleanup

## Purpose

Clean up the main navigation so the user can find the new audit/mobile workflow without confusion.

## Previous problem

The menu mixed unrelated actions together:

- normal QR labels
- new mobile findings
- Android scanner
- audit folders
- settings
- reports

This made the workflow unclear, especially after adding mobile-created findings.

## New structure

### Υλικό

- Εξοπλισμός
- Νέο υλικό
- Χώροι
- Import / Export

### Απογραφή

- Φάκελοι απογραφής
- Νέος φάκελος
- Χειροκίνητος έλεγχος χώρου
- Android Scanner

### Ευρήματα / QR

- Νέα ευρήματα
- QR νέων ευρημάτων
- QR ετικέτες εξοπλισμού
- Αναφορές απογραφής

### Τεχνικά / Απόθεμα

- Τεχνικός έλεγχος
- Τεχνικές αναφορές
- Ανταλλακτικά / Απόθεμα
- Ιστορικό ανταλλακτικών

### Διαχείριση

- Σχολείο / Ρυθμίσεις
- Καταστροφή υλικού
- Κατεστραμμένα υλικά
- Συντήρηση βάσης
- Update Center

### Βοήθεια

- Οδηγίες χρήσης
- About
- Επικοινωνία

## Mobile bottom navigation

Reduced and focused on field work:

- Αρχική
- Υλικό
- Απογραφή
- Ευρήματα
- Scanner

## File

- `Pages/Shared/_Layout.cshtml`

## No backend changes

This is a navigation/layout-only patch.
