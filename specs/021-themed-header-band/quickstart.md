# Quickstart: Themed Header Band

**Feature**: 021-themed-header-band

## What it is

A subtle, per-section abstract band behind the shared page header. The current controller decides which of 8 SVGs (`dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`, `auditoria`, `default`) paints, tinted with the section's brand accent and weighted to the right so the title stays crisp on the left.

## Replace a section's artwork (no code change)

1. Overwrite the file at `Mentoory.Web/wwwroot/img/headers/{slug}.svg` (e.g. `proyectos.svg`).
2. Keep `viewBox="0 0 1200 240"`, abstract/minimal, brand palette, composition readable when anchored right and clipped to header height.
3. Reload the section — new art appears. No rebuild of C# required (static asset).

## Add a new theme

1. Author `Mentoory.Web/wwwroot/img/headers/{newslug}.svg`.
2. Add a `.header-band--{newslug}` rule in `wwwroot/css/mentoory.css` (copy an existing rule, set the `url(...)` and the tint color).
3. Add the controller→`{newslug}` row in `Mentoory.Web/Infrastructure/HeaderTheme.cs`.
4. Add the matching row to the resolver unit test and `data-model.md` table.

## Change which section maps to which theme

Edit the single mapping table in `Mentoory.Web/Infrastructure/HeaderTheme.cs`. It is the only place route→theme lives. Update the resolver unit test to match.

## Manual QA checklist (run before merge)

- [ ] **Distinctness (SC-001)**: visit one page per theme + an unmapped page; each shows visibly distinct art; unmapped shows `default`.
- [ ] **Contrast (SC-002 / FR-007)**: on every theme, the page title and breadcrumb measure ≥ 4.5:1 against the band (browser devtools contrast checker or axe). Worst case is the most saturated tint (magenta `personas`?) — verify it.
- [ ] **Single header (FR-010)**: page source shows exactly one `.page-header`; existing breadcrumb / title / page actions / context + user menu all still work.
- [ ] **Accessibility (FR-008 / SC-005)**: screen reader announces no band content; Tab never lands on the band.
- [ ] **No layout shift (FR-013 / SC-004)**: header height unchanged vs. before; no CLS in devtools performance/Lighthouse.
- [ ] **Responsive (FR-009)**: at a narrow viewport the art does not crowd/overlap a wrapped title (art hidden, tint may remain).
- [ ] **Print (FR-011)**: print preview shows no band.
- [ ] **Swap (SC-003)**: temporarily replace one `{slug}.svg` with a different image; reload; art changes with no code change. Restore.
- [ ] **Static (FR-006)**: no animation/motion anywhere in the band.

## Automated tests

- `dotnet test tests/Mentoory.Tests.Integration` — `HeaderTheme.Resolve` mapping table + rendered-HTML band/structure assertions.
- `dotnet test tests/Mentoory.Tests.E2E` — band present, title visible, band not focusable (representative page).
