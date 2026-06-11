---
description: "Task list for page content banner strip implementation"
---

# Tasks: Page Content Banner Strip

**Input**: Design documents from `/specs/023-page-content-banner/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/page-banner.md, quickstart.md

**Tests**: Included. The constitution requires tests to ship with each feature; the action→icon resolver is pure logic (TDD-friendly) and the rendered strip has stable selectors for HTML/E2E assertions. Mirrors the feature-021 test trio.

**Organization**: Tasks grouped by the three user stories from spec.md (US1 P1 core banner, US2 P1 single title, US3 P2 graceful fallback) so each is independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete-task dependency)
- **[Story]**: US1 / US2 / US3 (story-phase tasks only)
- Paths are repo-relative.

## Path Conventions

Web app (modular monolith). All production changes in `Mentoory.Web/`. Tests in existing `tests/Mentoory.Tests.Integration/` (references `Mentoory.Web`) and `tests/Mentoory.Tests.E2E/`. No new project, no schema, no JS, no image assets.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the work surface. No new project, asset directory, or dependency is required — the strip is pure Razor + CSS + one static helper.

- [X] T001 Confirm the touch-set exists and compiles at baseline: `Mentoory.Web/Infrastructure/HeaderTheme.cs` (reused), `Mentoory.Web/Views/Shared/_Layout.cshtml`, `Mentoory.Web/wwwroot/css/mentoory.css`. Run `dotnet build` to establish a warning-free green baseline before changes.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The pure action→icon resolver and the shared strip CSS structure. Both block every user story (the layout cannot render an icon class without the resolver; all visual rules sit on the shared `.page-banner` CSS).

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [X] T002 [P] Write FAILING unit tests for `PageBannerIcon.Resolve(string? action)` in `tests/Mentoory.Tests.Integration/Web/PageBannerIconResolveTests.cs` — `[Trait("Category","Unit")]`, no host. Assert every row from data-model.md (`Index→list`, `Create→plus`, `Edit→edit`, `Details→eye`, `Delete→trash`), case-insensitivity (e.g. `eDiT→edit`), unmapped/`null`/blank → `layout-2` (default), and that the result is never null/whitespace. (FR-004, FR-005)
- [X] T003 Implement `Mentoory.Web/Infrastructure/PageBannerIcon.cs` — a pure static class with a case-insensitive `Dictionary<string,string>` action→slug map and `Resolve(string? action)` returning the bare Tabler icon slug, `default` = `layout-2`. Mirror the shape/XML-doc style of `HeaderTheme`. Make T002 pass. (FR-004, FR-005)
- [X] T004 Add the shared strip CSS to `Mentoory.Web/wwwroot/css/mentoory.css` (new "Page content banner (023)" block, after the 021 header-band block): `.page-banner` — flex row, `min-height: 60px`, gradient built from `rgba(var(--banner-accent), …)` weighted so the left title region stays WCAG-AA legible; `.page-banner__title` — `page-title` typography + ellipsis truncation (`overflow:hidden; white-space:nowrap`); `.page-banner__icon` — right-anchored, ~2rem, faint (low opacity, section-tinted), `pointer-events:none`. (FR-003, FR-014, FR-016, FR-017)
- [X] T005 Add the eight `.page-banner--{slug}` accent rules (each sets only `--banner-accent: R, G, B`) using the section RGB triples from data-model.md, plus the responsive rule `@media (max-width: 767.98px) { .page-banner__icon { display: none; } }`. Leave the existing `.header-band--{slug}` (021) rules unchanged. (FR-006, FR-007, small-screen clarification)

**Checkpoint**: `dotnet test … PageBannerIconResolveTests` green; CSS compiles; no visual wiring yet.

---

## Phase 3: User Story 1 — Consistent, action-aware page banner above content (Priority: P1) 🎯 MVP

**Goal**: A section-themed strip with the page title and an action icon renders above the content on every authenticated page.

**Independent Test**: Visit list/create/edit/details pages across sections; each shows the strip above content with the right section colour and action icon.

- [X] T006 [US1] Create the strip partial `Mentoory.Web/Views/Shared/_PageBanner.cshtml` — accepts the resolved `slug`, `icon`, and `title`; renders `<div class="page-banner page-banner--{slug}"> {title-h2 if non-blank} <i class="ti ti-{icon} page-banner__icon" aria-hidden="true"></i></div>` exactly per contracts/page-banner.md. (FR-001, FR-002, FR-003, FR-011)
- [X] T007 [US1] Edit `Mentoory.Web/Views/Shared/_Layout.cshtml`: compute `bannerIcon = PageBannerIcon.Resolve(action)` alongside the existing `headerTheme`, and render `_PageBanner` as the first child of `.page-body > .container-xl`, before `@RenderBody()`, passing `headerTheme` (slug), `bannerIcon`, and `pageTitle`. (FR-001, FR-006, FR-013 — inside the authenticated branch only)
- [X] T008 [P] [US1] Write integration render tests in `tests/Mentoory.Tests.Integration/Web/PageBannerRenderTests.cs` — `[Collection(IntegrationTestCollection.Name)]`, mirror `HeaderBandRenderTests`. Over routes `/Administration/Dashboard` (dashboard), `/Administration/Users` (personas), `/Administration/Projects` (proyectos), `/AvailableProjects` (default): assert exactly one `.page-banner` inside `.page-body`, carrying `page-banner--{slug}`, containing one `i.page-banner__icon` with the action's `ti-{icon}` and `aria-hidden="true"`, and no link/button/tabindex inside the icon (C-01..C-03, C-07). (SC-001, SC-003, SC-006)
- [X] T009 [P] [US1] Write E2E test `tests/Mentoory.Tests.E2E/Tests/PageBannerTests.cs` (mirror `HeaderBandTests`): on an authenticated route the `.page-banner` is visible, sits above the page content, is ≈60px tall, and shows the title once. (SC-001, SC-008)

**Checkpoint**: US1 delivers the visible, themed, action-aware strip — a usable MVP on its own.

---

## Phase 4: User Story 2 — Single, non-duplicated page title (Priority: P1)

**Goal**: The title lives only in the strip; the header band keeps breadcrumb + actions + topbar but no longer shows the title.

**Independent Test**: Any authenticated page shows the title once (in the strip, not in the header band), with breadcrumb/actions/topbar intact.

- [X] T010 [US2] Edit `Mentoory.Web/Views/Shared/_Layout.cshtml`: remove `<h2 class="page-title">@pageTitle</h2>` from the `.page-header` band block, keeping the `.page-pretitle`/breadcrumb, the `PageActions` section, and `_TopBar`. (FR-008, FR-009)
- [X] T011 [US2] Extend `PageBannerRenderTests` with single-title + header-preserved assertions: the page title text appears in `.page-banner` and the `.page-header` no longer contains `h2.page-title`; the title appears exactly once in the document; `.page-header` still renders `.breadcrumb`, any `PageActions`, and the `_TopBar` (C-04, C-05). Assert the strip title is the page's primary heading exposed to assistive tech (FR-010). (SC-002)
- [X] T012 [US2] Regression guard: run the existing 021 suites (`HeaderBandRenderTests`, `HeaderThemeResolveTests`, `HeaderBandTests`) and fix any assertion that depended on `h2.page-title` living in the band — the band's breadcrumb/decorative/topbar contract must still hold after the title moves out.

**Checkpoint**: Title shown exactly once across header band + strip; 021 band contract preserved.

---

## Phase 5: User Story 3 — Graceful fallback for unmapped / title-less pages (Priority: P2)

**Goal**: Pages with an unmapped action or no declared title still render a coherent strip.

**Independent Test**: An unmapped-action page shows the default icon + section colour; a title-less page renders the strip without an empty title node.

- [X] T013 [US3] Verify/confirm the partial (`_PageBanner.cshtml`) omits the `<h2>` entirely when `title` is null/blank (no empty/placeholder node) while still rendering the gradient + icon; adjust the conditional if needed. (FR-012)
- [X] T014 [P] [US3] Extend `PageBannerRenderTests`: a route with a non-standard action renders `ti-layout-2` (default icon) with the section's `page-banner--{slug}` and no layout breakage; assert the strip still renders for an authenticated page even when the title is blank (no empty `h2.page-title page-banner__title`). (SC-003, FR-005, FR-012)
- [X] T015 [P] [US3] Add a negative integration assertion: the login page (and/or an error page) renders no `.page-banner` element (C-06). (SC-004)

**Checkpoint**: All three stories complete; fallback behaviour defined and tested.

---

## Phase 6: Polish & Cross-Cutting

**Purpose**: Verification, hygiene, and the spec's measurable outcomes.

- [X] T016 Run `/simplify` (or equivalent) over the new helper, partial, and CSS — remove any dead/duplicated rules; confirm gradient logic lives only in `.page-banner` (per-slug rules carry only `--banner-accent`).
- [X] T017 `dotnet build` warning-free (Constitution V) and full `dotnet test` green (unit + integration + E2E + 021 regression).
- [X] T018 Manual quickstart pass (quickstart.md table): confirm SC-001..SC-008 — strip present above content on representative pages, title shown once, action icons correct, section colours match the 021 band, login/error excluded, title contrast ≥ 4.5:1, no new image files added (`git status`), and narrow-viewport (~375px) legibility with the icon hidden and no overflow.

---

## Dependencies & Execution Order

- **Phase 1 (T001)** → **Phase 2 (T002–T005)**: foundational resolver + CSS block everything.
- **T003** depends on **T002** (TDD). **T004/T005** are CSS-only and independent of the resolver (can run in parallel with T002/T003).
- **Phase 3 (US1)**: T006 (partial) → T007 (layout wiring) need T003+T004+T005. T008/T009 (tests) can be written in parallel once T006/T007 land.
- **Phase 4 (US2)**: T010 edits the same `_Layout.cshtml` as T007 — sequence after T007 (not parallel). T011/T012 follow T010.
- **Phase 5 (US3)**: T013 follows T006; T014/T015 are parallel test additions.
- **Phase 6**: after all stories.

## Parallel Opportunities

- T002 (resolver test) ∥ T004/T005 (CSS) — different files.
- Within US1: T008 ∥ T009 (integration vs E2E, different files) after the markup lands.
- Within US3: T014 ∥ T015 — independent assertions.

## Implementation Strategy

MVP = Phase 1 + Phase 2 + Phase 3 (US1): a visible, themed, action-aware strip. US2 removes the
title duplication (the reason the strip becomes the title's single home). US3 hardens the edges.
Each phase ends green and is independently demoable.
