---
status: spec-created
spec: specs/018-access-security-delivery-quality-gate/
date: 2026-04-19
---

# Brainstorm: Access-Security Delivery Quality Gate

**Date:** 2026-04-19
**Status:** spec-created
**Spec:** specs/018-access-security-delivery-quality-gate/

## Problem Framing

Feature 016 (registration-access-hardening) shipped with a coverage gap: several scenarios promised by the spec were only asserted at the unit level, leaving the admin path, public-path US3 variants, HTTP-layer byte-identity, form-state preservation, and defense-in-depth controls without integration or E2E coverage. The coverage gaps were catchable by a careful human reviewer but would silently regress over time. Beyond 016, the access-security work spanning features 003 (identity merge), 005 (access-security constitution), and 016 showed a consistent pattern of "response masking + structured outcome logging + public/admin attribution split" that the existing Section 11 testing-categories did not cover. The brainstorm aimed to produce a reusable, CI-enforced standard that both closes 016's gaps and prevents the same class of drift in every future access-security feature.

## Approaches Considered

### A: Narrow, feature-016-only closure
- Pros: fastest to land; unblocks PR #13 cleanly; no new infrastructure.
- Cons: no durability; the next access-security feature starts from zero; drift between spec and tests remains a manual-review concern.

### B: Scoped but reusable (chosen)
- Pros: closes 016 gaps immediately *and* codifies the pattern as a binding standard for future access-security work; automation catches drift mechanically; monolithic PR avoids tool-without-enforcement limbo.
- Cons: larger single PR surface; constitution amendment requires coordination; new tool adds a maintenance surface.

### C: Project-wide E2E methodology
- Pros: maximum coverage, uniform standard across all domains.
- Cons: too large to bundle with 016 unblocking; access-security is where the response-masking patterns concentrate, so binding Tenant/Diagnostic/etc. adds ceremony without proportional value.

## Decision

**Option B.** Monolithic PR delivering: (1) six new floor categories in the access-security constitution, (2) a `dotnet` console tool parsing specs for `FR-###`/`SC-###` IDs and reflecting over test assemblies for xUnit `[Trait("Spec",…)]` / `[Trait("Sc",…)]` claims, (3) MSBuild integration so `dotnet build` runs the check, (4) a required GitHub Actions `coverage-check` stage, (5) retrofit of trait attributes onto 016's existing 124 tests, (6) ~7 new automated scenarios closing 016's coverage gaps. Standard bound to access-security features via `access-security: true` spec front-matter; other domains adopt later.

## Key design choices (locked during Q&A)

- **Checkpoints = automated CI stages** — user ruled out all manual checkpoints. Every gate is a pass/fail in a test runner or the coverage tool.
- **Completeness = category floor + per-feature FR/SC traceability matrix** (layered). Floor categories catch patterns not captured by written requirements; matrix catches feature-specific drift.
- **Traceability = xUnit `[Trait]` attributes** + a build-time parser. YAML manifest and naming-convention alternatives rejected (second source of truth / brittle to renames).
- **Test layers = unit / integration / E2E, shallowest that faithfully asserts** the property. HTTP-header parity at integration; rendered-DOM checks at E2E; outcome mapping at unit.
- **Constitution amendment target = existing `access-security-constitution.md`** (feature 005), additive to Section 11 and a new Delivery Quality Gate section. Preserves existing numbering.

## Open Threads

- **OQ-001:** Should `Coverage: N/A` exceptions require explicit reviewer approval per occurrence or is the inline justification clause sufficient?
- **OQ-002:** Coverage tool project path — `tools/`, `build/`, or `specs/tooling/`? Implementation-phase decision.
- **OQ-003:** Does the 50-probe count (FR-019) need configurability for a nightly deep-run lane?
- Whether to promote floor enforcement from opt-in (`access-security: true`) to heuristic-detected across the whole `specs/` tree — punted to a future initiative.
- Retroactive trait retrofit for features 001–015 — organic migration expected as those areas get touched.
