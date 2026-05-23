# Implementation Plan: Themed Header Band

**Branch**: `021-themed-header-band` | **Date**: 2026-05-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/021-themed-header-band/spec.md`

## Summary

Add a subtle, on-brand decorative band behind the single shared authenticated page header (`_Layout.cshtml`), with a different abstract SVG per section. A pure static resolver maps the current controller to one of seven theme slugs (or `default`); the slug becomes a CSS modifier class on a decorative `aria-hidden` child element; CSS binds each class to a swappable SVG at a fixed path. Tint + art are right-weighted so the breadcrumb/title (left column) stay over near-body-background and remain WCAG-AA legible. Static, light-theme-only, no JS, no layout shift.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0), Razor views
**Primary Dependencies**: ASP.NET Core MVC; Tabler v1.4.0 (Bootstrap 5); Tabler Icons Webfont. No new dependency.
**Storage**: N/A — no database changes, no EF, no schema.
**Testing**: xUnit via `Mentoory.Tests.Integration` (references `Mentoory.Web`, uses `WebApplicationFactory`); Microsoft.Playwright via `Mentoory.Tests.E2E`.
**Target Platform**: ASP.NET Core web app (server-rendered Razor), modern browsers.
**Project Type**: Web application (modular monolith) — presentation layer only.
**Performance Goals**: No measurable page-load regression; zero added layout shift (CLS contribution 0). A few KB of static, cacheable SVG per theme.
**Constraints**: WCAG AA (≥ 4.5:1) for title/breadcrumb over the band; static (no animation); decorative-only (aria-hidden, not focusable); single header (no duplicate-header regression); light theme only; print-suppressed.
**Scale/Scope**: 7 themes + 1 default = 8 SVG assets; 1 resolver class; CSS additions to `mentoory.css`; 1 layout edit. ~5 areas / ~20 controllers covered by the mapping.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Relevance | Status |
|---|---|---|
| I. Clean Architecture layer boundaries | Feature is Web/presentation only; no Domain/Application/Infrastructure-persistence changes; no web deps leak inward. | PASS |
| II. CQRS | No commands/queries involved (no data access). | N/A |
| III. DDD constraints | No domain entities touched. | N/A |
| V. Zero-Warnings | New C# (resolver) + Razor must build warning-free with `TreatWarningsAsErrors`. | PASS (gate at build) |
| VI. DateTime handling | No time usage. | N/A |
| VII. Naming conventions | `HeaderTheme` resolver, `header-band--{slug}` classes follow existing conventions. | PASS |
| VIII. File organization | Resolver in `Mentoory.Web/Infrastructure/`; CSS in `wwwroot/css/mentoory.css`; assets in `wwwroot/img/headers/`; no JS added (CSS-only render). | PASS |
| IX. Spanish-First UI | No user-facing text added; theme slugs are internal identifiers. | PASS |
| X. Role hierarchy / session context | Band depends only on route (area/controller), not on role/permission; no authorization change. | PASS |
| XI. SSDT/DACPAC | No schema change. | N/A |
| Tabler-only components | SVG assets + CSS are not an external UI library. | PASS |

**Result**: No violations. Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/021-themed-header-band/
├── plan.md              # This file
├── spec.md              # Requirements
├── research.md          # Phase 0 — decisions (band size, tint, route table, test strategy)
├── data-model.md        # Phase 1 — conceptual theme registry + mapping (no persistence)
├── contracts/
│   └── header-band.md   # Phase 1 — DOM/CSS/file-path/route→theme contract (swap contract)
├── quickstart.md        # Phase 1 — how to swap art / add a theme / manual-QA checklist
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify)
├── REVIEW-SPEC.md       # Spec review gate output (SOUND)
├── review_brief.md      # Reviewer guide
└── tasks.md             # Phase 2 — created by /speckit-tasks (NOT here)
```

### Source Code (repository root)

```text
Mentoory.Web/
├── Infrastructure/
│   ├── ClaimsPrincipalExtensions.cs        # existing — sibling reference for placement
│   └── HeaderTheme.cs                       # NEW — pure static Resolve(area, controller) → slug
├── Views/Shared/
│   └── _Layout.cshtml                       # EDIT — inject decorative band child into .page-header
└── wwwroot/
    ├── css/
    │   └── mentoory.css                     # EDIT — .page-header-band + .header-band--{slug} rules
    └── img/headers/                         # NEW — 8 authored SVGs
        ├── dashboard.svg
        ├── proyectos.svg
        ├── conocimiento.svg
        ├── diagnostico.svg
        ├── personas.svg
        ├── incubadoras.svg
        ├── auditoria.svg
        └── default.svg

tests/
├── Mentoory.Tests.Integration/             # NEW tests: HeaderTheme.Resolve table + rendered-HTML assertions
└── Mentoory.Tests.E2E/                      # NEW test (light): band present, title visible, band not focusable
```

**Structure Decision**: Single modular-monolith web app; all changes confined to the `Mentoory.Web` presentation layer plus its existing test projects. The resolver lives beside `ClaimsPrincipalExtensions` in `Mentoory.Web/Infrastructure/`. No new projects.

## Phase 0 — Outline & Research

Complete. See [research.md](./research.md). All spec open-questions resolved:
- Band dimensions → natural header height, 1200×240 SVG canvas anchored right (R3).
- Tint model → per-theme, right-weighted fade (R2, R4).
- Route→theme table → controller-keyed static map, `default` fallback (R5).
- Light-theme-only → confirmed (R8).
- Rendering technique, swap contract, responsive, and test strategy → R1, R6, R7, R9.

No `NEEDS CLARIFICATION` markers remain.

## Phase 1 — Design & Contracts

Complete. Artifacts:
- [data-model.md](./data-model.md) — conceptual `HeaderTheme` registry + `ThemeMapping` rule (in-memory/static, not persisted).
- [contracts/header-band.md](./contracts/header-band.md) — the DOM contract, theme-slug set, CSS class↔file-path binding, and route→theme table that downstream code and the swap contract depend on.
- [quickstart.md](./quickstart.md) — swap art, add a theme/mapping, manual-QA checklist.
- Agent context updated (`CLAUDE.md` Active Technologies / plan reference).

### Re-evaluation (post-design Constitution Check)

No new violations introduced by the design. The resolver remains a pure presentation helper; no inward dependencies; no schema; no JS; no user-facing text. **PASS**.

## Complexity Tracking

Not applicable — no constitution violations to justify.
