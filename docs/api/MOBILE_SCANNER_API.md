# Mobile Scanner API

Αυτό το API προορίζεται για τη native Android εφαρμογή **School Inventory Scanner**.

## Base URL

Παράδειγμα σχολικού server:

```text
http://172.26.0.1:5148
```

## Health

```text
GET /api/mobile/health
```

Επιστρέφει βασικές πληροφορίες server/app.

## Φάκελοι απογραφής

```text
GET /api/mobile/audit-folders
GET /api/mobile/audit-folders?includeFinalized=true
```

Επιστρέφει διαθέσιμους φακέλους απογραφής.

## Χώροι φακέλου

```text
GET /api/mobile/audit-folders/{folderId}/rooms
```

Επιστρέφει τις απογραφές χώρων ενός φακέλου.

## Λεπτομέρειες απογραφής χώρου

```text
GET /api/mobile/room-sessions/{roomSessionId}
```

Επιστρέφει:

- στοιχεία φακέλου
- στοιχεία χώρου
- αναμενόμενα αντικείμενα
- ποια έχουν σκαναριστεί
- λάθος χώρο
- άγνωστα QR

## Καταγραφή σάρωσης

```text
POST /api/mobile/room-sessions/{roomSessionId}/scan
Content-Type: application/json

{
  "code": "SIM-2026-000123"
}
```

Δέχεται επίσης full QR URL:

```json
{
  "rawValue": "http://172.26.0.1:5148/q/SIM-2026-000123"
}
```

Πιθανά status:

- `Found`
- `WrongRoom`
- `Unknown`

Αν ο χώρος ή ο φάκελος είναι οριστικοποιημένος, επιστρέφει `423 Locked`.

## Lookup αντικειμένου

```text
GET /api/mobile/items/{code}
```

Επιστρέφει πληροφορίες αντικειμένου χωρίς να καταγράφει scan.

## Σημείωση ασφάλειας

Στο τρέχον στάδιο δεν υπάρχει authentication.
Το API προορίζεται μόνο για τοπικό σχολικό δίκτυο / VPN.
Πριν από δημόσια έκθεση χρειάζονται χρήστες, ρόλοι και tokens.
