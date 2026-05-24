---
description: "Task list for themed header band implementation"
---

# Tasks: Themed Header Band

**Input**: Design documents from `/specs/021-themed-header-band/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/header-band.md, quickstart.md

**Tests**: Included. The constitution requires tests to ship with each feature; the route→theme resolver is pure logic (TDD-friendly) and the rendered band has stable selectors for HTML/E2E assertions.

**Organization**: Tasks grouped by the three user stories from spec.md (US1 P1, US2 P2, US3 P3) so each is independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no incomplete-task dependency)
- **[Story]**: US1 / US2 / US3 (story-phase tasks only)
- Paths are repo-relative.

## Path Conventions

Web app (modular monolith). All production changes in `Mentoory.Web/`. Tests in existing `tests/Mentoory.Tests.Integration/` (references `Mentoory.Web`) and `tests/Mentoory.Tests.E2E/`. No new project, no schema, no JS.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Asset structure for the header SVGs.

- [X] T001 Create the header-asset directory `Mentoory.Web/wwwroot/img/headers/` (add a temporary `.gitkeep`; real SVGs land in T006).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The route→theme resolver and the band's shared structural CSS. Both block every user story (the layout injection in US1 cannot render a class without the resolver; all visual rules sit on the shared positioning CSS).

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [X] T002 [P] Write FAILING unit tests for `HeaderTheme.Resolve(string? area, string? controller)` in `tests/Mentoory.Tests.Integration/Web/HeaderThemeResolveTests.cs` — assert every mapping row from data-model.md (`Dashboard→dashboard`, `Projects→proyectos`, `Knowledge`/`Templates→conocimiento`, `Diagnostics`/`Diagnostic`/`AnswerCorrection→diagnostico`, `Users`/`Sponsor`/`BatchUpload→personas`, `Incubators→incubadoras`, `AuditLog→auditoria`), case-insensitivity, unmapped controller → `default`, and `(null,null)` → `default`.
- [X] T003 Implement `Mentoory.Web/Infrastructure/HeaderTheme.cs` — pure static `Resolve` returning a non-empty slug per the data-model table; make T002 pass. No I/O, no state.
- [X] T004 Add shared structural CSS for the band in `Mentoory.Web/wwwroot/css/mentoory.css`: `.page-header { position: relative; overflow: hidden; }`, `.page-header .container-xl { position: relative; z-index: 1; }`, `.page-header-band { position: absolute; inset: 0; z-index: 0; pointer-events: none; background-repeat: no-repeat; background-position: right center; background-size: auto 100%; }`.

**Checkpoint**: Resolver green and structural CSS in place — user stories can begin.

---

## Phase 3: User Story 1 - Distinct, fresh visual identity per section (Priority: P1) 🎯 MVP

**Goal**: Each section shows a visibly distinct, on-brand abstract band behind the header; unmapped sections show `default`; title/breadcrumb stay legible.

**Independent Test**: Visit one page per theme + one unmapped page; each mapped section shows distinct art, unmapped shows `default`, and the title/breadcrumb are crisp on every page.

### Tests for User Story 1 ⚠️ (write first, ensure they fail)

- [X] T005 [P] [US1] Integration HTML test in `tests/Mentoory.Tests.Integration/Web/HeaderBandRenderTests.cs` — via the existing `WebApplicationFactory` fixture, GET a representative authenticated route per theme and one unmapped route; assert exactly one `.page-header`, a first-child `.page-header-band` carrying the expected `header-band--{slug}` class, and unmapped → `header-band--default`. (Will fail until T007/T008 land.)

### Implementation for User Story 1

- [X] T006 [P] [US1] Author the 8 abstract SVGs in `Mentoory.Web/wwwroot/img/headers/` (`dashboard.svg`, `proyectos.svg`, `conocimiento.svg`, `diagnostico.svg`, `personas.svg`, `incubadoras.svg`, `auditoria.svg`, `default.svg`): `viewBox="0 0 1200 240"`, minimal abstract geometry, brand palette (coral `#E07850`, gold `#F5B731`, magenta `#D946A8`, neutrals), composed to read when anchored right and clipped to header height; a few KB each. Remove the `.gitkeep`.
- [X] T007 [P] [US1] Add per-theme CSS rules `.header-band--{slug}` (8 rules incl. `default`) in `Mentoory.Web/wwwroot/css/mentoory.css`, each binding `background-image: url('/img/headers/{slug}.svg'), linear-gradient(90deg, transparent 0 45%, <per-theme low-alpha tint> 100%)` per contracts/header-band.md. Depends on T004 + T006.
- [X] T008 [US1] Inject the band into `Mentoory.Web/Views/Shared/_Layout.cshtml`: compute `var headerTheme = Mentoory.Web.Infrastructure.HeaderTheme.Resolve(area, controller);` (using the existing `area`/`controller` vars) and render `<div class="page-header-band header-band--@headerTheme" aria-hidden="true"></div>` as the FIRST child of the single `.page-header`. The edit stays inside the existing `User.Identity?.IsAuthenticated == true` branch so the band renders only on authenticated app pages, never on auth pages (FR-014). Do not add any second header (FR-010). Depends on T003.
- [ ] T009 [US1] Manual QA — distinctness + contrast: confirm each theme renders distinct art (SC-001) and the title + breadcrumb measure ≥ 4.5:1 over the band on every theme (SC-002/FR-007); tune the per-theme tint alpha in `mentoory.css` for any theme that falls short (worst case: most-saturated tint).

**Checkpoint**: MVP — every section shows a distinct, legible, on-brand band.

---

## Phase 4: User Story 2 - Swappable art with no code change (Priority: P2)

**Goal**: A designer can replace a section's artwork by overwriting its file; a missing file degrades gracefully.

**Independent Test**: Replace one `{slug}.svg` with a different image, reload that section, confirm the new art appears with zero code change; remove a file, confirm the band degrades to tint-only with the title intact.

### Implementation for User Story 2

- [X] T010 [US2] Confirm the swap contract holds: each `.header-band--{slug}` rule references only `/img/headers/{slug}.svg` (no inlined art, no JS), and `.header-band--default` is always defined so the resolver's fallback always paints. Verify in `Mentoory.Web/wwwroot/css/mentoory.css`. (FR-005, FR-012)
- [ ] T011 [US2] Manual QA — swap + fallback (SC-003/FR-012): overwrite one `{slug}.svg` with a distinct image → art changes on reload, no rebuild; temporarily delete a `{slug}.svg` → band shows tint only, title unbroken. Restore the file afterward.

**Checkpoint**: Art is swappable per the file-path contract; missing files never break the header.

---

## Phase 5: User Story 3 - Inclusive and unobtrusive across devices (Priority: P3)

**Goal**: The band never crowds the title on narrow screens and is invisible to assistive tech / keyboard / print.

**Independent Test**: At a narrow viewport the title stays readable and uncrowded; a screen reader announces no band content; the band is never in the tab order; print preview shows no band.

### Tests for User Story 3 ⚠️

- [X] T012 [P] [US3] E2E test in `tests/Mentoory.Tests.E2E/` (Playwright, existing fixture) — on a representative authenticated page assert the `.page-header-band` is present, the page title is visible, and the band element is not reachable via keyboard tab order. (FR-008, SC-005)
- [X] T013 [P] [US3] Extend `tests/Mentoory.Tests.Integration/Web/HeaderBandRenderTests.cs` — assert the rendered `.page-header-band` carries `aria-hidden="true"` and contains no interactive descendants (no `<a>`/`<button>`/`tabindex`).

### Implementation for User Story 3

- [X] T014 [US3] Add responsive CSS in `Mentoory.Web/wwwroot/css/mentoory.css`: below the Bootstrap `md` breakpoint, suppress the `.page-header-band` SVG art layer (keep the gradient tint) so it never crowds a wrapped title (FR-009).
- [ ] T015 [US3] Manual QA — accessibility + responsive + print (quickstart): narrow-viewport crowding check, screen-reader silence, and print-preview suppression (FR-009/FR-011/SC-005).

**Checkpoint**: All three stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 Run `/simplify` and a self code-review on `HeaderTheme.cs`, the `_Layout.cshtml` edit, and the `mentoory.css` additions — small methods, no dead code, no magic strings, consistent with surrounding style.
- [X] T017 Build zero-warning (`dotnet build`, `TreatWarningsAsErrors=true`) and run `dotnet test tests/Mentoory.Tests.Integration` + `dotnet test tests/Mentoory.Tests.E2E` green.
- [ ] T018 Run the full `quickstart.md` manual-QA checklist end to end and record the outcome; address any failures before merge.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)**: none — start immediately.
- **Foundational (P2)**: after Setup — BLOCKS all stories (resolver + structural CSS).
- **US1 (P3 phase)**: after Foundational. MVP.
- **US2 (P4 phase)**: after US1 (the CSS class↔file binding it verifies is delivered in US1).
- **US3 (P5 phase)**: after US1 (extends the band element + CSS). Independent of US2.
- **Polish (P6)**: after all desired stories.

### Within Stories

- T002 (failing resolver tests) before T003 (implementation).
- T004 (structural CSS) + T006 (SVGs) before T007 (per-theme rules).
- T003 (resolver) before T008 (layout injection).
- T005 (render test) written before T007/T008 make it pass.

### Parallel Opportunities

- T002 ∥ (independent of T004).
- T005 ∥ T006 ∥ T007 — different files (test, SVGs, CSS) — though T007 depends on T006's files existing; sequence T006→T007, run T005 alongside.
- T012 ∥ T013 — E2E vs integration, different files.

---

## Parallel Example: Foundational

```bash
# T002 and T004 touch different files with no inter-dependency:
Task: "Write failing HeaderTheme.Resolve unit tests in tests/Mentoory.Tests.Integration/Web/HeaderThemeResolveTests.cs"
Task: "Add .page-header-band structural CSS in Mentoory.Web/wwwroot/css/mentoory.css"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational (resolver green, structural CSS).
2. Phase 3 US1: SVGs + per-theme CSS + layout injection + render test + contrast QA.
3. **STOP and VALIDATE**: distinct, legible band on every section. Demo-ready.

### Incremental Delivery

- US1 (MVP) → US2 (swap verification) → US3 (responsive + a11y). Each adds value without breaking the prior.

### Notes

- This spec is NOT opted into the prefixed-identifier coverage convention (only 016/018 are); no `[Trait("Spec","FR-021-NN")]` traits required by the coverage gate.
- Commit after each logical group; keep the unrelated `sqlserver2022-*` working-tree changes out of feature commits.
- No database, no EF, no JS — pure presentation.
