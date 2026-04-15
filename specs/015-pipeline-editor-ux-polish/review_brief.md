# Review Brief: Pipeline Editor UX Polish

**Spec:** specs/015-pipeline-editor-ux-polish/spec.md
**Generated:** 2026-04-14

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

The pipeline editor view (`/Coordination/ProjectPipeline`) has 4 UX issues: drag-and-drop reorder doesn't persist, state badges have poor contrast, the layout is misaligned vertically, and Diagnosis stages require an extra click to see linked forms. This spec fixes all four as a focused UX polish pass — no domain or command changes, only view-layer and query-layer improvements.

## Scope Boundaries

- **In scope:** Reorder persistence via explicit save button, status-dot indicators, CSS grid layout, inline form names for Diagnosis stages
- **Out of scope:** Unsaved-changes browser warning, DataTable conversion, touch/mobile drag-and-drop, domain entity changes
- **Why these boundaries:** This is a UX polish pass on an already-implemented feature. The backend (Reorder action, StageFormAssignment) already works — we're fixing the client-side gap and improving visual presentation.

## Critical Decisions

### Save button vs. auto-save on drop
- **Choice:** Explicit "Guardar Orden" button that appears only when order has changed
- **Trade-off:** Extra click required, but gives user clear control and avoids accidental saves
- **Feedback:** Is the explicit save model the right UX for coordinators who may reorder multiple stages in one session?

### Status dots vs. improved badges
- **Choice:** Small colored dots + text labels instead of full-color badge rectangles
- **Trade-off:** Lighter visual weight overall, relies on color + text rather than badge shape for state distinction
- **Feedback:** Do dots provide enough visual weight for quick scanning?

### CSS grid vs. table conversion
- **Choice:** CSS grid on the existing list container, keeping card-like feel
- **Trade-off:** Maintains current visual identity but requires custom grid CSS rather than leveraging DataTable infrastructure
- **Feedback:** Is CSS grid worth the custom CSS, or would a simple `<table>` be more maintainable?

## Areas of Potential Disagreement

### Cross-domain query join for form names
- **Decision:** Pipeline query handler joins to StageFormAssignment + ProjectForm (Diagnostic domain) to populate form names
- **Why this might be controversial:** Crosses bounded context boundaries at the query level
- **Alternative view:** Could use a separate AJAX call or a dedicated read model
- **Seeking input on:** Is the cross-domain read join acceptable here, or should form names be fetched separately?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Save button | "Guardar Orden" | Spanish, appears after drag reorder |
| No-form hint | "Sin formulario" | Muted text for unassigned Diagnosis stages |
| DTO property | AssignedFormNames | List of strings on PipelineStageDto |

## Open Questions

- (None — all decisions were made during brainstorming)

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Tabler icon set may not include a filled-circle icon | Low | Fall back to CSS-only dot (border-radius) |
| CSS grid may not align perfectly with existing Tabler card styles | Low | Test visually, adjust grid gap/padding to match |

---
*Share with reviewers before implementation.*
