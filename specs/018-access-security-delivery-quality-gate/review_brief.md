# Review Brief: Access-Security Delivery Quality Gate

**Spec:** specs/018-access-security-delivery-quality-gate/spec.md
**Generated:** 2026-04-19

> Reviewer's guide to scope and key decisions. See full spec for details.

---

## Feature Overview

Establish a CI-enforced Access-Security Delivery Quality Gate that (1) codifies six testing floor categories in the access-security constitution, (2) ships a `dotnet` console tool that parses feature specs and asserts every `FR-###`/`SC-###` identifier is claimed by a test via xUnit `[Trait]` attributes, (3) wires a required GitHub Actions stage that runs the tool and blocks merge on drift, and (4) uses feature 016 as the first complete instantiation — retrofitting traits onto 016's existing 124 tests plus writing ~7 missing scenarios (admin duplicate-NID, admin fresh redirect, admin unauth gate, admin US3 parity, public US3 national-ID coverage, 50-probe response-equality sweep, form-state preservation, defense-in-depth verification). Humans do not read checklists; CI fails builds.

## Scope Boundaries

- **In scope:** constitution amendment (`.specify/memory/access-security-constitution.md`) + coverage tool + MSBuild integration + GitHub Actions stage + 016 retrofit + 016 gap-closure scenarios. Single monolithic PR per the brainstorm decision.
- **Out of scope:** retroactive retrofit of features 001–015; extending the gate to non-access-security areas (Tenant, Diagnostic, Mentoring, Knowledge, Subscription, Example); any modification to feature-016 production code; interactive/visual tests.
- **Why these boundaries:** the gate only earns its keep when it is *binding*, and binding a standard to undelivered features creates noise without protection. Instantiating against 016 delivers immediate value; the standard propagates organically as other features get touched.

## Critical Decisions

### Trait-based traceability (xUnit `[Trait]`) over YAML manifest or naming convention
- **Choice:** every test claims its covered FR/SC IDs via `[Trait("Spec","FR-008")]` / `[Trait("Sc","SC-002")]`.
- **Trade-off:** ties coverage claim to the test method (rename-safe, co-located with the test body) but requires a custom parser tool. YAML manifest alternative was rejected as a second source of truth that can drift from the tests.
- **Feedback:** is the Trait key naming (`Spec` / `Sc` / `Floor`) acceptable, or do you want `FR` / `SC` / `Category`?

### Monolithic PR rollout
- **Choice:** constitution + tool + CI + retrofit + new scenarios in one PR.
- **Trade-off:** ~25+ files and a new tool in a single review; reviewers must absorb a lot. Alternative (staged rollout) was rejected to avoid a "tool exists but enforces nothing" limbo window.
- **Feedback:** acceptable review surface or should this be split after all?

### Amend `.specify/memory/access-security-constitution.md` rather than create a new document
- **Choice:** add floor categories to Section 11 and a new Delivery Quality Gate section (either 11.9+ or 13+), preserving existing numbering.
- **Trade-off:** concentrates governance in one place but makes the amendment tightly coupled to that file's existing structure.
- **Feedback:** is this the right home, or should the delivery-gate section live in the root `constitution.md` as a new Principle?

## Areas of Potential Disagreement

### 50-probe sweep as the response-indistinguishability assertion (FR-019)
- **Decision:** a single in-process integration test fires 50 mixed submissions and asserts pairwise equality of status, `Location`, body bytes (antiforgery stripped), `Cache-Control`, `Content-Type`, and `Set-Cookie` cookie-name set.
- **Why this might be controversial:** 50 is arbitrary — statistically it is not the number that mathematically proves indistinguishability. A security reviewer could argue for 500 or for a property-based framework instead.
- **Alternative view:** run as part of a nightly deep-run at 500+ probes, keeping the CI-hot-path sweep at 10 probes.
- **Seeking input on:** is 50 a defensible per-CI-run number, or do you want a nightly deep-run lane (OQ-003)?

### Floor-category enforcement depends on `access-security: true` front-matter opt-in
- **Decision:** a spec must explicitly declare itself access-security-sensitive for floor enforcement to apply.
- **Why this might be controversial:** misses features that *should* be access-security but forgot to declare. Silent opt-out.
- **Alternative view:** heuristic detection (look for `[Authorize]` references, auth-related keywords, or specific file paths in the spec's scope).
- **Seeking input on:** opt-in vs. heuristic detection. Opt-in is chosen for simplicity + zero false positives; heuristic is more protective but harder to get right.

### `NFR-002` makes flaky tests a spec-level violation
- **Decision:** any FR-claimed test that fails non-deterministically in CI must be quarantined via `[Trait("Flaky","true")]` within 24 hours, at which point the coverage tool treats the test as non-claiming (so quarantines immediately surface as drift).
- **Why this might be controversial:** aggressive. Some teams tolerate low-rate flakes; this policy forces immediate action.
- **Alternative view:** allow a grace window (e.g., a week) before the drift signal fires.
- **Seeking input on:** is 24 h the right quarantine window?

## Naming Decisions

| Item | Name | Context |
|------|------|---------|
| Coverage tool | `Mentoory.Specs.CoverageCheck` | tentative; OQ-002 parks project-path choice |
| Trait attribute for FR claims | `[Trait("Spec","FR-008")]` | xUnit-native |
| Trait attribute for SC claims | `[Trait("Sc","SC-002")]` | symmetric with above |
| Trait attribute for floor claims | `[Trait("Floor","response-indistinguishability")]` | category names kebab-case |
| Quarantine trait | `[Trait("Flaky","true")]` | drives NFR-002 drift signal |
| CI stage names | `build`, `coverage-check`, `unit-tests`, `integration-tests`, `e2e-tests` | FR-002 |
| Spec front-matter opt-in | `access-security: true` | FR-008 trigger |

## Open Questions

- [ ] **OQ-001** Should `Coverage: N/A` require explicit reviewer approval per occurrence, or is the inline justification clause sufficient?
- [ ] **OQ-002** Where does the coverage tool's project live — `tools/`, `build/`, or a dedicated `specs/tooling/`?
- [ ] **OQ-003** Does the 50-probe count (FR-019) need to be configurable for a nightly deep-run?

## Risk Areas

| Risk | Impact | Mitigation |
|------|--------|------------|
| Tool's reflection-only loader (`MetadataLoadContext`) fails on certain assembly shapes (signed, native deps) | High — blocks builds | Fall back to runtime Assembly.Load with an isolated AppDomain; spec allows implementation choice |
| 50-probe sweep exceeds 15 s budget (NFR-004) due to `WebApplicationFactory` startup cost | Medium — slows CI | Amortise via xUnit collection fixture; FR-019 assertion uses in-process HttpClient, no Testcontainers cold start inside the sweep |
| Trait retrofit on 124 existing tests misses some tests or mis-claims IDs | High — false-green coverage | Pairs with FR-019's canary test (SC-005) — deliberate trait removal must fail CI, catching coordination bugs |
| Constitution amendment conflicts with parallel work on `access-security-constitution.md` | Medium — merge hell | Small amendment, additive only; coordinate with 005 owner if parallel work lands |
| Branch-protection configuration drift (DEP-005 is manual) | Medium — gate not actually enforced | PR body must include repository-admin action item and screenshot confirming the new required check |

---
*Share with reviewers before implementation.*
