# Mobile Quick Add Options API

## Purpose

Add a mobile API endpoint so the Android Scanner can build a cleaner quick-add form using existing web app data.

This is the first step before changing the Android UI.

## Endpoint

```http
GET /api/mobile/quick-add-options
```

## Response

Returns:

- existing inventory categories/types
- equipment conditions
- default quantity info
- labels/guidance for the Android UI

## Why

The Android quick-add form should not be a loose set of text fields.

It should behave like a small mobile-friendly version of the web item card:

- `Τύπος αντικειμένου`
- `Κατάσταση λειτουργίας`
- `Μάρκα`
- `Μοντέλο`
- `Serial Number`
- `Ποσότητα (συνήθως 1)`
- `Σημειώσεις`

The review state `Προς έλεγχο από web app` remains separate from the item's operational condition.

## No database changes

This endpoint only reads:

- `InventoryCategories`
- `EquipmentCondition` enum

It does not create or modify data.

## Next steps

- Android models/API client for quick-add options.
- Android Quick Add form polish.
- Later: if needed, allow creating a new category/type directly from the mobile form.
