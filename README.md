# School Inventory Manager

**School Inventory Manager** είναι μια web εφαρμογή για σχολική απογραφή εξοπλισμού, με υποστήριξη QR labels, φακέλων απογραφής και Android/mobile scanner για επιτόπιο έλεγχο χώρων.

Η εφαρμογή ξεκίνησε ως εργαλείο για απογραφή ψηφιακού/τεχνικού εξοπλισμού σχολικής μονάδας και σταδιακά επεκτείνεται σε πιο πλήρη ροή απογραφής, ελέγχου, QR σήμανσης και καταστροφής υλικού.

---

## Τρέχουσα κατάσταση

Η εφαρμογή υποστηρίζει:

- Καταχώρηση και διαχείριση εξοπλισμού.
- Χώρους / αίθουσες.
- Κατηγορίες εξοπλισμού.
- Import / Export δεδομένων.
- QR labels για αντικείμενα.
- Φακέλους απογραφής.
- Χειροκίνητο έλεγχο χώρου από web.
- Android/mobile scanner API.
- Συγχρονισμό scans από κινητό/tablet.
- Νέα ευρήματα απογραφής από Android/mobile.
- QR labels ειδικά για νέα ευρήματα.
- Review workflow για αντικείμενα που βρέθηκαν επιτόπου.
- Ροή καταστροφής υλικού.
- Spare parts / απόθεμα ανταλλακτικών.
- About / Contact / Help pages.
- Docker deployment.

---

## Βασική ροή απογραφής

1. Περνάμε χώρους και εξοπλισμό.
2. Εκτυπώνουμε QR labels για τα αντικείμενα.
3. Δημιουργούμε φάκελο απογραφής.
4. Από Android/mobile scanner επιλέγουμε φάκελο και χώρο.
5. Σκανάρουμε QR αντικειμένων.
6. Αν βρεθεί αντικείμενο που δεν υπάρχει στη βάση, το προσθέτουμε από το κινητό ως νέο εύρημα.
7. Από το web app ελέγχουμε τα νέα ευρήματα.
8. Διορθώνουμε στοιχεία όπου χρειάζεται.
9. Εκτυπώνουμε QR labels μόνο για τα νέα ευρήματα.
10. Συνεχίζουμε και ολοκληρώνουμε την απογραφή.

---

## Android Scanner

Το Android Scanner είναι ξεχωριστή native Android εφαρμογή που συνδέεται με το web app.

Υποστηρίζει:

- Έλεγχο σύνδεσης με server.
- Επιλογή φακέλου απογραφής.
- Επιλογή χώρου.
- Σάρωση QR με κάμερα.
- Χειροκίνητη καταχώρηση QR/code ως fallback.
- Προσθήκη νέου αντικειμένου στον τρέχοντα χώρο.
- Συγχρονισμό με το web app.

Το Android project διατηρείται σε ξεχωριστό repository:

```text
SchoolInventoryScanner
```

---

## Mobile-created findings

Όταν ο χρήστης προσθέσει από κινητό ένα αντικείμενο που βρήκε στον χώρο, το web app το σημειώνει ως mobile audit finding.

Σχετικές σελίδες:

```text
/InventoryAudits/MobileFindings
/Labels/MobileFindings
```

Η πρώτη σελίδα είναι για έλεγχο/διόρθωση στοιχείων.  
Η δεύτερη είναι για εκτύπωση QR labels μόνο για αυτά τα νέα ευρήματα.

---

## Docker deployment

Η εφαρμογή μπορεί να τρέξει με Docker / Portainer.

Συνήθης ροή:

```powershell
git pull
docker compose build
docker compose up -d
```

ή redeploy από Portainer stack.

Τα δεδομένα της εφαρμογής πρέπει να διατηρούνται σε persistent volume, ώστε redeploy/update να μη χάνει τη βάση.

---

## Development workflow

Για ασφαλή ανάπτυξη:

```powershell
dotnet build
```

Μετά από επιτυχημένο build και δοκιμή:

```powershell
git add .
git commit -m "Meaningful commit message"
git push origin main
```

Για release:

```powershell
git tag -a v0.2.0 -m "School Inventory Manager v0.2.0"
git push origin v0.2.0
```

---

## Τρέχον προτεινόμενο release

Προτεινόμενο επόμενο release:

```text
v0.2.0
```

Λόγος: από τα παλιά `v0.1.x` έχουν προστεθεί σημαντικές ροές απογραφής, Android scanner integration, mobile quick add και νέα QR workflow.

---

## Roadmap

Άμεσα επόμενα:

- Navigation cleanup στο web app.
- Πιο καθαρό menu για απογραφή / QR / mobile findings.
- README / release notes σταθεροποίηση.
- Android README και release notes.
- Tablet layout test για Android scanner.
- App icon / splash / onboarding / tooltips στο Android app, σταδιακά και όχι όλα μαζί.

Μελλοντικά:

- Πιο πλήρη audit logs.
- Users / roles / login.
- Backup / restore από UI.
- Πλήρης γενική σχολική απογραφή πέρα από IT εξοπλισμό.
- Έπιπλα, γραφεία, καρέκλες και λοιπός σχολικός εξοπλισμός σε δεύτερη φάση.
- Πιο αναλυτική τεχνική απογραφή εργαστηρίου.

---

## Author

Created by **Μάριος Γιακαλάρας**.

```text
© 2026 - School Inventory Manager
```
