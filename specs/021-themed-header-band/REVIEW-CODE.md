# Code Review: Themed Header Band (021)

**Spec:** [spec.md](spec.md) · **Reviewer:** Claude (speckit.spex-gates.review-code) · **Date:** 2026-05-23

**Compliance: 100%** — all 14 functional requirements + 6 success criteria implemented per spec. No spec deviations. One recorded plan deviation (dropped `overflow: hidden`) that strengthens [FR-010](spec.md#requirements). Empirical verification of the visual requirements ([FR-007](spec.md#requirements)/[SC-002](spec.md#measurable-outcomes) contrast, [SC-003](spec.md#measurable-outcomes) swap, [SC-004](spec.md#measurable-outcomes) CLS, print) is deferred to the manual-QA tasks (T009/T011/T015/T018).

---

## Code Review Guide (30 minutes)

This section guides a code reviewer through the implementation changes, focusing on
high-level questions that need human judgment.

**Changed files:** 3 production files (`Mentoory.Web/Infrastructure/HeaderTheme.cs` — new;
`Mentoory.Web/Views/Shared/_Layout.cshtml` — 3-line edit; `Mentoory.Web/wwwroot/css/mentoory.css`
— one new section), 8 new SVG assets in `Mentoory.Web/wwwroot/img/headers/`, 2 new test files
(integration + E2E). No schema, no JS, no new dependency.

### Understanding the changes (8 min)

- Start with [`HeaderTheme.cs`](../../Mentoory.Web/Infrastructure/HeaderTheme.cs): the entire
  routing logic — a case-insensitive controller→slug dictionary and a 6-line `Resolve`. This is
  the only C# in the feature.
- Then the [`_Layout.cshtml`](../../Mentoory.Web/Views/Shared/_Layout.cshtml) edit: one `var`
  and one `<div>` injected as the first child of the single `.page-header`, inside the existing
  `User.Identity?.IsAuthenticated == true` branch.
- Then the *Themed header band* section of [`mentoory.css`](../../Mentoory.Web/wwwroot/css/mentoory.css):
  structural positioning, 8 per-theme rules (via `--band-art`/`--band-tint` custom props), and the
  responsive media query.
- Question: the design keeps art + tint **right-weighted** so the left text column stays over the
  body background. Does that read as the "full-width tinted band" the brainstorm intended
  ([FR-001](spec.md#requirements)), or is the left half too bare?

### Key decisions that need your eyes (12 min)

**`overflow: hidden` deliberately omitted** (`mentoory.css` `.page-header` rule, relates to [FR-010](spec.md#requirements))

The plan ([research R1](research.md#r1--rendering-technique-how-the-band-attaches-to-the-existing-header))
specified `overflow: hidden` on `.page-header`. It was dropped: the `_TopBar` user-menu/context
dropdowns live inside `.page-header` and open downward, so `overflow: hidden` would clip them. The
band doesn't need it (its SVG is a background, auto-clipped to the `inset:0` band box).
- Question: is relying on background-clipping (instead of `overflow: hidden`) robust across the
  themes, or do you want an explicit clip that can't ever leak the art outside the header?

**Controller-keyed mapping with conceptual groupings** (`HeaderTheme.cs`, relates to [FR-003](spec.md#requirements))

`Sponsor`/`BatchUpload`→`personas`, `AnswerCorrection`→`diagnostico`, `Knowledge`/`Templates`→`conocimiento`.
- Question: do those groupings match how the team thinks about these sections, or should e.g.
  `Sponsor` get a distinct identity rather than sharing `personas` with admin user management?

**Per-theme tint alphas** (`mentoory.css` `.header-band--*` rules, relates to [FR-007](spec.md#requirements))

Tints are hand-set low alphas (0.06–0.12). The most saturated (`proyectos` gold 0.12,
`personas`/`conocimiento`) are the contrast risk.
- Question: these need the human eye of T009 — does any theme's tint push the title/breadcrumb
  below 4.5:1, especially where the right-weighted gradient bleeds left on wide screens?

**Engineer-authored SVGs** (`wwwroot/img/headers/*.svg`, relates to [FR-004](spec.md#requirements))

Eight abstract SVGs authored in-house, swappable later by a designer with zero code change.
- Question: are these good enough to ship as v1, or should they gate on a design pass first?

### Areas where I'm less certain (5 min)

- `mentoory.css` `--band-art`/`--band-tint` custom-property layering ([research R6](research.md#r6--swap-contract-fr-005--sc-003)):
  I used `background-image: var(--band-art, none), var(--band-tint, none)` so the responsive rule
  can drop *only* the art by setting `--band-art: none`. This is cleaner than re-declaring 8
  gradients but depends on `none` being a valid per-layer value — confirmed rendering in tests,
  but worth a sanity check across target browsers.
- `HeaderTheme.Resolve` takes an unused `area` parameter ([contract §3](contracts/header-band.md)):
  kept for the documented signature and future disambiguation. If that's noise, it could be dropped.

### Deviations and risks (5 min)

- `mentoory.css` `.page-header`: dropped `overflow: hidden` vs [research R1](research.md#r1--rendering-technique-how-the-band-attaches-to-the-existing-header).
  Recorded in research.md + [contract §1](contracts/header-band.md). Question: acceptable, or do you
  want the explicit clip back with the dropdowns re-parented?
- Contrast ([FR-007](spec.md#requirements)/[SC-002](spec.md#measurable-outcomes)) and CLS
  ([SC-004](spec.md#measurable-outcomes)) have **no automated assertion** — they rest on manual QA
  (T009/T018). For a background-only change CLS is structurally zero, but contrast is the
  highest-risk requirement and is unverified until someone measures it. Question: is one automated
  computed-contrast E2E assertion worth adding before merge?
