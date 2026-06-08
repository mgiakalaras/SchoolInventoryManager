# Mobile create room route fix

## Problem

The existing mobile API uses this route convention:

```text
/api/mobile/audit-folders
/api/mobile/audit-folders/{id}/rooms
```

Patch 41b accidentally added the create-room endpoint as:

```text
/api/mobile/folders/{folderId}/rooms/create
```

This was inconsistent and caused confusion during testing.

## Fixed route

The endpoint is now:

```http
POST /api/mobile/audit-folders/{folderId}/rooms/create
```

## Test existing folder list

```powershell
Invoke-RestMethod -Uri "http://SERVER:5148/api/mobile/audit-folders"
```

## Test create room

```powershell
Invoke-RestMethod -Method Post `
  -Uri "http://SERVER:5148/api/mobile/audit-folders/FOLDER_ID/rooms/create" `
  -ContentType "application/json; charset=utf-8" `
  -Body '{"name":"Δοκιμαστικός χώρος από Android"}'
```

## Files

- `Pages/Api/Mobile/FolderCreateRoom.cshtml`
- `Pages/Api/Mobile/FolderCreateRoom.cshtml.cs`
