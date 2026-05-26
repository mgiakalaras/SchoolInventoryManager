# Web scanner removal note

Η σάρωση QR με κάμερα αφαιρείται από το web UI.

## Γιατί

Οι mobile browsers μπλοκάρουν συχνά την πρόσβαση στην κάμερα όταν η εφαρμογή τρέχει σε απλό HTTP τοπικού δικτύου.
Αυτό δημιουργεί κακή εμπειρία και ασταθή συμπεριφορά.

## Νέα κατεύθυνση

- Web app:
  - φάκελοι απογραφής
  - QR labels
  - αναφορές
  - help
  - download Android scanner app
  - manual fallback

- Native Android app:
  - camera scan
  - επιλογή φακέλου/χώρου
  - καταγραφή αποτελέσματος
  - συγχρονισμός με server

## Επόμενο τεχνικό βήμα

Mobile Scanner API Foundation.
