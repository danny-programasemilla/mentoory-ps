# Feature Specification: Page Content Banner Strip

**Feature Branch**: `023-page-content-banner`

**Created**: 2026-06-10

**Status**: Draft

**Input**: User description: "Add a slim, sober ~60px content banner strip to every authenticated page, rendered purely in CSS, sitting above page content and below the feature-021 themed header band. Section gradient + per-action Tabler icon watermark on the right. The page title relocates into the strip; the 021 header band drops its H2 but keeps breadcrumb + actions + topbar. Source brainstorm: brainstorm/15-page-content-banner.md."

## Clarifications

### Session 2026-06-10

- Q: When a page declares no title, does the strip still render (just without title text), or is it hidden entirely? → A: It still renders — the strip shows the section colour treatment and the (action/default) icon, and simply omits the title text. The strip is never hidden on an in-scope page solely because a title is absent.
- Q: On small/narrow viewports, what happens to the decorative right-side icon? → A: It is hidden below the same viewport breakpoint at which the existing header band hides its decorative art (the Bootstrap `md` breakpoint, < 768px), leaving the title and colour treatment intact. The strip's height band is preserved.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consistent, action-aware page banner above content (Priority: P1)

A platform user navigating any authenticated page (a list, a form, a detail view) sees a slim, on-brand banner strip sitting directly above the page's main content. The strip carries the page title on the left and a faint contextual icon on the right that reflects what the user is doing (browsing a list, creating, editing, or viewing a record). The strip's colour treatment reflects the section of the platform the user is in, giving each area a subtle, recognisable identity that is consistent with the rest of the brand system.

**Why this priority**: This is the core of the feature — a single, reusable banner that appears on every qualifying page and gives the page content a clear, branded visual anchor. Without it, there is no feature. It is independently valuable on its own.

**Independent Test**: Visit several authenticated pages across different sections and actions (e.g. an incubators list, a project edit form, a user detail view). Confirm a banner strip appears above the content on each, showing the correct page title, a section-appropriate colour treatment, and an action-appropriate icon.

**Acceptance Scenarios**:

1. **Given** an authenticated user on a section's list page, **When** the page loads, **Then** a banner strip appears directly above the page content (above any table and its filter controls) showing the page title on the left and a "list" icon on the right.
2. **Given** an authenticated user on a record creation page, **When** the page loads, **Then** the banner strip shows an "add/create" icon on the right.
3. **Given** an authenticated user on a record edit page, **When** the page loads, **Then** the banner strip shows an "edit" icon on the right.
4. **Given** an authenticated user on a record detail page, **When** the page loads, **Then** the banner strip shows a "view/details" icon on the right.
5. **Given** authenticated users on pages within two different sections, **When** each page loads, **Then** each banner strip uses the colour treatment associated with its own section, consistent with the existing section theming.

---

### User Story 2 - Single, non-duplicated page title (Priority: P1)

A user looking at any authenticated page sees the page title exactly once, presented prominently within the banner strip. The area above the strip (the existing header band) continues to show the breadcrumb trail, page action buttons, and the top bar (notifications, user menu), but no longer repeats the page title.

**Why this priority**: The banner strip becomes the home of the page title. If the title also remained in the header band above, the same text would appear twice within a small vertical span, which looks broken and unprofessional. Eliminating the duplication is essential to the feature reading as intentional.

**Independent Test**: Load any authenticated page and confirm the page title text appears once (in the strip) and is not repeated in the header band above it, while the breadcrumb, action buttons, and top bar remain present and functional in the header band.

**Acceptance Scenarios**:

1. **Given** any authenticated page, **When** the page loads, **Then** the page title appears in the banner strip and does not appear a second time in the header band above.
2. **Given** any authenticated page that previously displayed action buttons in the header band, **When** the page loads, **Then** those action buttons, the breadcrumb, and the top bar are still present in the header band.
3. **Given** a user relying on assistive technology, **When** they navigate the page, **Then** the page title is exposed once as the page's primary heading in the document outline.

---

### User Story 3 - Graceful, defined behaviour for unmapped or title-less pages (Priority: P2)

A user visiting a page whose action has no specific icon mapping, or a page that does not declare a title, still sees a coherent, non-broken banner strip rather than an empty or malformed element.

**Why this priority**: The platform has many pages and will grow more. The feature must degrade predictably for pages outside the common Index/Create/Edit/Details pattern, or pages that omit a title, otherwise the banner becomes a source of visual bugs. Important but secondary to the core experience.

**Independent Test**: Visit a page with an action that is not one of the standard four, and a page with no declared title. Confirm the strip still renders sensibly (default icon, section colour treatment, and either a sensible fallback for the title or a graceful absence of title text) with no empty boxes, stray icons, or broken layout.

**Acceptance Scenarios**:

1. **Given** a page whose action is not one of the standard mapped actions, **When** the page loads, **Then** the banner strip shows a neutral default icon and the section colour treatment, with no layout breakage.
2. **Given** a page that does not declare a title, **When** the page loads, **Then** the banner strip does not display empty or placeholder title text and remains visually coherent.

---

### Edge Cases

- **Login and error pages**: These use a different layout and MUST NOT show the banner strip; the feature applies only to authenticated pages on the main application layout.
- **Long titles**: A very long page title MUST remain readable within the ~60px strip without overflowing into or overlapping the right-side icon; it should truncate or wrap gracefully.
- **Small screens**: On narrow viewports the strip must remain legible and uncluttered. The decorative right-side icon is hidden below the same breakpoint at which the existing header band hides its decorative art (Bootstrap `md`, < 768px), while the title and colour treatment remain; the strip keeps its height band.
- **Section not recognised**: A page in a section with no specific colour mapping MUST fall back to the platform's default treatment, identical to how the existing header band handles unmapped sections.
- **Print**: The strip is decorative chrome and SHOULD follow the same print-hiding behaviour as the existing page header.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST display a horizontal banner strip on every authenticated page that uses the main application layout, positioned inside the page content area, directly above the page's main content and below the existing themed header band.
- **FR-002**: The banner strip MUST present the page's title on its leading (left) side as the page's single, primary title.
- **FR-003**: The banner strip MUST display a single contextual icon on its trailing (right) side, treated as a faint, decorative watermark.
- **FR-004**: The contextual icon MUST be selected by the current page's action, mapping at minimum: list/index → a list icon, create → an add icon, edit → an edit icon, details/view → a view icon.
- **FR-005**: For any action without a specific mapping, the system MUST display a neutral default icon (no missing or broken icon).
- **FR-006**: The banner strip's colour treatment MUST vary by platform section and MUST be resolved consistently with the existing section theming used by the header band, rather than introducing a separate, independent section mapping.
- **FR-007**: For a section without a specific colour mapping, the system MUST apply the platform's default treatment, consistent with the existing header band's fallback.
- **FR-008**: The system MUST display the page title only once across the header band and the banner strip; the header band MUST no longer render the page title.
- **FR-009**: The header band MUST continue to display the breadcrumb trail, page action buttons, and the top bar (notifications and user menu) unchanged.
- **FR-010**: The page title within the banner strip MUST be exposed as the page's primary heading in the document outline for assistive technologies.
- **FR-011**: The decorative icon MUST be hidden from assistive technologies (it conveys no information beyond the title and section already present).
- **FR-012**: For a page that does not declare a title, the banner strip MUST still render (showing the section colour treatment and the action/default icon) and MUST omit the title text entirely rather than rendering empty or placeholder text. An in-scope page MUST NOT have its strip hidden solely because a title is absent.
- **FR-013**: The banner strip MUST NOT appear on pages that use the authentication or error layouts (login, error pages).
- **FR-014**: The banner strip MUST be approximately 60px tall and present a sober, professional appearance consistent with the platform's existing brand design language (the gradient/abstract treatment seen on the login panel and header band).
- **FR-015**: The banner strip MUST be implemented without introducing any new raster or vector image asset files; its visual treatment is produced from existing styling primitives and the existing icon set.
- **FR-016**: The title text and any text within the strip MUST meet WCAG-AA contrast against the strip's background treatment.
- **FR-017**: A long page title MUST NOT overflow the strip or overlap the right-side icon; it MUST truncate or wrap gracefully while keeping the strip at its intended height band.
- **FR-018**: All user-facing text introduced by this feature MUST be in Spanish.

### Key Entities

Not applicable — this feature introduces no data, persistence, or domain entities. It is presentation-layer chrome derived from the current page's existing routing context (section + action) and its declared title.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of authenticated pages on the main application layout display the banner strip above their content.
- **SC-002**: 0% of pages display the page title more than once (no page shows the title in both the header band and the strip).
- **SC-003**: For the four standard actions (list, create, edit, details), 100% of pages show the action-appropriate icon; any page outside those four shows the default icon (never a missing/broken icon).
- **SC-004**: 0 login or error pages display the banner strip.
- **SC-005**: The strip's title text passes WCAG-AA contrast (ratio ≥ 4.5:1 for normal text) against its background on every section colour treatment.
- **SC-006**: Each platform section's strip uses the same colour family already associated with that section by the header band (visual consistency verifiable by side-by-side comparison), with unmapped sections falling back to the default treatment.
- **SC-007**: No new image asset files are added to the project as part of this feature.
- **SC-008**: At a representative narrow viewport, every page's strip remains legible with no overlapping title and icon and no horizontal overflow.

## Assumptions

- The page title is sourced from the page's existing declared title (the value already used by the header band today); pages already set this consistently.
- "Section" and "action" are derived from the current page's existing routing context (area/controller and action), the same inputs the existing header-band theming already consumes.
- The existing header-band section theming is the authoritative source for section colour treatment; this feature reuses it rather than redefining section→colour mappings.
- The standard action vocabulary for icon mapping is the conventional list/create/edit/details set; other actions (custom or non-CRUD) fall through to the default icon, which is acceptable for v1.
- On small viewports, hiding or reducing the decorative icon (mirroring the header band's existing responsive behaviour) is acceptable and preferred over cramming the strip.
- This is a presentation-only change: no database, schema, EF, Application, or Domain changes are in scope.
- Per-view overrides of the strip's icon or the addition of a per-page subtitle are out of scope for v1 and may be considered as a later enhancement.
