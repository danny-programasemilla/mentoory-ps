# Review Guide: Themed Header Band

**Spec:** [spec.md](spec.md) | **Plan:** [plan.md](plan.md) | **Tasks:** [tasks.md](tasks.md)
**Generated:** 2026-05-23

---

## What This Spec Does

The admin UI shares one flat page header on every screen, which makes the product feel like a generic admin tool. This feature paints a subtle, on-brand abstract band behind that header, with a different image per section (Proyectos, Conocimiento, Diagnóstico, etc.), so each area feels distinct and fresh without adding clutter. It reuses the visual language already shipped on the login/auth pages.

**In scope:** A decorative background band on the single authenticated page header; a curated set of 7 themed SVGs + a `default`; a controller→theme mapping; swappable image files; legibility, accessibility, responsive, and print guarantees.

**Out of scope:** Dark theme, animation/motion, generative or per-page-unique art, per-tenant configurability, and the auth pages (which already have their own decoration). These exclusions are listed in [Out of Scope](spec.md#out-of-scope).

## Bigger Picture

This is the latest in a run of UI-polish features (009 Tabler migration, 010/011 design-system + dashboard polish, 020 sidebar context footer). It sits entirely in the presentation layer — no domain, no database, no JS — so its blast radius is small. The one structural risk it must respect is recent: PR #20 removed duplicated per-page headers, so this feature is deliberately constrained to the single shared header (see [FR-010](spec.md#requirements)).

The contrast question below is the one genuinely external concern: WCAG AA contrast for text over imagery is a well-trodden accessibility problem, and the plan's answer (weight the art to the right, keep the text column over near-white) is a standard technique — worth a sanity check from anyone who has fought this before.

---

## Spec Review Guide (30 minutes)

### Understanding the approach (8 min)

Read [spec.md User Scenarios](spec.md#user-scenarios--testing-mandatory) and [research R1–R2](research.md) for how the band attaches and how text stays legible. As you read:

- The band is "full-width" ([FR-001](spec.md#requirements)) yet the contrast solution relies on keeping the **left** (text) region nearly untinted ([research R2](research.md)). Does "full-width band, but visually weighted right" match what was actually wanted, or did the brainstorm choice of "full-width tinted band" imply a more present, edge-to-edge tint?
- The mapping is keyed on **controller name**, not area ([data-model.md](data-model.md#thememapping-resolution-rule)). Given Administration alone spans Dashboard/Projects/Users/Audit, is controller-keying the right granularity, or would a few sections rather share an area-level identity?

### Key decisions that need your eyes (12 min)

**Right-weighted tint instead of per-theme contrast tuning** ([research R2](research.md), [FR-007](spec.md#requirements))
The plan avoids tuning each theme's tint for contrast by concentrating color/art on the right where only icons live.
- Question: is the right zone truly text-free on every page? The `_TopBar` holds the avatar name + active role text on `≥ xl` screens — does faint art behind that text risk dropping below 4.5:1? (Task [T009](tasks.md) is the contrast QA gate.)

**Controller→theme table** ([research R5](research.md), [data-model.md](data-model.md#thememapping-resolution-rule))
Some assignments are judgment calls: `AnswerCorrection → diagnostico`, `BatchUpload → personas`, `Sponsor → personas`.
- Question: do those groupings read as intuitive, or should `Sponsor` (a distinct external persona) get its own identity rather than sharing `personas` with admin user management?

**Authoring art in-house now, swappable later** ([spec Assumptions](spec.md#assumptions), [contracts §2](contracts/header-band.md))
The starter SVGs are engineer-authored; a designer can overwrite the files later with zero code change.
- Question: is the swap contract (file path + CSS-class binding, no code change) a strong enough guarantee that "engineer-authored v1" is acceptable to ship?

**Test split** ([research R9](research.md), [tasks Phase 2/3/5](tasks.md))
Resolver gets unit tests; structure gets integration HTML assertions; contrast/visual/responsive are manual QA + one Playwright check.
- Question: is leaving contrast (the highest-risk requirement) to manual QA acceptable, or should at least one theme get an automated computed-contrast assertion in E2E?

### Areas where I'm less certain (5 min)

- [research R2](research.md): I interpreted "full-width tinted band" (the approved brainstorm choice) as compatible with a right-weighted fade. If the intent was a uniformly visible tint edge-to-edge, the contrast strategy needs rethinking and per-theme tuning returns.
- [tasks T006](tasks.md): "Author 8 SVGs" is one task covering eight files. It's a cohesive set, but it bundles the most subjective, highest-effort work into a single checkbox — reviewers may want it split per theme or gated on a design check.
- [FR-013](spec.md#requirements) (no layout shift / no load regression): covered by manual QA ([T018](tasks.md)) only, no automated CLS assertion. For a background-image-only change this is likely fine, but it is an unautomated claim.

### Risks and open questions (5 min)

- If a themed SVG is missing at runtime, the band degrades to tint-only and the title stays intact ([FR-012](spec.md#requirements), [T010](tasks.md)). Is "silent degrade to tint" the right behavior, or should a missing asset be caught earlier (build-time check that every resolver slug has a file)?
- The band is injected inside the authenticated layout branch only ([T008](tasks.md), [FR-014](spec.md#requirements)). Worth confirming there is no second code path (e.g. error pages, modals rendered in a layout) that would surface a header without the band — or worse, a second header.
- Responsive behavior hides the art below `md` ([FR-009](spec.md#requirements), [T014](tasks.md)). On tablet widths where the title doesn't wrap but the right zone is tight, is hiding art too aggressive, or is "tint only" the right call there too?

---
*Full context in linked [spec](spec.md), [plan](plan.md), and [tasks](tasks.md).*
