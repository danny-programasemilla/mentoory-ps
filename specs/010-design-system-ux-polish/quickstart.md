# Quickstart: Design System & UX Polish

**Date**: 2026-04-13  
**Feature**: specs/010-design-system-ux-polish/

## Prerequisites

- .NET 10.0 SDK
- Node.js (for Tabler package if rebuilding)
- SQL Server with Mentoory database

## Build & Run

```bash
# Build
dotnet build

# Run with Aspire
dotnet run --project Mentoory.Aspire.AppHost

# Run Web only
dotnet run --project Mentoory.Web

# Test
dotnet test
```

## Key Files to Modify (by phase)

### Phase 1: Foundation
- `Mentoory.Web/wwwroot/css/mentoory.css` — brand palette CSS custom properties

### Phase 2: Shell
- `Mentoory.Web/Views/Shared/_Navigation.cshtml` — sidebar with logo, section styling, badge counts
- `Mentoory.Web/Views/Shared/_TopBar.cshtml` — avatar dropdown, notification bell, context display
- `Mentoory.Web/Views/Shared/_Breadcrumbs.cshtml` — integrate into page-header
- `Mentoory.Web/Views/Shared/_Footer.cshtml` — left/right layout
- `Mentoory.Web/Infrastructure/Menu/MenuItem.cs` — add BadgeCount property
- `Mentoory.Web/Infrastructure/Menu/MenuService.cs` — inject badge counts
- `Mentoory.Web/wwwroot/img/logo-white.svg` — new file
- `Mentoory.Web/wwwroot/img/logo-gradient.svg` — new file

### Phase 3: Component Patterns
- `Mentoory.Web/wwwroot/js/datatable-helper.js` — empty states, skeleton loading, render helpers
- `Mentoory.Web/Views/Shared/Components/DataTable/Default.cshtml` — table-vcenter, card wrapper
- `Mentoory.Web/wwwroot/css/mentoory.css` — component-specific styles (sidebar hover, avatar)

### Phase 4: View Application
- All `.cshtml` files in `Areas/*/Views/` (~35 content views)
- `Mentoory.Application/Administration/Queries/GetDashboardMetrics/` — new query + handler
- `Mentoory.Web/Areas/Administration/Controllers/DashboardController.cs` — dispatch metrics query

### Phase 5: Auth Pages
- `Mentoory.Web/Views/Shared/_AuthLayout.cshtml` — gradient panel, logo, illustration
- All views in `Areas/Access/Views/` (~8 content views)

## Verification

```bash
# Build must pass with zero warnings
dotnet build

# All tests must pass
dotnet test

# Run and verify visually at https://localhost:7061
dotnet run --project Mentoory.Web
```
