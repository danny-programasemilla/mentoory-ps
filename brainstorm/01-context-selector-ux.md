# Brainstorm: Context Selector UX Redesign

**Date:** 2026-04-11
**Status:** spec-created
**Spec:** specs/008-context-selector-ux/

## Problem Framing

The post-login context selection page displays a flat card grid where each card represents a unique (role, incubator, project) combination. For users with multiple roles or GlobalAdmin users browsing many incubators, this grid becomes unwieldy and hard to scan. The user wanted simplicity — just 3 dropdowns that progressively narrow choices.

## Approaches Considered

### A: Cascading Dropdowns on Full Page + Modal in Top-Bar (Selected)
- Pros: Clean progressive filtering, modal is well-understood in Bootstrap, shared partial avoids duplication, fast in-app switching
- Cons: Slightly more frontend JS work for AJAX cascade + modal wiring

### B: Cascading Dropdowns — Page + Offcanvas Panel
- Pros: Offcanvas feels less intrusive than modal, shared partial reuse
- Cons: Offcanvas may feel unfamiliar in this app's existing patterns

### C: Wizard-Style Steps + Inline Top-Bar Dropdowns
- Pros: Very guided experience for first-time users
- Cons: Over-engineered for 3 simple dropdowns, inline nav dropdowns clutter the top-bar

## Decision

**Approach A selected.** Three cascading dropdowns (Role → Incubator → Project) on the full page, Bootstrap 5 modal with same dropdowns in the top-bar. Server-side AJAX for cascade filtering. Single-option dropdowns auto-select as read-only. Auto-skip preserved for single-context users.

Key design decisions:
- Server-side AJAX (not client-side filtering) — user preference
- Optional project selection — incubator-level roles can proceed without a project
- GlobalAdmin sees all incubators/projects in the system
- Cascade API endpoints include `roleAssignmentExternalId` for context submission

## Open Threads

- GlobalAdmin incubator dropdown may need search/filter if deployment scales to many incubators (deferred — current deployments are small)
