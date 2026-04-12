# Review Brief: Cascading Context Selector UX

**Spec:** specs/008-context-selector-ux/spec.md  
**Generated:** 2026-04-11

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Replace the post-login context selection page (currently a flat card grid of every role+incubator+project combination) with 3 cascading dropdowns: Role, then Incubator (filtered by role), then Project (filtered by role+incubator). Add a Bootstrap 5 modal in the top-bar for in-app context switching without page navigation. The cascade is server-driven (AJAX per dropdown change), single-option dropdowns auto-select as read-only, and the existing auto-skip for single-context users is preserved.

## Scope Boundaries

- **In scope:** Full-page selector redesign, top-bar modal switcher, 3 new cascade API endpoints, extension of existing switch endpoint, shared partial view
- **Out of scope:** Database schema changes, authentication/cookie mechanics changes, role management UI, real-time permission refresh, Available Projects page
- **Why these boundaries:** This is a pure UX improvement — the domain model, auth flow, and database are sound. Only the presentation and cascade filtering are new.

## Critical Decisions

### Server-Side AJAX for Cascade
- **Choice:** Each dropdown change triggers a server-side API call (not client-side filtering)
- **Trade-off:** Adds network round-trips per dropdown interaction, but keeps data loading on the server and avoids shipping the full context tree to the client
- **Feedback:** Is the 500ms latency target sufficient for a good user experience?

### Bootstrap Modal for Top-Bar Switching
- **Choice:** Modal dialog (not offcanvas panel or inline dropdowns)
- **Trade-off:** Modal is more disruptive than offcanvas but is a well-established pattern in the project
- **Feedback:** Does the modal pattern fit the project's existing UX conventions?

### Optional Project Selection
- **Choice:** Users can proceed with only Role + Incubator (no project required)
- **Trade-off:** Simplifies flow for incubator-level roles, but means some users may forget to scope to a project
- **Feedback:** Should there be any nudge/reminder for project-scoped roles that skip project selection?

## Areas of Potential Disagreement

### GlobalAdmin Sees All Incubators
- **Decision:** GlobalAdmin role populates incubator dropdown with ALL active incubators in the system
- **Why this might be controversial:** For large deployments, this could be a long list
- **Alternative view:** Paginate or search-filter the incubator dropdown for GlobalAdmin
- **Seeking input on:** Is a flat dropdown sufficient, or should we plan for search/filter in the incubator dropdown?

### roleAssignmentExternalId in Cascade Responses
- **Decision:** Cascade API endpoints include `roleAssignmentExternalId` in their response so the frontend can submit it directly
- **Why this might be controversial:** Exposes internal assignment mapping in API responses
- **Alternative view:** Separate resolution endpoint that maps (role, incubatorId, projectId) to a roleAssignmentExternalId
- **Seeking input on:** Is embedding the ID in cascade responses acceptable, or should resolution be a separate call?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Full-page route | `/Context/Select` | Same as current (no URL change) |
| Cascade API | `/api/context/roles`, `/api/context/incubators`, `/api/context/projects` | RESTful resource naming |
| Shared partial | `_ContextSelector.cshtml` | Underscore prefix per ASP.NET convention |
| Confirm button | "Confirmar" | Spanish UI text |
| Context switch button | "Cambiar contexto" | Existing Spanish text preserved |

## Open Questions

- (none — all decisions resolved during brainstorming)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| AJAX latency feels sluggish | Med | 500ms target; loading spinner on dropdown while fetching |
| GlobalAdmin incubator list grows too large | Low | Current deployments are small; can add search/filter later if needed |
| Shared partial complexity (page vs modal mode) | Low | Partial receives a mode flag to control form-POST vs AJAX behavior |

---
*Share with reviewers before implementation.*
