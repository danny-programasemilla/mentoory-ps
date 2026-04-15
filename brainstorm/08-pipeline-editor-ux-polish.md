# Brainstorm: Pipeline Editor UX Polish

**Date:** 2026-04-14
**Status:** spec-created
**Spec:** specs/015-pipeline-editor-ux-polish/

## Problem Framing

The pipeline editor view (`/Coordination/ProjectPipeline`) shipped with 4 UX issues that hurt daily usability for coordinators:

1. Drag-and-drop reorder moves DOM elements but never POSTs the new order — the backend endpoint exists but the JS never calls it
2. State badges (En Progreso, Pendiente, Completada) use full-color Tabler badges with poor contrast against the white row background
3. The list uses `list-group-item` with `d-flex` — nothing aligns vertically across rows (handles, badges, names, buttons all flow inline at different widths)
4. Diagnosis stages show only the type label + display name; to see which form is linked, the user must click the config button and navigate to another page

## Approaches Considered

### A: Auto-save on drop + light badges + table + inline form names
- Pros: Instant persistence, minimal UI, DataTable alignment for free
- Cons: Every drag triggers a server call; full table conversion is heavier than needed

### B: Explicit save button + status dots + CSS grid + inline form names (Chosen)
- Pros: User control over save, lighter visual style, keeps card-like feel, form names visible inline
- Cons: Extra click to save, custom CSS grid required

## Decision

**Approach B** — explicit "Guardar Orden" button, status-dot indicators, CSS grid layout, and inline form names via cross-domain query join.

Rationale: The user preferred explicit save control (fewer accidental server calls), a lighter dot-based indicator style, and maintaining the card-like feel with CSS grid rather than converting to a full DataTable. The inline form name feature was agreed unanimously as the most impactful change — it eliminates a navigation step that coordinators currently hit on every pipeline review.

## Open Threads

- Should the "Guardar Orden" button position be in the header bar (next to "Agregar Etapa") or floating/sticky at the bottom? Spec says "header area" — implementer to decide.
- Tabler icon availability for filled-circle dots needs verification at implementation time.
