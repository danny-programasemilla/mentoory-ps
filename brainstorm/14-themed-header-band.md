# Brainstorm: Themed Header Band

**Date:** 2026-05-23
**Status:** spec-created
**Spec:** specs/021-themed-header-band/

## Problem Framing

The admin UI shares a single page header (breadcrumb + title) that reads flat and boring — the classic "admin website" feel. Goal: inject a fresh, eye-catching but unobtrusive visual into the header on each page, inspired by the existing brand look & feel, "just details, not overloaded." The brand already proves an abstract-minimal aesthetic on the auth pages (gradient `linear-gradient(135deg,#F5B731,#E07850,#D946A8)` + decorative circles/lines, `mentoory.css .auth-*`); this feature brings that energy into the authenticated app header.

## Approaches Considered

### A: CSS/SVG decoration band (code-drawn shapes, no asset files)
- Pros: zero asset management, fully themeable from existing tokens, near-zero weight, most consistent with current auth-page engine.
- Cons: less art-directable; harder to give each section a strong distinct identity.

### B: Curated SVG assets, one per semantic theme — **CHOSEN**
- Pros: distinct, art-directed identity per section; swappable files (designer can overwrite, zero code change); on-brand.
- Cons: a fixed art set to author and maintain; grows if new sections need their own identity.

### C: Generative per-page pattern (seeded from route)
- Pros: infinite variety, every page subtly unique, no asset files.
- Cons: hard to art-direct / keep tasteful; risks looking random; conflicts with "minimal, not overloaded".

## Decision

Chosen: **B — curated SVG assets, ~7 semantic themes + default**, with these forks settled:

- **Mapping:** per semantic theme (~7) — `dashboard, proyectos, conocimiento, diagnostico, personas, incubadoras, auditoria` + `default`. Related controllers share a theme (e.g. Users + Sponsor → `personas`; both Projects controllers → `proyectos`).
- **Art source:** authored in-house now, files at fixed paths (`wwwroot/img/headers/{theme}.svg`) so a designer can swap later with zero code change.
- **Placement:** full-width tinted band, title/breadcrumb overlaid — contrast becomes a hard requirement (WCAG AA ≥ 4.5:1, FR-007/SC-002).
- **Motion:** static, no animation.
- **Constraints:** single header only (no regression of the duplicate-header fix, PR #20); decorative/aria-hidden; responsive (art scales/fades on mobile); print-hidden; light-theme only.

Spec review gate: **SOUND** (no critical/important issues). See `specs/021-themed-header-band/REVIEW-SPEC.md` and `review_brief.md`.

## Open Threads

- Band vertical size / canvas dimensions — deferred to `/speckit-plan`.
- Background tint: per-theme colored wash vs. single neutral wash.
- Exact section→theme routing table (`Sponsor` → `personas`; `Configuration` / `BatchUpload` → `default`) — finalize in planning.
- Confirm app is light-theme only (assumption); dark-mode variants deferred if dark theme ever lands.
- Whether a strictly-static band should later allow a one-shot reduced-motion-aware entrance.
