# School Inventory Scanner - UI direction

## Απόφαση

Η σάρωση QR δεν θα γίνεται πλέον από browser camera page.

Η web εφαρμογή παραμένει για:

- διαχείριση εξοπλισμού
- φάκελους απογραφής
- αναφορές
- εκτυπώσεις
- QR labels
- help / download native app

Η native Android εφαρμογή αναλαμβάνει:

- κάμερα
- σάρωση QR
- γρήγορη καταγραφή
- επιλογή φακέλου / χώρου
- συγχρονισμό με server

## Ύφος UI

Θέλουμε dark UI, αλλά όχι generic AI dashboard.

Κατεύθυνση:

- βαθύ navy background
- teal/cyan accent
- clean cards
- ανθρώπινα μικροκείμενα
- μεγάλα touch targets
- λίγα βήματα ανά οθόνη
- όχι υπερβολικά γραφήματα
- όχι “enterprise spaceship everywhere”

## Mobile scanner screens

Προτεινόμενη ροή:

1. Σύνδεση server
2. Επιλογή φακέλου απογραφής
3. Επιλογή χώρου
4. Σάρωση QR
5. Αποτέλεσμα
6. Συνέχεια σάρωσης / αλλαγή χώρου
7. Συγχρονισμός

## Web app changes for tomorrow

- Αφαίρεση browser camera scanner από `/Audit/Scan`.
- Μετατροπή `/Audit/Scan` σε landing/download/help page για Android Scanner.
- Προσθήκη σελίδας `/MobileApp/Index` ή `/Scanner/Download`.
- Update Help.
- Update menu labels.
- Προετοιμασία API για native app.
