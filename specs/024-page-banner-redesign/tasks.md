# Tasks: Page Content Banner Redesign

**Feature**: 024-page-banner-redesign | **Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)

**Scope reminder**: CSS rewrite + 8 SVG assets + test updates. **No C# production-code changes,
no Razor markup changes.** The `_PageBanner.cshtml` / `_Layout.cshtml` / `HeaderTheme.cs` /
`PageBannerIcon.cs` files are reused verbatim. `[P]` = parallelisable (different files, no shared
edits).

---

## Phase 1 — Curated SVG assets (US1, US2)

Mirror the 021 header-SVG conventions (`viewBox="0 0 1200 240"`, `xmlns`, `aria-hidden`,
`fill="none"` root, ~2–4 KB) but **bold** (solid fills / high opacity), geometry weighted to the
right ~40–50%. Use the accent + motif from `data-model.md`. Shared-hue pairs MUST differ by motif
(FR-006): dashboard=mosaic vs personas=chevron; conocimiento=mosaic vs incubadoras=diagonal.

- [ ] **T001** Create directory `Mentoory.Web/wwwroot/img/banners/`.
- [ ] **T002 [P]** Author `banners/dashboard.svg` — accent #E07850, **mosaic** motif.
- [ ] **T003 [P]** Author `banners/proyectos.svg` — accent #F5B731, **chevron** motif.
- [ ] **T004 [P]** Author `banners/conocimiento.svg` — accent #D946A8, **mosaic** motif.
- [ ] **T005 [P]** Author `banners/diagnostico.svg` — accent #4299E1, **diagonal** motif.
- [ ] **T006 [P]** Author `banners/personas.svg` — accent #E07850, **chevron** motif.
- [ ] **T007 [P]** Author `banners/incubadoras.svg` — accent #D946A8, **diagonal** motif.
- [ ] **T008 [P]** Author `banners/auditoria.svg` — accent #1B2434 (slate), **mosaic** motif.
- [ ] **T009 [P]** Author `banners/default.svg` — accent #E07850, **diagonal** motif (fallback).

**Checkpoint**: 8 well-formed bold SVGs exist; each visibly distinct in colour and/or motif.

---

## Phase 2 — CSS rewrite (US1, US2, US3)

Single block in `Mentoory.Web/wwwroot/css/mentoory.css` (the existing `.page-banner` block,
~lines 394–465).

- [ ] **T010** Rewrite the shared `.page-banner` rule:
  - `min-height: 96px` (was 60px).
  - Layered background: `background-image: var(--banner-art, none), var(--banner-field, none);`
    with `background-repeat: no-repeat; background-position: right center; background-size: auto 100%;`.
  - `--banner-field`: a **bold** right-anchored colour gradient (hard edge near 50–56%, e.g.
    `linear-gradient(90deg, transparent 0 50%, rgba(var(--banner-accent), 0.92) 56% 100%)`) — a
    pure CSS layer that always paints (guarantees FR-021 missing-asset fallback).
  - Keep flex/centering, radius, `overflow: hidden`, `margin-bottom`.
- [ ] **T011** Per-slug rules (one per slug, mirroring `.header-band--{slug}`): set
  `--banner-art: url('/img/banners/{slug}.svg')` + `--banner-accent` (RGB triple from
  data-model.md) for `dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`,
  `incubadoras`, `auditoria`, `default`.
- [ ] **T012** `.page-banner__title`: confirm dark-on-light over the transparent left region; keep
  single-line truncation (`overflow/white-space/text-overflow`). Verify WCAG-AA (FR-009/SC-005).
- [ ] **T013** `.page-banner__icon`: restyle as a faint **light/white** watermark legible over the
  bold right field (tune opacity ~0.22–0.30); stays decorative, non-interactive.
- [ ] **T014** Responsive `@media (max-width: 767.98px)`: set `--banner-art: none` and hide
  `.page-banner__icon` (keep the colour field + title) — FR-020.
- [ ] **T015** `@media print`: keep `.page-banner { display: none; }` (FR-022).

**Checkpoint**: All in-scope pages render a bold ~96px per-area banner; title legible; icon reads
over the field; small-screen + print behave per spec.

---

## Phase 3 — Tests

- [ ] **T016 [P]** Add `tests/Mentoory.Tests.Integration/Web/BannerAssetsServedTests.cs`: for each
  of the 8 slugs, GET `/img/banners/{slug}.svg` returns 200, SVG content type, and a well-formed
  `<svg …` root. `[Trait("Category","Integration")]` via `WebApplicationFactory`; no DB. Guards
  FR-021 / SC-011 and catches a mis-named asset.
- [ ] **T017** Update `tests/Mentoory.Tests.E2E/Tests/PageBannerTests.cs`: change the height-band
  assertion from `>= 56` to `>= 88` (with a generous upper bound for padding slack) to enforce
  SC-006/FR-012; keep the single-title + visible-above-content assertions.
- [ ] **T018** Verify `PageBannerRenderTests` (markup contract) still passes **unchanged** — this is
  the regression guard that DOM/accessibility (FR-014/FR-016) did not regress. Do not edit it.

---

## Phase 4 — Polish & verification

- [ ] **T019** `dotnet build` clean (zero warnings — `TreatWarningsAsErrors`).
- [ ] **T020** Run `dotnet test tests/Mentoory.Tests.Integration` and
  `tests/Mentoory.Tests.E2E` (banner + header-band suites) green.
- [ ] **T021 [P]** Run `/simplify` over the changed CSS block; confirm no dead rules, no leftover
  023 gradient artefacts, comments updated to reflect the redesign (reference 024).
- [ ] **T022 [P]** Manual pass per `quickstart.md` acceptance list (bold/distinct, height, contrast,
  icon, single title, excluded pages, responsive, missing-asset fallback).

---

## Dependencies & parallelism

- T001 before T002–T009. T002–T009 are mutually `[P]` (separate files).
- T010 before T011 (shared rule defines the custom props the per-slug rules set). T012–T015 follow
  T010/T011 (same file, sequential edits to the block).
- Phase 3 tests depend on Phase 1 (assets) + Phase 2 (CSS) existing. T016 is `[P]` vs T017.
- Phase 4 last. T021/T022 `[P]`.

## Traceability

| Tasks | Requirements |
|-------|--------------|
| T002–T009 | FR-002, FR-003, FR-005, FR-006, FR-024, SC-002, SC-003, SC-004, SC-011 |
| T010, T011 | FR-002, FR-004, FR-007, FR-010, FR-012, FR-021, SC-006 |
| T012 | FR-008, FR-009, SC-005 |
| T013 | FR-011, FR-016, SC-007 |
| T014 | FR-020, SC-010 |
| T015 | FR-022 |
| T016 | FR-021, SC-011 |
| T017 | FR-012, FR-013, FR-014, SC-006, SC-008 |
| T018 | FR-014, FR-016, FR-017, SC-008 (regression guard) |
| T019–T022 | Constitution (zero warnings, Spanish UI), SC-001, SC-009, SC-010 |
