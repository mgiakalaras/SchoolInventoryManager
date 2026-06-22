# Release checklist - v0.7.1

## Before release

- [ ] `dotnet build` succeeds.
- [ ] Dashboard opens at `/`.
- [ ] No runtime error on dashboard.
- [ ] First Inventory link works.
- [ ] Mobile Findings link works.
- [ ] QR Mobile Findings link works.
- [ ] Android Scanner help page opens.
- [ ] Desktop wide-screen layout checked.
- [ ] Smaller browser width checked.
- [ ] Database backup exists before server redeploy.

## Git

```powershell
git status
git add .
git commit -m "Prepare web release v0.7.1"
git push origin main
git tag v0.7.1
git push origin v0.7.1
```

## Server

- [ ] Redeploy Portainer stack.
- [ ] Check dashboard after deploy.
- [ ] Check browser cache if old layout appears.
