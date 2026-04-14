# Quickstart: Table Polish System-Wide

**Feature**: 012-table-polish

## Prerequisites

- .NET 10.0 SDK
- SQL Server running (for the app to function, though no DB changes in this feature)
- A seeded database with test data (users, projects, incubators, diagnostics) for visual QA

## Development Setup

```bash
# 1. Switch to the feature branch
git checkout 012-table-polish

# 2. Build the solution (zero-warnings gate)
dotnet build

# 3. Run the web app
dotnet run --project Mentoory.Web

# Or with Aspire (full orchestration):
dotnet run --project Mentoory.Aspire.AppHost
```

## Key Files to Edit

| File | Purpose |
|------|---------|
| `Mentoory.Web/wwwroot/css/mentoory.css` | Add zebra/hover/info padding CSS |
| `Mentoory.Web/wwwroot/js/datatable-helper.js` | Add icon registry + applyHeaderIcons() |
| `Mentoory.Web/Views/Shared/Components/DataTable/Default.cshtml` | Enhanced template with table classes |
| `Mentoory.Web/Views/Shared/Components/DataTable/DataTableViewComponent.cs` | New: strongly-typed model |
| 7 DataTable views (see plan.md Phase 3) | Migrate to shared component + status dots |
| 4 static table views (see plan.md Phase 4) | Add CSS classes |

## Verification

### Build Check
```bash
dotnet build  # Must have zero warnings
```

### Visual QA (in browser)

1. Navigate to **Administration > Users** — verify zebra rows, hover, header icons, status dots
2. Navigate to **Administration > Projects** — verify same + action buttons still work
3. Navigate to **Platform > Incubators** — verify same across Platform area
4. Navigate to **Coordination > Diagnostics** — verify same across Coordination area
5. Check any empty table state — verify no visual artifacts
6. Click column headers — verify sorting still works
7. Check pagination — verify page navigation works
8. Check the info line ("Mostrando...") — verify left padding is aligned

### Regression Check

- Skeleton loading during data fetch should look correct
- Filter forms (if present) should still submit and refresh the table
- Empty state messages should display cleanly
- Action buttons (view, edit links) should still work
