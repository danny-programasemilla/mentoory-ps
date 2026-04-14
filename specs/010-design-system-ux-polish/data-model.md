# Data Model: Design System & UX Polish

**Date**: 2026-04-13  
**Feature**: specs/010-design-system-ux-polish/

## Entity Changes

### MenuItem (extended)

**File**: `Mentoory.Web/Infrastructure/Menu/MenuItem.cs`

**Current state**: Immutable model with Title, Url, Icon, Roles properties.

**Change**: Add optional `BadgeCount` property.

| Property | Type | Description |
|----------|------|-------------|
| Title | string | Menu item display text (existing) |
| Url | string | Navigation URL (existing) |
| Icon | string | Tabler icon class e.g. "ti ti-users" (existing) |
| Roles | string[] | Authorized roles (existing) |
| **BadgeCount** | **int?** | **Optional count badge displayed next to menu item title. Null = no badge.** |

**Validation**: BadgeCount must be >= 0 when not null.

### MenuGroup (no changes)

Inherits from MenuItem. Items collection is `IReadOnlyList<MenuItem>`. No structural changes needed — badge counts are per-item, not per-group.

### DashboardMetricsDto (new)

**Location**: `Mentoory.Application/Administration/Queries/`

| Property | Type | Description |
|----------|------|-------------|
| UserCount | int | Total users in the active incubator |
| ProjectCount | int | Total projects in the active incubator |
| DiagnosticFormCount | int | Total diagnostic forms in the active incubator |

**No database changes**: This DTO is populated by counting existing entities. No new tables or columns.

## Query Changes

### GetDashboardMetricsQuery (new)

**Location**: `Mentoory.Application/Administration/Queries/GetDashboardMetrics/`

| Item | Value |
|------|-------|
| Request | `GetDashboardMetricsQuery(long IncubatorId) : IBaseRequest<DashboardMetricsDto>` |
| Handler | `GetDashboardMetricsQueryHandler : BaseCommandHandler<GetDashboardMetricsQuery, DashboardMetricsDto>` |
| Dependencies | `IUserRepository`, `IProjectRepository`, `IDiagnosticFormRepository` |
| Pattern | Three `CountAsync()` calls, return DTO |

## CSS Design Tokens

Not a data model per se, but the CSS custom property palette is the "schema" for the design system. Defined in `mentoory.css` as specified in the spec's FR-001. This is the single source of truth for all brand colors.

## No Database Schema Changes

This feature requires zero SQL changes, zero SSDT modifications, and zero PostDeployment scripts. All changes are in the Web layer (views, CSS, JS) and Application layer (one new query).
