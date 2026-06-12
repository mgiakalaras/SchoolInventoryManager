# Release Checklist - v0.7.0

## Build

- [ ] `dotnet build` ολοκληρώνεται χωρίς errors.
- [ ] `dotnet build` ολοκληρώνεται χωρίς warnings.
- [ ] `dotnet run` ξεκινά κανονικά την εφαρμογή.

## Web smoke test

- [ ] Ανοίγει το dashboard.
- [ ] Ανοίγει η λίστα αντικειμένων.
- [ ] Ανοίγει η λίστα χώρων.
- [ ] Ανοίγει η λίστα φακέλων απογραφής.
- [ ] Ανοίγει η σελίδα νέων ευρημάτων.
- [ ] Ανοίγει η σελίδα QR labels νέων ευρημάτων.
- [ ] Ανοίγει η σελίδα Android Scanner / mobile workflow.

## First inventory workflow

- [ ] Δημιουργείται νέος φάκελος με επιλογή `Πρώτη απογραφή / από μηδενική βάση`.
- [ ] Ο νέος φάκελος δημιουργείται κενός.
- [ ] Δεν εμφανίζει παλιούς χώρους.
- [ ] Δεν εμφανίζει παλιά αντικείμενα.
- [ ] Τα notes του φακέλου περιέχουν marker `[AuditMode:FirstInventory]`.

## Annual audit workflow

- [ ] Δημιουργείται νέος φάκελος με επιλογή `Ετήσιος έλεγχος υπάρχουσας βάσης`.
- [ ] Ο ετήσιος φάκελος δημιουργεί room sessions από την υπάρχουσα βάση.
- [ ] Τα υπάρχοντα workflows QR απογραφής παραμένουν λειτουργικά.

## Mobile API smoke test

Αντικατάστησε `FOLDER_ID` με πραγματικό id φακέλου:

```powershell
Invoke-RestMethod -Uri "http://192.168.1.80:5148/api/mobile/health"

Invoke-RestMethod -Uri "http://192.168.1.80:5148/api/mobile/audit-folders"

Invoke-RestMethod -Method Post `
  -Uri "http://192.168.1.80:5148/api/mobile/audit-folders/FOLDER_ID/rooms/create" `
  -ContentType "application/json; charset=utf-8" `
  -Body '{"name":"Δοκιμαστικός χώρος release 0.7.0"}'
```

- [ ] Το health endpoint απαντά.
- [ ] Οι φάκελοι απογραφής επιστρέφονται.
- [ ] Δημιουργείται χώρος από mobile API.
- [ ] Ο χώρος εμφανίζεται στο web app μέσα στον φάκελο.

## Android integration test

- [ ] Το Android Scanner βλέπει τον νέο φάκελο πρώτης απογραφής.
- [ ] Το Android Scanner δημιουργεί νέο χώρο.
- [ ] Το Android Scanner ανοίγει τον νέο χώρο.
- [ ] Το Android Scanner προσθέτει νέο αντικείμενο στον χώρο.
- [ ] Το νέο αντικείμενο εμφανίζεται στο web app ως νέο εύρημα.
- [ ] Τα QR labels νέων ευρημάτων εκτυπώνονται.

## Git

- [ ] `git status` καθαρό πριν το release commit.
- [ ] Commit release prep:

```powershell
git add .
git commit -m "Prepare web release v0.7.0"
git push origin main
```

- [ ] Δημιουργία tag:

```powershell
git tag v0.7.0
git push origin v0.7.0
```

## GitHub release

- [ ] Δημιουργία GitHub release για tag `v0.7.0`.
- [ ] Τίτλος:

```text
School Inventory Manager v0.7.0 - First inventory mobile discovery workflow
```

- [ ] Περιγραφή από `docs/RELEASE_NOTES_v0.7.0.md`.
