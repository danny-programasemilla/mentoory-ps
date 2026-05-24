# Feature Specification: Themed Header Band

**Feature Branch**: `021-themed-header-band`  
**Created**: 2026-05-23  
**Status**: Draft  
**Input**: User description: "Themed header band: add a subtle, on-brand full-width decorative band behind the shared page header (breadcrumb + title), showing a themed abstract image resolved per section. Curated ~7-image set authored in-house (dashboard, proyectos, conocimiento, diagnostico, personas, incubadoras, auditoria) plus a default fallback, mapped by section. Files at fixed paths so a designer can swap them with zero code change. Static (no animation). Title + breadcrumb must stay WCAG AA legible over the band. Decorative only. Responsive. One header only — must not regress the duplicate-header fix. Light theme only."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Distinct, fresh visual identity per section (Priority: P1)

A signed-in user moves between the main sections of the platform (e.g. Dashboard, Proyectos, Conocimiento, Diagnóstico). On each section, the page header carries a subtle, on-brand abstract visual that differs from the other sections, so the app feels fresh rather than flat, and the user gets an at-a-glance cue of where they are.

**Why this priority**: This is the entire point of the feature — replacing the boring, uniform admin header with a tasteful per-section identity. Without it there is no feature. It is also the slice that delivers visible value on its own.

**Independent Test**: Sign in and visit one page in each mapped section plus one unmapped page. Confirm each mapped section shows a visibly distinct band, the unmapped page shows the default band, and on every page the breadcrumb and title remain crisp and fully readable.

**Acceptance Scenarios**:

1. **Given** a signed-in user on the Dashboard, **When** the page loads, **Then** the header shows the `dashboard` themed band behind the breadcrumb and title.
2. **Given** a signed-in user, **When** they navigate from a Proyectos page to a Conocimiento page, **Then** the header band changes from the `proyectos` art to the `conocimiento` art.
3. **Given** a signed-in user on any page whose section is not explicitly mapped, **When** the page loads, **Then** the header shows the `default` band and no error occurs.
4. **Given** any themed band is shown, **When** the user reads the breadcrumb and page title, **Then** the text is fully legible over the band.

---

### User Story 2 - Swappable art with no code change (Priority: P2)

A brand owner or designer wants to refine or replace the abstract artwork for a section later, without involving a developer or a code deploy. They overwrite the image file for that theme; the section's header art updates on next load.

**Why this priority**: Protects the investment and keeps the brand evolvable. Valuable but not required for the first visible win, so it ranks below P1. The fixed-path contract must be designed in from the start even though the swap is exercised later.

**Independent Test**: Replace one theme's image file with a clearly different image, reload the corresponding section, and confirm the new art appears with no code or configuration change.

**Acceptance Scenarios**:

1. **Given** the `proyectos` theme image file is replaced with different artwork, **When** a Proyectos page is reloaded, **Then** the new artwork is shown in the header band.
2. **Given** a theme's image file is removed entirely, **When** a page in that section loads, **Then** the header falls back to the `default` band and the title remains intact.

---

### User Story 3 - Inclusive and unobtrusive across devices (Priority: P3)

A user on a small screen, or one relying on assistive technology, is never hindered by the decoration: the art does not crowd or obscure the title on narrow viewports, and screen-reader / keyboard users are not exposed to the purely decorative artwork.

**Why this priority**: Required for the feature to be acceptable in a professional, accessibility-conscious tool, but it refines the P1 experience rather than standing alone.

**Independent Test**: Load a themed page at a narrow (mobile) viewport and confirm the title stays readable and uncrowded. Inspect with assistive technology and confirm the artwork is not announced and is not in the tab order.

**Acceptance Scenarios**:

1. **Given** a themed page at a narrow viewport, **When** it renders, **Then** the art scales down or fades so it never overlaps the title illegibly.
2. **Given** a screen-reader user on a themed page, **When** they traverse the page, **Then** the decorative artwork is not announced and cannot receive keyboard focus.
3. **Given** a themed page is printed, **When** the print preview is generated, **Then** the band does not appear.

---

### Edge Cases

- **Missing or unresolvable theme image** → header falls back to the `default` band; the title and breadcrumb are never broken or hidden.
- **New / unmapped section** (e.g. a controller added later) → resolves to the `default` theme with no error.
- **Very long page title** → wraps within the header without breaking the band layout or reducing title contrast below the legibility threshold.
- **Print output** → the band is suppressed (header is already print-hidden).
- **Anonymous / authentication pages** (login, register, reset) → no themed band is applied; those pages keep their existing decoration.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST render a single full-width decorative band within the shared authenticated page header, positioned behind the breadcrumb and page title.
- **FR-002**: The band MUST display a themed abstract image resolved from the user's current section.
- **FR-003**: The system MUST map the current section to one of these themes — `dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`, `auditoria` — and MUST resolve any section without an explicit mapping to a `default` theme.
- **FR-004**: The system MUST provide a curated set of themed images covering the seven themes plus `default`, visually consistent with the existing brand language (brand gradient, coral/gold/magenta palette, minimal geometric abstract shapes).
- **FR-005**: Each themed image MUST reside at a fixed, predictable location so that replacing the image changes a section's art with no code or configuration change.
- **FR-006**: The band MUST be static — it MUST NOT animate, move, or transition.
- **FR-007**: The breadcrumb and page-title text MUST remain legible over the band, meeting WCAG AA contrast (≥ 4.5:1) on every theme.
- **FR-008**: The band artwork MUST be decorative only — hidden from assistive technologies, excluded from the keyboard tab order, and carrying no semantic meaning.
- **FR-009**: On narrow (mobile) viewports the band artwork MUST scale down or fade so it never crowds or illegibly overlaps the title; a faint background tint MAY remain.
- **FR-010**: The page header MUST remain a single header with no duplicate or per-page header reintroduced; the existing header zones (breadcrumb, title, per-page actions, context indicators, user menu) MUST continue to function unchanged.
- **FR-011**: The band MUST NOT appear in printed output.
- **FR-012**: If a theme's image is missing or cannot be resolved, the system MUST fall back to the `default` band without breaking the header.
- **FR-013**: The band MUST introduce no perceptible layout shift (its space is reserved) and no perceptible page-load delay.
- **FR-014**: The themed band MUST apply only to authenticated application pages, not to authentication pages, which retain their own existing decoration.

### Key Entities

- **Header theme**: A named visual identity for a section (e.g. `proyectos`), associated with exactly one abstract image and a set of sections it represents.
- **Theme mapping**: The rule set that resolves the current section to a header theme, including the `default` fallback for unmapped sections.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of authenticated pages display a header band, and the seven mapped themes plus `default` each render visibly distinct artwork.
- **SC-002**: On every theme, the breadcrumb and page-title text measure at least 4.5:1 contrast against the band.
- **SC-003**: Replacing a single theme's image file changes that section's header art on reload with zero code or configuration change (verified by a swap test).
- **SC-004**: The band contributes zero layout shift and adds no perceptible increase to page load time.
- **SC-005**: Assistive technology announces no header-artwork content, and the artwork is never reachable via keyboard tab order.
- **SC-006**: A section with no explicit theme mapping automatically displays the `default` band with no error.

## Out of Scope

- Dark-theme support (the application is light-theme only today); bands are tuned for the light background and dark-mode variants are deferred.
- Generative or per-page-unique artwork (the chosen approach is a curated per-section set).
- Any animation or motion in the band.
- Per-tenant or user-configurable header artwork.
- Authentication pages, which already have their own decoration.

## Assumptions

- The application is light-theme only; no dark theme is in use, so bands are designed for the existing light background.
- The single shared authenticated page header is the integration point, and the recent removal of duplicate per-page headers remains intact.
- The existing brand tokens and the auth-page abstract decoration are the visual reference for the new artwork.
- A granularity of roughly seven themes (related sections sharing one theme) is the right balance of distinctiveness versus a maintainable art set.
- The artwork is authored in-house as part of this feature and delivered as crisp, lightweight vector images, with files placed so a designer can later overwrite them.
