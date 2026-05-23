# Feature Specification: Sidebar Context Footer

**Feature Branch**: `020-sidebar-context-footer`
**Created**: 2026-05-22
**Status**: Draft
**Input**: User description: "Move the current-context information (Rol, current incubator, current project) to the bottom of the left panel, following a professional UI/UX aligned with the rest of the site, and simplify the header."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See current context in the sidebar, header decluttered (Priority: P1)

An authenticated user works across the platform. On every page they can glance at the bottom of the left sidebar to confirm which role, incubator, and project they are currently acting in. The page header no longer carries this information, so it reads as a clean breadcrumb + page title + actions area with only the notification bell and the user avatar on the right.

**Why this priority**: This is the core of the request — relocating the context display and simplifying the header. Without it there is no feature.

**Independent Test**: Log in, navigate to any authenticated page, confirm the context (role, and incubator/project when present) is shown at the bottom of the sidebar and is absent from the page header.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an active role, incubator, and project, **When** they view any authenticated page, **Then** a context card at the bottom of the left sidebar shows the active role as the primary line and "Incubadora · Proyecto" as a secondary line.
2. **Given** an authenticated user, **When** they view the page header, **Then** the header shows only the breadcrumb, page title, optional page-action buttons, the notification bell, and the user avatar — no context badges.
3. **Given** an authenticated user, **When** they navigate between pages, **Then** the context card remains visible and consistent on each page.

---

### User Story 2 - Switch context from the sidebar card (Priority: P1)

A user who belongs to more than one context (multiple roles, incubators, or projects) clicks the context card at the bottom of the sidebar. The existing context switcher opens, they pick a new role/incubator/project, confirm, and the application reflects the new context. This is now the single, obvious place to switch context.

**Why this priority**: Switching must keep working after the relocation, and the brainstorm decision made the card the sole switch entry point. A display that breaks switching would be a regression.

**Independent Test**: As a multi-context user, click the sidebar context card, confirm the switcher opens, change context, and verify the new context is reflected on return.

**Acceptance Scenarios**:

1. **Given** a user with more than one selectable context, **When** they view the sidebar context card, **Then** the card is interactive (hover affordance and a switch indicator) and signals it can be clicked.
2. **Given** a user with more than one selectable context, **When** they click the card, **Then** the existing context switcher opens with its current behavior unchanged.
3. **Given** the switcher is open, **When** the user confirms a new selection, **Then** the active context changes exactly as it did before this feature, and the card updates to the new context.
4. **Given** the relocation, **When** a user opens the avatar dropdown, **Then** "Cambiar contexto" is no longer listed there (the sidebar card is the only switch entry point), while "Perfil" and "Cerrar sesión" remain.

---

### User Story 3 - Single-context user sees a clean, non-interactive card (Priority: P2)

A user who has exactly one context (nothing to switch to) sees the same context card, but it presents as a static display: no switch indicator, no hover affordance, not clickable. This avoids offering a dead control, consistent with the platform already skipping the context selector for single-context users.

**Why this priority**: Polish and professionalism; the feature is usable without it, but a dead affordance would undercut the "professional UI/UX" goal.

**Independent Test**: As a single-context user, view the sidebar card and confirm it shows the context but exposes no clickable switch affordance.

**Acceptance Scenarios**:

1. **Given** a user with only one selectable context, **When** they view the sidebar context card, **Then** it renders as a static display with no chevron/switch indicator and is not clickable.

---

### Edge Cases

- **No incubator and no project** (e.g., a platform-level / global administrator before selecting an incubator): the card shows the role only and hides the secondary line.
- **Incubator present, no project**: the secondary line shows the incubator only.
- **Long names**: role, incubator, or project names that exceed the sidebar width are truncated with an ellipsis, with the full value available on hover (tooltip / title).
- **Small screens / collapsed sidebar**: when the sidebar collapses behind a hamburger toggle, the context card appears at the end of the expanded menu rather than floating; switching is reached by opening the menu and tapping the card.
- **No active context at all** (unauthenticated or pre-context state): the card is not rendered (matches today's behavior where the header context only renders when a role is present).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST display the active context at the bottom of the left sidebar as a card with a leading icon, a primary line showing the active role, and a secondary line showing the active incubator and project (formatted as "Incubadora · Proyecto") when those values are present.
- **FR-002**: The context card MUST be positioned at the bottom of the sidebar, below the navigation menu, visually separated from the menu items, and pushed to the bottom of the available sidebar height on screens where the sidebar is expanded.
- **FR-003**: The system MUST NOT display the active-context indicators in the page header; the header retains only the breadcrumb, page title, optional page-action buttons, the notification bell, and the user avatar/menu.
- **FR-004**: When the user has more than one selectable context, clicking the context card MUST open the existing context switcher, with the switcher's selection-and-confirm behavior unchanged from its current implementation.
- **FR-005**: When the user has only one selectable context, the card MUST render as a static, non-interactive display with no switch affordance.
- **FR-006**: The system MUST make the sidebar card the sole entry point for switching context; the previous "Cambiar contexto" entry in the user/avatar dropdown MUST be removed.
- **FR-007**: The avatar/user menu MUST continue to provide "Perfil" and "Cerrar sesión", and the notification bell MUST remain in the header.
- **FR-008**: All user-facing text introduced or relocated by this feature MUST be in Spanish.
- **FR-009**: The card MUST truncate over-long role/incubator/project names with an ellipsis and expose the full value on hover.
- **FR-010**: The card MUST visually match the dark sidebar theme with sufficient contrast for legibility.

### Reuse & Integration Constraints

- **FR-011**: The feature MUST reuse the existing context switcher (modal markup, its element identifier, the shared selector partial, and its client-side behavior) without changing the switcher's logic — only the control that opens it relocates.
- **FR-012**: The relocated context indicator MUST remain discoverable by the existing automated context-locator hook (`data-testid="current-context"`) so that current tests continue to find it.
- **FR-013**: The feature MUST NOT introduce new server round-trips solely to render the context card; it reuses the context values already available to the page (active role, active incubator name, active project name).
- **FR-014**: The existing context-switching end-to-end tests MUST be updated so that the steps which previously opened the avatar dropdown and clicked "Cambiar contexto" instead exercise the sidebar context card, and MUST pass after the change.

### Key Entities

- **Active Context**: The user's currently selected working context — composed of an active role (always present when authenticated into a context), an optional active incubator, and an optional active project. This is read-only display data for this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On 100% of authenticated pages, the active context appears at the bottom of the sidebar and is absent from the page header.
- **SC-002**: A multi-context user can change context entirely from the sidebar card — open switcher, confirm, see the new context reflected — with the same number of confirmation steps as before (no added friction).
- **SC-003**: A single-context user is presented with zero clickable switch affordances in the context card.
- **SC-004**: The page header presents at most the breadcrumb, page title, optional action buttons, notification bell, and avatar — verifiable by inspection on representative pages.
- **SC-005**: All previously passing context-switching tests pass after being repointed to the sidebar card; the build completes with zero warnings.

## Assumptions

- The active role, active incubator name, and active project name are already available to every authenticated page (carried in the signed-in user's claims), so the card needs no new data source. This matches the current header implementation.
- The existing context switcher (the "Cambiar Contexto de Trabajo" modal, its shared selector partial, and its client-side script) is correct and stays as-is; this feature only moves the trigger and the read-only display.
- "Selectable context count > 1" is the condition that makes the card interactive. **How** that count is determined at render time (e.g., a value set at sign-in, a cached value, or an always-interactive fallback) is deferred to the planning phase and recorded as an open question.
- Mobile/responsive behavior follows the existing collapsing-sidebar pattern; no new mobile-specific navigation is introduced.
- Affected files are expected to be the shared layout partials for the sidebar and the header, the existing context-switcher assets (reused, not rewritten), and the existing context-switching end-to-end test (repointed). The switcher's cascading logic from the prior context-selector feature is out of scope.

## Out of Scope

- The context switcher's cascading selection logic (role → incubator → project) — unchanged.
- Notification/bell behavior and the (currently disabled) Profile page.
- Any change to how context is persisted or applied server-side.

## Open Questions

- **Switchability detection**: What is the cheapest reliable way to know, at layout render time, whether the user has more than one selectable context — a count carried in the user's claims set at sign-in, a cached lookup, or simply always opening the switcher (which already handles the single-option case as read-only)? To be resolved in `/speckit-plan`.
