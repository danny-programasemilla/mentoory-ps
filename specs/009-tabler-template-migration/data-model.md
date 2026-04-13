# Data Model: Tabler Admin Template Migration

**Date:** 2026-04-13
**Feature:** 009-tabler-template-migration

## Overview

This feature introduces **no new domain entities, database tables, or data model changes**. It is a pure frontend template migration affecting only:

- Razor view files (`.cshtml`)
- Static assets (CSS, JS, font files)
- `MenuConfiguration.cs` (icon string values only)

## Modified Entities

### MenuItem (Web Infrastructure)

**File:** `Mentoory.Web/Infrastructure/Menu/MenuItem.cs` (and related types)

**Change:** The `Icon` property type remains `string`. Only the stored values change.

| Before | After |
|--------|-------|
| `"fas fa-home"` | `"ti ti-home"` |
| `"fas fa-building"` | `"ti ti-building"` |
| `"fas fa-users"` | `"ti ti-users"` |

No structural changes to the `MenuItem`, `MenuGroup`, or `IMenuService` types.

## Static Asset Changes

### Added
- `wwwroot/lib/tabler/css/tabler.min.css`
- `wwwroot/lib/tabler/js/tabler.min.js`
- `wwwroot/lib/tabler-icons-webfont/tabler-icons.min.css`
- `wwwroot/lib/tabler-icons-webfont/fonts/*` (webfont files)

### Removed
- `wwwroot/lib/bootstrap/` (entire directory — Tabler bundles Bootstrap 5)

### Modified
- `wwwroot/css/mentoory.css` (remove Bootstrap-duplicate rules, add DataTables-Tabler overrides)
