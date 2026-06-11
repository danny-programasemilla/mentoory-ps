# Implementation Plan: Page Content Banner Strip

**Branch**: `023-page-content-banner` | **Date**: 2026-06-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/023-page-content-banner/spec.md`

## Summary

Add a slim (~60px), sober content banner strip to every authenticated page rendered by the
main `_Layout`. The strip sits inside `.page-body`, above the page content, and carries the
page title (relocated out of the feature-021 header band) on the left plus a faint,
per-action Tabler webfont icon on the right. Its colour treatment is section-themed, reusing
the existing feature-021 section resolver (`HeaderTheme.Resolve`) so there is a single
section→theme mapping authority. No image assets, no JavaScript, no database, no
Domain/Application changes — this is a Razor view + CSS + one pure helper.

**Technical approach**: (1) Relocate `<h2 class="page-title">` from the header band into a
new shared partial `_PageBanner.cshtml` rendered at the top of `.page-body > .container-xl`.
(2) Add a pure static resolver `PageBannerIcon.Resolve(action)` mapping the MVC action name to
a Tabler icon slug (Index→list, Create→plus, Edit→edit, Details→eye, Delete→trash, else
default). (3) Reuse `HeaderTheme.Resolve(area, controller)` for the section slug so the strip
class is `page-banner page-banner--{slug}`. (4) Add CSS for `.page-banner` (height band,
gradient built from a per-slug accent custom property, right-anchored faint icon, title
truncation, `md`-breakpoint icon hiding). Mirror the 021 test trio (unit resolver +
integration render + E2E).

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0), Razor views

**Primary Dependencies**: ASP.NET Core MVC 10.x; Tabler v1.4.0 (Bootstrap 5); Tabler Icons
Webfont. No new dependency.

**Storage**: N/A — no database, no EF, no schema. Reads existing `ViewData["Title"]` and route
values (`area`/`controller`/`action`).

**Testing**: xUnit + FluentAssertions. Unit (`HeaderThemeResolveTests`-style, no host) for the
icon resolver; integration render via `MentooryWebApplicationFactory` (mirrors
`HeaderBandRenderTests`); Playwright E2E (mirrors `HeaderBandTests`).

**Target Platform**: Server-rendered web (authenticated pages on the main `_Layout`).

**Project Type**: Web application (modular monolith) — change is confined to `Mentoory.Web`.

**Performance Goals**: No measurable runtime cost — one extra pure static call + a small
static partial per render. No new network requests (no image assets, CSS already loaded).

**Constraints**: ~60px height band; WCAG-AA contrast (≥ 4.5:1) for the title over every
section treatment; zero new image assets; zero warnings (Constitution V); all user-facing text
Spanish (the only text is the existing page title — already Spanish).

**Scale/Scope**: One layout edit, one new partial, one new ~25-line static helper, one CSS
block (~40 lines incl. 8 per-slug accent lines), three test files. Affects all authenticated
pages (~dozens of routes) uniformly via the shared layout.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Assessment |
|-----------|------------|
| I. Clean Architecture Layer Boundaries | **PASS** — change is entirely in `Mentoory.Web` (view + `Infrastructure` helper). No Domain/Application/Infrastructure-persistence touch. No web→domain leakage; the helper is a pure presentation utility, peer to the existing `HeaderTheme`. |
| II. CQRS | **N/A** — no commands/queries; no handler changes. |
| III. DDD | **N/A** — no aggregates, entities, or value objects. |
| IV. Integration Events | **N/A**. |
| V. Zero-Warnings Policy | **PASS (design intent)** — verified at build in the implement/verify stages. |
| VI. DateTime Handling | **N/A** — no time usage. |
| VII. Naming Conventions | **PASS** — `PageBannerIcon` (PascalCase type), CSS `page-banner--{slug}` mirrors the existing `header-band--{slug}` convention. |
| VIII. File Organization | **PASS** — partial in `Views/Shared/`, helper in `Web/Infrastructure/`, styles in `wwwroot/css/mentoory.css`. No JS (none needed), so the "JS lives in wwwroot/js" rule is vacuously satisfied. |
| IX. Spanish-First UI | **PASS** — the only rendered text is the existing `ViewData["Title"]` (already Spanish per existing pages). The feature introduces no new hardcoded user-facing strings. |
| X. Role Hierarchy & Session Context | **N/A** — no authorization logic; the strip renders for any authenticated user, identical to the header band it sits below. |
| XI. SSDT/DACPAC | **N/A** — no schema. |

**Result**: All gates PASS or N/A. No violations → Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/023-page-content-banner/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions
├── data-model.md        # Phase 1 — the presentation view-model + resolver tables
├── quickstart.md        # Phase 1 — manual + automated validation guide
├── contracts/
│   └── page-banner.md   # Phase 1 — rendered DOM/CSS contract
└── tasks.md             # Phase 2 — created by /speckit-tasks
```

### Source Code (repository root)

```text
Mentoory.Web/
├── Infrastructure/
│   ├── HeaderTheme.cs          # EXISTING (021) — reused unchanged for the section slug
│   └── PageBannerIcon.cs       # NEW — pure action → Tabler-icon-slug resolver
├── Views/Shared/
│   ├── _Layout.cshtml          # EDIT — remove H2 from header band; render _PageBanner in page-body
│   └── _PageBanner.cshtml      # NEW — the strip partial (title + section class + action icon)
└── wwwroot/css/
    └── mentoory.css            # EDIT — add .page-banner block + per-slug accents + md responsive

tests/
├── Mentoory.Tests.Integration/Web/
│   ├── PageBannerIconResolveTests.cs   # NEW — unit, no host (mirrors HeaderThemeResolveTests)
│   └── PageBannerRenderTests.cs        # NEW — integration render (mirrors HeaderBandRenderTests)
└── Mentoory.Tests.E2E/Tests/
    └── PageBannerTests.cs              # NEW — E2E DOM/visual (mirrors HeaderBandTests)
```

**Structure Decision**: Single-project change inside `Mentoory.Web`, following the exact
pattern established by feature 021 (a pure static resolver in `Infrastructure/`, a class on a
layout element, and per-slug CSS rules). Tests live alongside the 021 tests in the existing
`Mentoory.Tests.Integration` and `Mentoory.Tests.E2E` projects.

## Complexity Tracking

> No constitution violations. Section intentionally empty.
