# Review Brief: Themed Header Band

**Spec:** specs/021-themed-header-band/spec.md
**Generated:** 2026-05-23

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Admin pages currently share one flat, uniform page header and feel boring. This feature adds a subtle, on-brand abstract decorative band behind the existing breadcrumb + title, with a different image per section (Dashboard, Proyectos, Conocimiento, Diagnóstico, Personas, Incubadoras, Auditoría, plus a default). The art reuses the brand language already proven on the auth pages (gradient + coral/gold/magenta + minimal geometry). It is static, decorative-only, light-theme, and must never hurt title legibility, accessibility, or load.

## Scope Boundaries

- **In scope:** A full-width decorative band in the single shared authenticated header; a curated ~7-theme image set + default; section→theme mapping with default fallback; swappable image files; WCAG-AA legibility; decorative/aria-hidden art; responsive behavior; print suppression.
- **Out of scope:** Dark theme, generative/per-page-unique art, any motion, per-tenant/user-configurable art, auth pages (already decorated).
- **Why these boundaries:** Keep the change a pure presentation enhancement — visible freshness with zero risk to data, security, or the recently-fixed single-header structure.

## Critical Decisions

### Curated per-section art (not generative)
- **Choice:** ~7 hand-authored theme images + default, mapped by section.
- **Trade-off:** Distinct, art-directed, on-brand identity vs. a fixed set to maintain (grows when new sections need their own identity; otherwise they inherit `default`).
- **Feedback:** Is ~7 the right granularity, or should any section currently folded into another get its own theme?

### Full-width band with title overlaid
- **Choice:** Tinted band spans the header; title/breadcrumb sit over the art.
- **Trade-off:** More visual presence and "fresh air" vs. a contrast obligation (FR-007: ≥ 4.5:1 on every theme).
- **Feedback:** Comfortable with the contrast bar as the guardrail, or prefer a lighter touch (e.g. right-anchored motif)?

### Authored in-house now, swappable later
- **Choice:** We draw the starter set; files live at fixed paths so a designer can overwrite them with no code change.
- **Trade-off:** Ships complete immediately vs. the initial art is engineer-authored, not designer-authored (mitigated by the swap contract).

## Areas of Potential Disagreement

### Static vs. a touch of motion
- **Decision:** Fully static, no animation.
- **Why this might be controversial:** Some would argue a one-shot fade-in adds life "for free".
- **Alternative view:** Gentle entrance animation, reduced-motion-aware.
- **Seeking input on:** Keep it strictly static, or allow a one-shot entrance later?

### Full-width band vs. minimal motif
- **Decision:** Full-width tinted band.
- **Why this might be controversial:** A band has more banner-like presence; "not overloaded" was a stated goal.
- **Alternative view:** A small right-anchored motif keeps the title on a plain background.
- **Seeking input on:** Is the full-width treatment still the desired level of presence?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Themes | `dashboard`, `proyectos`, `conocimiento`, `diagnostico`, `personas`, `incubadoras`, `auditoria` | Internal theme identifiers (not user-facing text) |
| Fallback | `default` | Applied to any unmapped section |

## Open Questions

- [ ] Band vertical size / canvas dimensions — deferred to `/speckit-plan`.
- [ ] Background tint: per-theme colored wash vs. a single neutral wash.
- [ ] Exact section→theme routing table (e.g. `Sponsor` → `personas`; `Configuration` / `BatchUpload` → `default`) — finalize in planning.
- [ ] Confirm the app is light-theme only (assumption).

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Title illegible over busy art | High | FR-007/SC-002 contrast bar (≥ 4.5:1); low art opacity + tint tuned per theme |
| Reintroducing duplicate headers | Medium | FR-010 single-header guarantee; integrate only in the shared layout header |
| Layout shift / load regression | Medium | FR-013/SC-004 reserved band space, lightweight static vector art |
| Art crowds title on mobile | Medium | FR-009 scale-down/fade on narrow viewports |

---
*Share with reviewers before implementation.*
