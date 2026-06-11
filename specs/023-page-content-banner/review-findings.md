# Deep Review Findings

**Date:** 2026-06-10
**Branch:** 023-page-content-banner
**Rounds:** 1
**Gate Outcome:** PASS-WITH-NOTES
**Invocation:** quality-gate (autonomous ship pipeline, ask=smart)

## Summary

| Severity | Found | Fixed | Remaining |
|----------|-------|-------|-----------|
| Critical | 0 | 0 | 0 |
| Important | 2 | 1 | 1 |
| Minor | 5 | 0 | 5 |
| **Total** | **7** | **1** | **6** |

**Agents completed:** 5/5 (Correctness, Architecture, Security, Production Readiness, Test Quality)
**External tools:** disabled via `--no-external` (CodeRabbit/Copilot not configured)
**Stage-1 spec compliance:** 100% (FR-001..FR-018 and C-01..C-07 all satisfied in code, pre-fix; FR-008/SC-002 had one regression on the Privacy view, now fixed)

## Findings

### FINDING-1 (FIXED)
- **Severity:** Important
- **Confidence:** 95
- **File:** Mentoory.Web/Views/Home/Privacy.cshtml:5-11
- **Category:** correctness / spec compliance (regression)
- **Source:** correctness-agent
- **Resolution:** fixed (round 1, commit d327f09)

**What was wrong:**
`Privacy.cshtml` rendered its own inline `<div class="page-header"><h2 class="page-title">@ViewData["Title"]</h2></div>`. The view uses the main `_Layout`, which after feature 023 also emits the relocated title via the banner strip (`<h2 class="page-title page-banner__title">`). For an authenticated user the title therefore rendered twice (two `h2.page-title` nodes).

**Why this matters:**
Directly violates FR-008 ("title only once; header band MUST no longer render the page title"), SC-002 ("0% of pages display the title more than once"), and contract C-04 ("title appears exactly once in the document"). The inline block predates 023, but 023's universal banner is what turns it into a visible duplicate — a regression introduced by this feature's scope.

**How it was resolved:**
Removed the redundant inline `page-header`/`h2.page-title` block from Privacy.cshtml, leaving the card content. The banner is now the single title source. Web project rebuilds clean (0 warnings, 0 errors). Committed as d327f09.

### FINDING-2 (REMAINING — recommend)
- **Severity:** Important
- **Confidence:** 95
- **File:** tests/Mentoory.Tests.Integration/Web/PageBannerRenderTests.cs:110-128
- **Category:** test-quality
- **Source:** test-quality-agent
- **Resolution:** pending (needs judgment — not auto-fixed)

**What is wrong:**
`UnmappedAction_RendersDefaultIcon_WithSectionColour` is named for the FR-005 default-icon (`layout-2`) render path but GETs `/Administration/Projects/Create` (a *mapped* action → `plus`) and asserts `ti-plus`, never `ti-layout-2`. The single assertion is wrapped in `if (response.StatusCode == HttpStatusCode.OK)` with no `else`, so if Create redirects/forbids (the comment admits it may) the test executes **zero assertions and passes vacuously**. The default-icon DOM path is consequently covered only by the pure unit test, never through rendered HTML.

**Why this matters:**
False-confidence test for the exact behavior it is named after; a real coverage hole for FR-005/SC-003 in the render path.

**Recommended fix (not applied — requires route judgment):**
Drive a route with a genuinely unmapped (non-CRUD) action reachable as IncubatorAdmin, assert `<i class="ti ti-layout-2 page-banner__icon" aria-hidden="true">` + the section slug, and remove the `if (StatusCode==OK)` guard (assert OK unconditionally like the sibling tests). NOT auto-fixed because choosing a reliably-reachable unmapped route for the test fixture is a design decision (a naive guard-removal could fail if Create isn't reachable for the test principal).

### FINDING-3 (REMAINING — recommend)
- **Severity:** Minor
- **Confidence:** 90
- **File:** Mentoory.Web/Views/Shared/_PageBanner.cshtml:18-21 (branch); no covering test
- **Category:** test-quality / spec coverage
- **Source:** test-quality-agent
- **Resolution:** pending

**What is wrong:**
FR-012 (title-less page renders the strip but omits the `<h2>`) has no test. The omit-branch is also near-unreachable in the live layout because `_Layout` computes `pageTitle = ViewData["Title"]?.ToString() ?? controller ?? ""`, so the branch only fires when both Title and controller are empty.

**Why this matters:**
The FR-012 omit-branch is untested and nearly dead; a refactor dropping the `?? controller` fallback could silently render `<h2></h2>` (the empty/placeholder text FR-012 forbids) with no test catching it.

**Recommended fix:**
Add an integration test rendering `_PageBanner` with blank `BannerTitle` asserting `html.Should().NotContain("page-banner__title")` while strip + icon still render. Optionally reconcile the `?? controller` fallback with FR-012's intent.

### FINDING-4 (REMAINING — recommend)
- **Severity:** Minor
- **Confidence:** 80
- **File:** tests/Mentoory.Tests.Integration/Web/PageBannerRenderTests.cs:104-107
- **Category:** test-quality
- **Source:** test-quality-agent
- **Resolution:** pending

**What is wrong:**
C-05 guarantees the header band still renders breadcrumb, **any PageActions**, and **_TopBar**. The test asserts `page-header` + `breadcrumb` + `page-pretitle` but never asserts a PageActions marker or a _TopBar marker.

**Recommended fix:**
Pick a route that defines a `PageActions` section, assert a stable marker from it inside `.page-header`, and assert a `_TopBar` marker (e.g., user-menu container). Acceptable as-is for now; partial C-05 coverage.

### FINDING-5 (REMAINING — recommend / design trade-off)
- **Severity:** Minor
- **Confidence:** 85
- **File:** Mentoory.Web/wwwroot/css/mentoory.css (.page-banner--{slug} accent block) vs 021 .header-band--{slug} (mentoory.css:346-384)
- **Category:** architecture / spec consistency (FR-006)
- **Source:** architecture-agent
- **Resolution:** pending (ambiguous — design decision)

**What is wrong:**
The banner reuses 021's section *hues* via `--banner-accent` but applies a single uniform `0.14` gradient opacity, whereas 021's authoritative `--band-tint` encodes a **per-section opacity** (incubadoras 0.09, auditoria 0.06, default 0.06, others 0.10–0.12). So the banner's muted/dark sections (auditoria, default, incubadoras) are stronger than the header band's on the same page.

**Why this matters:**
FR-006 requires the colour treatment "resolved consistently with the existing section theming." The hue is consistent; the opacity dimension is flattened. This is a subtle visual inconsistency, not a functional break — the section slug, hue family, and fallback all match (FR-006/FR-007 are substantially met). The data-model.md documents the accent triples as the intended source, so this is a documented v1 simplification rather than an accidental divergence.

**Recommended fix (deferred / judgment):**
If exact 021 fidelity is desired, carry the per-section opacity (e.g. push the full gradient per slug as 021 does, or add a `--banner-alpha` per slug). Otherwise document the uniform-opacity choice explicitly as an intentional deviation. NOT auto-fixed: this is a design trade-off, and the chosen single-gradient factoring is arguably cleaner than 021's repeated gradients.

### FINDING-6 (REMAINING — recommend)
- **Severity:** Minor
- **Confidence:** 80
- **File:** Mentoory.Web/Views/Shared/_PageBanner.cshtml:13-14
- **Category:** architecture / magic strings
- **Source:** architecture-agent
- **Resolution:** pending

**What is wrong:**
The partial hardcodes fallbacks `?? "default"` and `?? "layout-2"`, duplicating `HeaderTheme.Default` and `PageBannerIcon.Default`. CLAUDE.md flags "magic strings where constants exist." If a Default constant changes, the view's literal silently diverges.

**Recommended fix:**
`@using Mentoory.Web.Infrastructure` then `?? HeaderTheme.Default` / `?? PageBannerIcon.Default`. Low risk; the layout always sets both keys via the resolvers today, so the fallbacks are belt-and-suspenders. NOT auto-fixed (low value, touches the partial's contract-shaped header).

### FINDING-7 (REMAINING — recommend / SHOULD)
- **Severity:** Minor
- **Confidence:** 95
- **File:** Mentoory.Web/Views/Shared/_PageBanner.cshtml (banner div) / mentoory.css (.page-banner)
- **Category:** production-readiness / spec edge (Print)
- **Source:** production-readiness-agent
- **Resolution:** pending (attempted, reverted)

**What is wrong:**
The 021 header band wrapper carries `d-print-none`; the spec Print edge case says the strip SHOULD follow the same print-hiding. `.page-banner` has neither `d-print-none` nor an `@media print` rule, so a printed page shows the banner while the header band is suppressed.

**Why this matters:**
A SHOULD (Minor), not a blocker. Printed output is slightly inconsistent.

**Why not auto-fixed:**
Adding `d-print-none` to the banner div changes the element's class list to `page-banner page-banner--{slug} d-print-none`, which breaks the integration assertion at PageBannerRenderTests.cs:56 (`class="page-banner page-banner--{slug}"` expects a closing quote right after the slug) and the contract DOM in contracts/page-banner.md (which shows exactly `page-banner page-banner--{slug}`). Honouring it cleanly requires either an `@media print { .page-banner { display:none } }` CSS rule (no class change, no test impact) or a coordinated update to the contract + test. Recommended approach: add the `@media print` CSS rule (safest, no DOM contract change). Left for the implementer to keep the documented DOM contract authoritative.

## Post-Fix Spec Coverage

Code removed in fix round 1: the inline `page-header`/`h2.page-title` block in Privacy.cshtml. This removal *advances* spec compliance (eliminates the FR-008/SC-002/C-04 duplicate-title violation) rather than dropping a requirement. No FR is left unimplemented by the removal — the title is still rendered, now solely by the banner. All FR-001..FR-018 remain satisfied after the fix.

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| FR-001 strip on every auth _Layout page | _Layout.cshtml (page-body, first child) | ✓ |
| FR-002 title leading | _PageBanner.cshtml h2.page-banner__title | ✓ |
| FR-003 trailing decorative icon | _PageBanner.cshtml i.page-banner__icon | ✓ |
| FR-004 action→icon mapping | PageBannerIcon.cs (Index/Create/Edit/Details) | ✓ |
| FR-005 default icon | PageBannerIcon.Default = layout-2 | ✓ |
| FR-006 section colour reuses 021 | HeaderTheme.Resolve + page-banner--{slug} | ✓ (hue; opacity flattened — FINDING-5) |
| FR-007 section fallback | HeaderTheme default + page-banner--default | ✓ |
| FR-008 title once, not in band | h2 removed from band; Privacy fixed | ✓ (after fix) |
| FR-010 primary heading | h2 element | ✓ |
| FR-011 icon aria-hidden | aria-hidden="true" | ✓ |
| FR-012 title-less omits h2 | IsNullOrWhiteSpace guard | ✓ (untested — FINDING-3) |
| FR-013 no banner on auth/error layout | inside IsAuthenticated branch | ✓ |
| FR-014 ~60px height | min-height: 60px | ✓ |
| FR-015 no new image assets | gradient + webfont only | ✓ |
| FR-016 WCAG-AA contrast | transparent left region, ≤0.14 tint | ✓ |
| FR-017 long title no overflow | flex + min-width:0 + ellipsis | ✓ |
| FR-018 Spanish UI | no new user-facing strings | ✓ |

## Test Suite Results

The full test suite was not executed in this round: the integration/E2E suites require Testcontainers SQL Server + Playwright (heavy, container-dependent) and the only production change (Privacy.cshtml) was verified via `dotnet build` (0 warnings, 0 errors) and is covered by the existing C-04 render assertion (`<h2 class="page-title">` bare-match must be false), which the fix preserves (Privacy now emits no bare h2.page-title). The banner unit/render/E2E tests for 023 were read and assessed (see FINDING-2/3/4) but not run here.

| Round | Verification | Result |
|-------|-------------|--------|
| 1 | dotnet build Mentoory.Web | 0 warnings, 0 errors |

## Note: Pre-existing feature-018 coverage gate (independent of 023)

A solution-level `dotnet build` reports ONE error from the feature-018 `Mentoory.Specs.CoverageCheck` gate: unclaimed identifiers FR-018-06/07, SC-018-01/05/06, SC-016-05. Independently verified: all flagged IDs live in `specs/018-...` and `specs/016-...`; `git diff --name-only develop...HEAD` shows NO 016/018 files changed; spec 023 uses unprefixed IDs not enrolled in the gate (grep for those IDs in specs/023 returns nothing). This is pre-existing and NOT attributable to feature 023. The 023 production assemblies build clean on their own.

## Remaining Findings (gate decision)

One Important finding remains (FINDING-2, the vacuous/mis-named test) plus five Minor. None are Critical, and the one Important remaining is a test-quality defect (not a production behavior bug). The single production-behavior defect (FINDING-1, duplicate title) was fixed and committed. Gate outcome: **PASS-WITH-NOTES** — the remaining Important is a test-fixture judgment call surfaced for the pipeline/implementer rather than a blocker.
