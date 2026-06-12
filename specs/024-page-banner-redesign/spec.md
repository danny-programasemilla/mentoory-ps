# Feature Specification: Page Content Banner Redesign

**Feature Branch**: `024-page-banner-redesign`

**Created**: 2026-06-11

**Status**: Draft

**Input**: User description: "Redesign the feature-023 page content banner as a curated set of pre-authored per-area SVG banners. Replace the faint parametric gradient with bold, pre-defined per-area designs (solid colour fields + geometric accents), one per platform area, selected by the area being visited. Taller than 60px, title only, dark title on a clean left area, per-action icon retained. Supersedes feature 023's no-image-asset constraint. Source brainstorm: brainstorm/16-page-banner-redesign.md."

## Clarifications

### Session 2026-06-11

(Decisions carried in from brainstorm `16-page-banner-redesign.md`; recorded here as the resolved baseline.)

- Q: What is the selection axis for the banner set, and how many designs? → A: One distinct design per platform area (`dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`, `auditoria`) plus a `default` fallback — selected by the area being visited, reusing the existing section resolution. The per-action icon stays as the right-side overlay.
- Q: What are the per-area designs built from? → A: One lightweight, pre-authored SVG asset per area (vector, version-controlled). The set is pre-defined, not generated at runtime. This intentionally supersedes feature 023's "no new image asset files" constraint.
- Q: How tall is the banner and what text does it carry? → A: A compact band, taller than the previous ~60px (target band ~88–100px), carrying the page title only — no subtitle.
- Q: How do colour and title relate across the banner? → A: Bold colour and geometry occupy roughly the trailing (right) ~40–50% of the band; the title sits on the leading (left) side over a clean, light area in dark text, preserving WCAG-AA contrast with the least per-design risk.
- Q: Does the per-action icon change? → A: No. The contextual per-action icon is retained on the right, unchanged in behaviour from feature 023 (list/create/edit/details + default fallback).
- Q: What exact height within the 88–100px band? → A: Target **96px** as the nominal banner height; the 88–100px band is the acceptable tolerance for responsive/measurement variation. (Removes ambiguity for SC-006 / FR-012.)
- Q: Where does each area's colour come from — reuse the existing area colours or pick new ones? → A: Each area's colour is an **exact site-palette token** (`:root` in `mentoory.css`), rendered as a **bold colour field** rather than the previous ~14% wash. Most areas reuse the hue the platform already associates with them; the off-palette magenta is not used (see the 2026-06-11 revision). No new colour system is introduced.
- Q: Is the geometry bespoke per area or drawn from a shared set? → A: A **small, shared geometric vocabulary** (a bounded set of motifs such as chevron / diagonal / mosaic) is varied per area so each area is still visibly distinct (FR-006), keeping authoring bounded rather than fully bespoke per page.

### Session 2026-06-11 (post-implementation design revision)

Following a visual review of the first rendered result, two refinements were made (spec + code kept in sync):

- Q: The hard mid-band colour split read as the banner being "cut in half." → A: The colour treatment is now a **single full-width gradient** that runs edge-to-edge (faint tint on the left for title legibility, smoothly deepening to bold on the right) — no hard seam (revises FR-010).
- Q: The magenta hue used for `conocimiento` and `incubadoras` did not fit the site palette. → A: Both areas are recoloured to **documented palette tokens**: `conocimiento` → deep terracotta `#A85234` (`--mentory-primary-700`); `incubadoras` → green `#2FB344` (`--mentory-success`). All banner colours are now exact site-palette tokens; the off-palette magenta is removed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bold, area-identifiable page banner above content (Priority: P1)

A platform user navigating any authenticated page sees a bold, designed banner strip directly above the page content. Unlike the previous faint gradient wash, the banner now carries a strong, recognisable visual identity for the platform area being visited — a solid colour field with geometric accents (in the spirit of a professional banner template) — making each area of the platform instantly distinguishable at a glance. The page title sits clearly on the left, and a faint contextual icon on the right still reflects what the user is doing.

**Why this priority**: This is the core of the redesign. The previous banner's treatment was too faint to read as intentional design; this story replaces it with a curated, bold per-area banner. Without it there is no feature. It is independently valuable on its own.

**Independent Test**: Visit authenticated pages across several different platform areas (e.g. an incubators list, a projects page, a knowledge page, an audit page). Confirm each page shows a banner with a bold, area-specific design that is visually distinct between areas and clearly stronger than the previous faint gradient, with the page title legible on the left.

**Acceptance Scenarios**:

1. **Given** an authenticated user on a page within a given platform area, **When** the page loads, **Then** a banner appears directly above the page content showing that area's distinct bold design (colour field + geometric accents) with the page title legible on the left.
2. **Given** authenticated users on pages in two different platform areas, **When** each page loads, **Then** the two banners are visibly distinct from each other (different colour and geometric composition), not the same shape recoloured.
3. **Given** an authenticated user on any in-scope page, **When** the page loads, **Then** the banner's visual treatment is clearly bolder and more designed than the previous near-transparent gradient wash.

---

### User Story 2 - Consistent area-to-banner mapping with a defined fallback (Priority: P1)

A user moving between pages of the same area always sees the same banner design for that area, and a user on a page whose area has no specific banner sees a sensible default banner rather than a broken or empty strip. The banner that appears is chosen purely from the area being visited, consistent with how the rest of the platform already themes each section.

**Why this priority**: A curated set is only valuable if the right banner reliably appears for the right area, and if unmapped areas degrade gracefully. This makes the per-area identity trustworthy and predictable as the platform grows. Essential to the feature reading as intentional.

**Independent Test**: Visit multiple pages within the same area and confirm the identical banner design appears on each; then visit a page in an area with no specific banner and confirm the default banner appears with no layout breakage.

**Acceptance Scenarios**:

1. **Given** several pages within the same platform area, **When** each loads, **Then** the same area banner design appears on all of them.
2. **Given** a page in an area that has no specific banner mapping, **When** it loads, **Then** the default banner design appears, with no empty boxes, broken images, or layout breakage.
3. **Given** the existing section resolution used elsewhere in the platform, **When** a banner is selected, **Then** it uses that same area identity (the banner area mapping is consistent with the existing section theming, not a separate independent mapping).

---

### User Story 3 - Legible title and retained contextual icon across all banners (Priority: P1)

A user reading any page can always read the page title clearly over the banner, regardless of the area's colour, and still sees the small contextual icon on the right indicating the kind of page (browsing a list, creating, editing, viewing). The title remains the page's single primary title, not duplicated in the header band above.

**Why this priority**: A bold banner is worthless if it makes the title hard to read or loses the action cue. Legibility and the retained icon are what keep the redesign usable, not just decorative. Equal in importance to the visual redesign itself.

**Independent Test**: Across every area banner, confirm the title text meets readable contrast and is never obscured by the colour field or geometry, the per-action icon still appears on the right with the correct glyph for the action, and the title appears only once on the page (in the banner, not repeated in the header band).

**Acceptance Scenarios**:

1. **Given** any area banner, **When** a page loads, **Then** the page title is rendered in dark text over a clean, light region on the left and remains clearly legible (meets WCAG-AA contrast) over that area's treatment.
2. **Given** a list, create, edit, or details page, **When** it loads, **Then** the correct per-action icon (list/add/edit/view respectively) appears on the right; an action outside those four shows the neutral default icon.
3. **Given** any in-scope page, **When** it loads, **Then** the page title appears exactly once — in the banner — and is not repeated in the header band above, which continues to show the breadcrumb, action buttons, and top bar.

---

### Edge Cases

- **Login and error pages**: These use a different layout and MUST NOT show the banner; the feature applies only to authenticated pages on the main application layout (unchanged from feature 023).
- **Long titles**: A very long page title MUST remain readable within the band without overflowing into or overlapping the right-side colour/geometry or the icon; it truncates or wraps gracefully while the band keeps its height.
- **Title-less pages**: A page that declares no title still renders the banner (area design + icon) and simply omits the title text — never an empty or placeholder title (unchanged behaviour from feature 023).
- **Small screens**: On narrow viewports the banner must remain legible and uncluttered; the title and the area's colour identity remain, and the decorative right-side detail degrades gracefully (consistent with how the header band hides its decorative art below the same breakpoint).
- **Area not recognised**: A page in an area with no specific banner falls back to the default banner, identical in spirit to how the header band handles unmapped sections.
- **Asset unavailable**: If an area's banner asset cannot be displayed, the banner MUST still degrade to a coherent coloured band with the legible title and icon, never a broken-image placeholder or empty box.
- **Print**: The banner is decorative chrome and follows the same print-hiding behaviour as the existing page header (unchanged from feature 023).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST display a content banner on every authenticated page that uses the main application layout, positioned inside the page content area, directly above the page's main content and below the existing themed header band (scope unchanged from feature 023).
- **FR-002**: The banner's visual treatment MUST be drawn from a **curated set of pre-defined, pre-authored designs** — one design per platform area — rather than produced by a single runtime-generated formula. The set MUST be fixed and version-controlled, not generated dynamically at request time.
- **FR-003**: Each area's banner design MUST present a **bold, designed appearance** — a solid colour field with geometric accents — clearly stronger and more visually distinct than feature 023's near-transparent gradient wash, consistent with the platform's existing brand design language.
- **FR-004**: The banner shown for a page MUST be selected by the **platform area being visited**, resolved consistently with the existing section/area identity used by the header band (the same area-resolution mechanism, not a separate independent mapping).
- **FR-005**: The system MUST provide a distinct banner design for each of the platform's recognised areas at minimum: `dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`, and `auditoria`.
- **FR-006**: Two different recognised areas MUST be visibly distinguishable by their banners — each area MUST differ in both colour and geometric composition (not merely the same shape recoloured).
- **FR-007**: For an area without a specific banner design, the system MUST apply a **default banner**, consistent with the existing header band's fallback for unmapped sections, with no layout breakage.
- **FR-008**: The banner MUST present the page's title on its leading (left) side as the page's single, primary title, rendered in dark text over a clean, light region.
- **FR-009**: The page title MUST meet WCAG-AA contrast (ratio ≥ 4.5:1 for normal text) against the banner background in the title region, on every area's banner design.
- **FR-010**: The banner's colour treatment MUST be a single full-width gradient that runs edge-to-edge as one integrated band (no hard mid-band seam): a faint tint of the area colour on the leading (left) side — light enough to keep the leading title region legible per FR-009 — smoothly deepening to a bold colour field on the trailing (right) side, where the geometric accents sit.
- **FR-011**: The banner MUST retain a single contextual icon on its trailing (right) side, treated as a faint decorative element, selected by the current page's action — mapping at minimum: list/index → a list icon, create → an add icon, edit → an edit icon, details/view → a view icon — with a neutral default icon for any unmapped action. This behaviour is unchanged from feature 023.
- **FR-012**: The banner MUST be taller than the previous ~60px strip, with a nominal height of **96px**, sitting within a compact band of approximately 88–100px (the acceptable tolerance).
- **FR-013**: The banner MUST carry the page title only — it MUST NOT introduce a subtitle, description line, or call-to-action element.
- **FR-014**: The system MUST display the page title only once across the header band and the banner; the header band MUST NOT render the page title (unchanged from feature 023).
- **FR-015**: The header band MUST continue to display the breadcrumb trail, page action buttons, and the top bar (notifications and user menu) unchanged.
- **FR-016**: The page title within the banner MUST be exposed as the page's primary heading in the document outline for assistive technologies; the decorative banner art and the contextual icon MUST be hidden from assistive technologies (they convey no information beyond the title and area already present).
- **FR-017**: For a page that does not declare a title, the banner MUST still render (showing the area design and the action/default icon) and MUST omit the title text entirely rather than rendering empty or placeholder text. An in-scope page MUST NOT have its banner hidden solely because a title is absent.
- **FR-018**: The banner MUST NOT appear on pages that use the authentication or error layouts (login, error pages).
- **FR-019**: A long page title MUST NOT overflow the banner or overlap the right-side colour/geometry or the icon; it MUST truncate or wrap gracefully while the banner keeps its intended height band.
- **FR-020**: On narrow viewports the banner MUST remain legible and uncluttered: the title and the area's colour identity are preserved, and the decorative trailing detail degrades gracefully, consistent with the breakpoint behaviour of the existing header band's decorative art.
- **FR-021**: If an area's banner asset cannot be displayed, the banner MUST degrade to a coherent coloured band carrying the legible title and icon — never a broken-image placeholder or an empty box.
- **FR-022**: The banner MUST follow the same print-hiding behaviour as the existing page header (decorative chrome, hidden in print).
- **FR-023**: All user-facing text introduced or affected by this feature MUST be in Spanish.
- **FR-024**: This feature intentionally **supersedes feature 023's prohibition on new image asset files** (023 FR-015 / SC-007). Introducing the curated per-area banner assets is a deliberate, recorded reversal of that constraint, not an oversight. The number of new assets MUST remain bounded to the curated per-area set (one per recognised area plus the default).

### Key Entities

- **Area banner**: A pre-authored, named visual design associated with exactly one platform area (or the default). Each has an area identity (the area slug it serves) and a bold visual composition (colour field + geometry). The set is fixed and curated; there is one banner per recognised area plus one default. No persistence or domain data is involved — this is presentation chrome derived from the current page's existing routing context (area + action) and its declared title.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of authenticated pages on the main application layout display the banner above their content.
- **SC-002**: Each of the seven recognised areas (`dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`, `auditoria`) displays its own distinct banner design; any page in an unmapped area displays the default banner — verifiable by visiting one page per area and comparing.
- **SC-003**: Any two recognised areas' banners are visibly distinguishable from each other in both colour and geometric composition (no two areas share an identical design merely recoloured), verifiable by side-by-side comparison.
- **SC-004**: The banner's treatment is clearly bolder than the previous implementation — a reviewer comparing the new banner against the feature-023 baseline confirms a solid colour field and visible geometry rather than a near-transparent wash, on every area.
- **SC-005**: The page title passes WCAG-AA contrast (ratio ≥ 4.5:1 for normal text) against the banner background in the title region on every area's banner design.
- **SC-006**: The banner height is a nominal 96px and sits within the 88–100px band on every in-scope page (taller than the previous ~60px), verifiable by measurement.
- **SC-007**: For the four standard actions (list, create, edit, details), 100% of pages show the action-appropriate icon; any page outside those four shows the default icon (never a missing or broken icon) — unchanged from feature 023.
- **SC-008**: 0% of pages display the page title more than once (no page shows the title in both the header band and the banner).
- **SC-009**: 0 login or error pages display the banner.
- **SC-010**: At a representative narrow viewport, every page's banner remains legible with no overlapping title and decoration and no horizontal overflow.
- **SC-011**: The total count of newly introduced banner assets equals the curated per-area set (seven recognised areas + one default = eight) and does not grow per page — no per-page or runtime-generated assets are produced.

## Assumptions

- The canonical scope predicate remains "an authenticated page rendered by the main application layout"; the auth/error-layout exclusion (FR-018) is its corollary. FR-001, FR-018, SC-001, and SC-009 all refer to this same page set (carried over from feature 023).
- The page title is sourced from the page's existing declared title (the value feature 023 already relocated into the strip); pages already set this consistently.
- The area identity used to select a banner is the same area/section slug the platform already resolves for the header band (feature 021/023). The recognised area set above mirrors that existing mapping; if the platform's area set changes, the banner set is expected to track it.
- The per-area palette reuses the colours the platform already associates with each area (the feature 021/023 area accent hues), rendered as bold solid fields; no new colour system is introduced (resolved in Clarifications).
- The geometry is drawn from a small shared vocabulary varied per area, and the nominal height is 96px (resolved in Clarifications). The exact mechanism for displaying and recolouring the curated assets remains a planning detail that does not alter the requirements above.
- The feature reuses feature 023's existing plumbing for area resolution, action-to-icon mapping, title relocation, and layout scoping; only the visual treatment, the asset strategy, and the banner height change.
