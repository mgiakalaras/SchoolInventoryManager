# School Inventory Manager

Web εφαρμογή για την απογραφή Η/Υ, tablets, οθονών, εκτυπωτών, βιντεοπροβολέων και λοιπού ψηφιακού εξοπλισμού σε σχολικές μονάδες.

Στόχος είναι ο υπεύθυνος απογραφής να μπορεί να καταχωρίζει χώρους και εξοπλισμό από υπολογιστή, tablet ή κινητό, να εισάγει/εξάγει δεδομένα από Excel και να παράγει καθαρό έντυπο απογραφής για εκτύπωση ή PDF.

## Βασικά χαρακτηριστικά

- Dashboard με στατιστικά εξοπλισμού
- Χώροι σχολείου και εξοπλισμός ανά χώρο
- Excel import/export
- CSV export
- Εκτύπωση / Save as PDF
- Responsive περιβάλλον για PC, tablet και κινητό
- Docker / Portainer εγκατάσταση
- SQLite βάση δεδομένων
- Σελίδες About, Επικοινωνία και Update Center

Η εφαρμογή ξεκίνησε από πραγματικό έντυπο απογραφής σχολείου και εξελίσσεται σε εργαλείο που μπορεί να χρησιμοποιηθεί από οποιαδήποτε σχολική μονάδα για καταγραφή χώρων, εξοπλισμού, κατάστασης υλικού, αναφορών, εκτύπωσης και εξαγωγής δεδομένων.

> Τρέχουσα φάση: School testing  
> Προτεινόμενη χρήση: τοπικό σχολικό δίκτυο, OXI δημόσια έκθεση στο Internet

---

## Δημιουργός

Δημιουργός: **Marios Giakalaras**  
GitHub: **@mgiakalaras**  
Email: **mgiakalaras@hotmail.com**

Ο Μάριος είναι εκπαιδευτικός Πληροφορικής και διαχειρίζεται σχολικό εργαστήριο και ψηφιακές υποδομές. Η εφαρμογή δημιουργήθηκε με βάση πραγματικές ανάγκες σχολικής απογραφής: μετακίνηση από αίθουσα σε αίθουσα, χρήση από κινητό/tablet/PC, εκτύπωση επίσημου εντύπου και δυνατότητα εισαγωγής/εξαγωγής δεδομένων.

---

## Βασικά χαρακτηριστικά

### Διαχείριση σχολείου

- Στοιχεία σχολικής μονάδας
- Σχολικό έτος
- Υπεύθυνος/η απογραφής
- Λογότυπο και branding εφαρμογής
- Σελίδες **About**, **Επικοινωνία** και **Update Center**

### Χώροι

- Καταχώριση αιθουσών, γραφείων και εργαστηρίων
- Αυτόματη αρίθμηση Α/Α
- Compact προβολή για πιο γρήγορη χρήση
- Responsive εμφάνιση για PC, tablet και κινητό

### Εξοπλισμός

- Καταχώριση αντικειμένων ανά χώρο
- Κατηγορίες εξοπλισμού
- Κατάσταση εξοπλισμού
- Ποσότητα
- Μάρκα / μοντέλο
- Serial number
- Περιγραφή
- Παρατηρήσεις
- Ομαδοποιημένη προβολή εξοπλισμού με δυνατότητα εμφάνισης αναλυτικών εγγραφών

### Import / Export

- Λήψη προτύπου Excel
- Εισαγωγή από Excel `.xlsx`
- Εξαγωγή σε Excel
- Εξαγωγή σε CSV
- Προεπισκόπηση εισαγωγής
- Αντιστοίχιση άγνωστων χώρων/κατηγοριών πριν την τελική εισαγωγή
- Υποστήριξη περιπτώσεων όπως `Tablet` / `Tablets`
- Δυνατότητα διαχωρισμού μαζικών εγγραφών για αντικείμενα που χρειάζονται ξεχωριστή παρακολούθηση

### Αναφορές / Εκτύπωση

- Καθαρή αναφορά απογραφής για εκτύπωση
- Λευκό έντυπο ανεξάρτητο από το dark theme
- Πίνακες ανά χώρο
- Συγκεντρωτικά στοιχεία
- Προβληματικός εξοπλισμός
- Χώρος για υπογραφές
- Εκτύπωση σε φυσικό εκτυπωτή ή αποθήκευση ως PDF από τον browser

### Dashboard

- Σύνολα απογραφής
- Στατιστικά εξοπλισμού
- Ποσοστά κατάστασης
- Οπτικά στοιχεία/γραφήματα για γρήγορη εικόνα

### Συντήρηση βάσης

- Εκκαθάριση μόνο εξοπλισμού
- Πλήρης επαναφορά βάσης για δοκιμές
- Επιβεβαίωση πριν από επικίνδυνες ενέργειες
- Καθαρισμός προσωρινών import αρχείων

### Mobile / Tablet χρήση

Η εφαρμογή έχει σχεδιαστεί ώστε να μπορεί να χρησιμοποιηθεί κατά την απογραφή μέσα στο σχολείο:

- από σταθερό υπολογιστή
- από laptop
- από tablet
- από κινητό

Η λογική είναι να τρέχει σε σχολικό server και ο χρήστης να ανοίγει την εφαρμογή από browser σε οποιαδήποτε συσκευή βρίσκεται στο ίδιο τοπικό δίκτυο.

---

## Τεχνολογίες

- ASP.NET Core Razor Pages
- .NET 8
- Entity Framework Core
- SQLite
- ClosedXML για Excel import/export
- Docker / Docker Compose
- Portainer-friendly deployment
- Responsive HTML/CSS/JavaScript

---

## Τοπική εκτέλεση με Visual Studio

### Απαιτήσεις

- Visual Studio 2022 ή νεότερο
- .NET 8 SDK
- Git

### Εκτέλεση

1. Κάνε clone το repository:

```powershell
git clone https://github.com/mgiakalaras/SchoolInventoryManager.git
cd SchoolInventoryManager
```

2. Άνοιξε το:

```text
SchoolInventoryManager.sln
```

ή το:

```text
SchoolInventoryManager.csproj
```

3. Περίμενε να γίνει restore των NuGet packages.

4. Κάνε:

```text
Clean Solution
Build Solution
Run
```

5. Η βάση δημιουργείται αυτόματα στο:

```text
App_Data/school_inventory.db
```

---

## Docker / Portainer deployment

Η εφαρμογή μπορεί να στηθεί σε σχολικό server, ZimaOS, CasaOS, Linux server ή Windows μηχάνημα με Docker.

### Docker Compose

Από τον φάκελο του project:

```bash
docker compose up -d --build
```

Άνοιγμα από άλλη συσκευή στο ίδιο δίκτυο:

```text
http://SERVER-IP:5148
```

Παράδειγμα:

```text
http://192.168.1.80:5148
```

### Portainer

Στο Portainer:

```text
Stacks → Add stack → Repository
```

Ρυθμίσεις:

```text
Name:
school-inventory-manager

Repository URL:
https://github.com/mgiakalaras/SchoolInventoryManager.git

Repository reference:
refs/heads/main

Compose path:
docker-compose.yml
```

Η εφαρμογή χρησιμοποιεί Docker named volume για τα δεδομένα:

```text
school_inventory_app_data
```

Η βάση SQLite και τα προσωρινά import αρχεία παραμένουν εκεί ώστε να μη χάνονται σε redeploy.

---

## Update / αναβάθμιση

### Από Portainer

Αν το stack έχει στηθεί από Git repository:

```text
Stacks → school-inventory-manager → Pull and redeploy / Update stack
```

Πριν από σημαντική αναβάθμιση, κράτα backup των δεδομένων ή του Docker volume.

### Από terminal

```bash
git pull
docker compose up -d --build
```

### Από Visual Studio

```powershell
git pull
```

Μετά:

```text
Clean Solution
Build Solution
Run
```

---

## Backup

Τα δεδομένα δεν πρέπει να βασίζονται μόνο στο container. Για σοβαρή χρήση χρειάζεται backup πολιτική.

Προτεινόμενα:

- τακτικό export σε Excel
- backup του Docker volume `school_inventory_app_data`
- backup του αρχείου `App_Data/school_inventory.db` σε local εγκατάσταση

---

## Ασφάλεια

Η τρέχουσα έκδοση προορίζεται για χρήση **μόνο μέσα στο τοπικό σχολικό δίκτυο**.

Μην την εκθέτεις απευθείας στο Internet πριν προστεθούν:

- authentication / login
- ρόλοι χρηστών
- HTTPS setup
- backup policy
- audit log
- βασική προστασία από μη εξουσιοδοτημένη πρόσβαση

---

## Git workflow

Τρέχουσα λογική ανάπτυξης:

- μικρά patches
- καθαρά commits
- tags ανά σημαντική έκδοση
- changelog
- δοκιμή σε Visual Studio πριν από push
- deploy σε server μέσω Portainer/GitHub

Χρήσιμες εντολές:

```powershell
git status
git add .
git commit -m "Your message"
git push
```

Tag έκδοσης:

```powershell
git tag -a v0.x.x -m "Release description"
git push origin v0.x.x
```

---

## Roadmap

Πιθανά επόμενα βήματα:

- login / users / roles
- admin area
- backup / restore μέσα από UI
- QR codes ανά αντικείμενο
- εκτύπωση ετικετών
- φωτογραφίες εξοπλισμού
- ιστορικό αλλαγών
- audit log
- αναφορά για Δήμο / Σχολική Επιτροπή
- καλύτερο Update Center
- export κατευθείαν σε πραγματικό PDF server-side
- documentation / user manual

---

## Κατάσταση έργου

Το project βρίσκεται σε ενεργή ανάπτυξη και δοκιμή.

Η εφαρμογή έχει ήδη δοκιμαστεί σε:

- Visual Studio
- GitHub workflow
- Docker / Portainer deployment
- PC browser
- mobile/tablet responsive χρήση

---

## License

Δεν έχει οριστεί ακόμη τελική άδεια χρήσης.  
Μέχρι να προστεθεί επίσημο `LICENSE` file, το project θεωρείται υπό ανάπτυξη και δεν παρέχεται ξεκάθαρη άδεια επαναχρησιμοποίησης ή διανομής.
