# Razor @media escape fix

## Problem

Build error:

```text
CS0103: The name 'media' does not exist in the current context
Pages/InventoryAudits/Index.cshtml
```

## Cause

Inside Razor `.cshtml` files, CSS `@media` must be escaped as `@@media`.

Razor otherwise tries to interpret `@media` as C# code.

## Fix

Changed:

```css
@media (max-width: 980px)
```

to:

```css
@@media (max-width: 980px)
```

## File

- `Pages/InventoryAudits/Index.cshtml`
