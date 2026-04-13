# Research: Design System & UX Polish

**Date**: 2026-04-13  
**Feature**: specs/010-design-system-ux-polish/

## R1: Tabler CSS Variable Override Strategy

**Decision**: Override `--tblr-primary` and `--tblr-primary-rgb` at `:root` level in `mentoory.css` to globally change the primary color from Bootstrap blue to Mentoory coral.

**Rationale**: Tabler builds all component colors from CSS custom properties. Overriding at `:root` cascades to buttons, badges, alerts, form focus states, links, and all other components without touching individual component styles. This is the intended customization path documented by Tabler.

**Alternatives considered**:
- Per-component overrides: Too verbose, maintenance burden, easy to miss components
- SASS rebuild with custom variables: Requires build tooling change, adds complexity
- CSS `color-scheme` approach: Not supported by Tabler's architecture

**Key finding**: Tabler uses `--tblr-primary-rgb` for rgba() variants (focus rings, translucent backgrounds). Both `--tblr-primary` and `--tblr-primary-rgb` must be set together. The semantic colors (success, danger, warning, info) also need explicit override to prevent them from using Tabler's defaults which may clash with the new primary.

## R2: Logo SVG Asset Creation

**Decision**: Create two SVG logo files manually based on the PDF logo. Store in `wwwroot/img/` directory.

**Rationale**: The PDF contains raster renditions of the logo. SVG is needed for crisp rendering at any size. Two variants needed:
- `logo-white.svg` — white monochrome, for dark sidebar background
- `logo-gradient.svg` — full gradient version, for auth pages and light backgrounds

**Alternatives considered**:
- Tracing PDF to SVG via automated tools: Quality loss, gradient handling unreliable
- Embedding the PDF logo as `<img>`: Large file size, can't control colors
- Using just text "Mentoory": Doesn't meet spec requirement for logo image

**Key finding**: The logo is an "M" lettermark with a checkmark inside a circle, using a left-to-right gradient (gold #F5B731 → coral #E07850 → magenta #D946A8). The sidebar width in Tabler is ~250px, so logo should be sized to ~120px wide.

## R3: DataTable Empty State Integration

**Decision**: Use DataTable's `language.emptyTable` callback combined with Tabler's `.empty` component HTML injected when zero rows are returned.

**Rationale**: DataTables 2.x supports custom HTML in the `language.emptyTable` string. By injecting Tabler's `.empty` component markup, we get a rich empty state without needing to toggle between the table and a separate empty-state div.

**Alternatives considered**:
- Hide table, show separate empty div: Requires JavaScript coordination, flicker on load
- Use DataTables `drawCallback` to replace content: Works but more complex
- Custom DataTables plugin: Over-engineering for this use case

**Key finding**: The `initDataTable` helper in `datatable-helper.js` already centralizes DataTable initialization. Adding `language.emptyTable` there with configurable icon/message/action parameters extends the pattern cleanly.

## R4: Relative Date Rendering in Spanish

**Decision**: Create a small `formatRelativeDate(isoString)` utility function in `datatable-helper.js` that returns relative Spanish time strings.

**Rationale**: No external dependency needed. The rules are simple:
- < 1 minute: "hace un momento"
- < 1 hour: "hace N minutos"  
- < 1 day: "hace N horas"
- < 7 days: "hace N días"
- < 30 days: "hace N semanas"
- Otherwise: formatted date string

**Alternatives considered**:
- moment.js / date-fns: New dependency, spec says no new JS dependencies
- Intl.RelativeTimeFormat: Browser API, but limited Spanish locale support and requires more code than a simple function
- Server-side rendering: Would prevent DataTable client-side sorting on raw dates

**Key finding**: DataTable column renderers can return HTML. The relative date can be wrapped in a `<span>` with a `title` attribute containing the exact ISO date for tooltip display.

## R5: Menu Badge Count Architecture

**Decision**: Add optional `int? BadgeCount` property to `MenuItem` class. `MenuService` injects counts by querying lightweight count services at render time.

**Rationale**: The MenuItem model is immutable (set in constructor). A new constructor overload or factory method that accepts badge count keeps the existing API compatible. The `MenuService` is `Scoped`, so it can inject Application-layer query dispatchers.

**Alternatives considered**:
- Separate badge data via ViewBag: Disconnected from menu items, fragile matching
- JavaScript-fetched badges: Extra HTTP round-trip, flash of unbadged menu
- Full menu item replacement: Breaking change to MenuConfiguration

**Key finding**: `MenuService` is registered as `Scoped` in `Program.cs`. It receives `IHttpContextAccessor` and `ITenantContext`. Adding an `IMediator` injection to dispatch count queries per context is architecturally sound and follows existing patterns.

## R6: Dashboard Metric Queries

**Decision**: Create `GetDashboardMetricsQuery` as a single MediatR query that returns a `DashboardMetricsDto` with user count, project count, and diagnostic count for the current incubator context.

**Rationale**: A single query with one DB round-trip is more efficient than three separate count queries. The controller dispatches this query and passes the DTO to the view model.

**Alternatives considered**:
- Three separate count queries: More HTTP round-trips, harder to manage
- View component with self-contained query: Adds complexity, splits dashboard logic
- Cached counts refreshed on a timer: Over-engineering for current scale

**Key finding**: The dashboard is incubator-scoped (checks `User.HasValidIncubatorContext()`). Count queries filter by `ActiveIncubatorId` claim. Repositories already support `IQueryable` patterns for counting.

## R7: Auth Page Illustration Approach

**Decision**: Create a CSS-only decorative illustration using positioned geometric shapes (circles, lines, dots) that evoke growth/connection. No inline SVG needed.

**Rationale**: CSS shapes are lightweight, responsive, and trivially maintainable. They create a professional abstract pattern without requiring illustration skills or external assets.

**Alternatives considered**:
- Inline SVG illustration: More detailed but harder to maintain, larger markup
- External SVG file: Adds asset dependency, caching concerns
- No illustration (gradient only): Too minimal per spec requirement

**Key finding**: The auth layout left panel (`_AuthLayout.cshtml`) is already a flex container. CSS `::before` and `::after` pseudo-elements plus a few positioned `<div>` elements can create floating circles and connecting lines at various opacities.
