# Phase 1 Data Model: Access-Security Delivery Quality Gate

**Feature**: `018-access-security-delivery-quality-gate`
**Date**: 2026-04-19
**Scope**: This feature adds no production domain entities. All entities below are logical models the coverage tool reasons over. They are not persisted; they exist only in-memory during a tool run.

---

## Entities

### FeatureSpec

A single `spec.md` file found under `specs/`.

| Field | Type | Notes |
|-------|------|-------|
| `Path` | `string` (absolute) | Absolute filesystem path to the `spec.md`. |
| `FeatureNumber` | `string` | Zero-padded numeric prefix extracted from the parent directory (e.g., `016`, `018`). |
| `Slug` | `string` | Human-readable part of the directory name (e.g., `registration-access-hardening`). |
| `AccessSecurityOptIn` | `bool` | `true` when front-matter contains `access-security: true`; `false` otherwise. Drives floor-category enforcement (FR-008). |
| `RequirementIds` | `IReadOnlyList<RequirementId>` | Every `FR-###` and `SC-###` declared in the spec body. |
| `Exclusions` | `IReadOnlyList<ExclusionMarker>` | Identifiers inline-marked `Coverage: N/A` with a valid justification. |

**Derivation**: Instances produced by `SpecParser.Parse(string specMdPath)`. Parsing is pure; no side effects.

**Validation rules** (surfaced as parse errors, exit code `2` per FR-005 + EH-001):
- Front-matter block that opens with `---` MUST close with `---` within the first 30 lines; otherwise malformed-frontmatter.
- `Coverage: N/A` markers that do not match the regex defined in research.md #10 are ignored (identifier treated as unclaimed per EH-004); a warning is printed with file path + line number.
- Duplicate `RequirementId` within the same spec → parse error naming all source lines.

---

### RequirementId

A single `FR-###` or `SC-###` token declared in a spec.

| Field | Type | Notes |
|-------|------|-------|
| `Kind` | enum `{ Fr, Sc }` | Determines which trait key claims it. |
| `Value` | `string` | The 6-character literal token (e.g., `FR-015`, `SC-002`). |
| `SpecPath` | `string` | Back-reference to the owning spec file. |
| `LineNumber` | `int` | 1-based line in the spec where the identifier is first declared. |

**Equality**: Two `RequirementId` instances are equal iff `Kind` + `Value` match. `SpecPath` and `LineNumber` are metadata, not identity.

**Collisions across specs**: Detected during analysis (not parsing) by grouping all `RequirementId` instances by `(Kind, Value)` and flagging groups with more than one distinct `SpecPath` → error per EH-003.

---

### ExclusionMarker

An identifier deliberately marked `Coverage: N/A` in a spec.

| Field | Type | Notes |
|-------|------|-------|
| `Target` | `RequirementId` | The identifier being excluded. |
| `Justification` | `string` | Free-text justification (min 20 chars per research.md #10). |
| `LineNumber` | `int` | 1-based line of the `*Coverage: N/A — …*` line in the spec. |

Exclusions are reported in a separate section of the tool's output (FR-006); they do NOT count as "unclaimed."

---

### TestAssembly

A single managed DLL discovered by the `--test-assemblies` glob.

| Field | Type | Notes |
|-------|------|-------|
| `Path` | `string` (absolute) | Physical path to the DLL. |
| `AssemblyName` | `string` | Short name (e.g., `Mentoory.Access.Tests`). |
| `TestMethods` | `IReadOnlyList<TestMethodMetadata>` | Every method bearing `[Fact]`, `[Theory]`, or any xUnit-recognised test attribute. |

**Load mechanism**: `MetadataLoadContext.LoadFromAssemblyPath(Path)` (research.md #3). Failure → exit code `3` per EH-002.

---

### TestMethodMetadata

A test method inside a test assembly.

| Field | Type | Notes |
|-------|------|-------|
| `DeclaringTypeFullName` | `string` | e.g., `Mentoory.Access.Tests.Handlers.RegisterUserHandlerTests`. |
| `MethodName` | `string` | e.g., `Handle_DuplicateEmail_ReturnsSuccess_AndMasksOutcome`. |
| `IsSkipped` | `bool` | `true` when the `[Fact]` or `[Theory]` attribute has a non-null `Skip` property. Skipped tests are non-claiming (EC-001). |
| `TraitClaims` | `IReadOnlyList<TestClaim>` | Every `[Trait]` attribute on the method. |

**Identity for reporting**: Fully-qualified method name, `DeclaringTypeFullName + "." + MethodName`.

---

### TestClaim

A single `[Trait(key, value)]` attribute instance.

| Field | Type | Notes |
|-------|------|-------|
| `Kind` | enum `{ Spec, Sc, Floor, Flaky, Other }` | Derived from the trait key; `Other` covers any key the tool does not care about. |
| `Value` | `string` | Literal value as written in source. |
| `Method` | `TestMethodMetadata` | Back-reference. |

**Validation** (per research.md #11):
- `Spec` value must match `^FR-\d{3}$` → otherwise build-fail with clear message naming method + offending trait.
- `Sc` value must match `^SC-\d{3}$` → same treatment.
- `Floor` value must match `^[a-z][a-z0-9-]+$` AND be in the canonical list (see FloorCategory) → otherwise build-fail.
- `Flaky` value must be literal `true` → other values treated as absence (no effect).

**Claim counts when quarantined**: A test method with `[Trait("Flaky","true")]` is treated as NON-CLAIMING for every other trait it carries (NFR-002). The tool enforces this at analysis time.

---

### FloorCategory

A canonical quality dimension.

| Field | Type | Notes |
|-------|------|-------|
| `Name` | `string` (kebab-case) | One of six exact values (see below). |
| `TriggerDescription` | `string` | Human-readable summary of when the category applies. |
| `CanonicalExample` | `string` | Short example from the access-security constitution amendment. |

**The six canonical names**:

1. `response-indistinguishability`
2. `outcome-audit-logging`
3. `public-vs-admin-attribution`
4. `form-state-preservation`
5. `defense-in-depth-controls`
6. `content-policy-rules`

**Source of truth**: The constitution amendment (Section 11.9–11.14). The tool hard-codes the list matching the amendment. Drift between the amendment and the tool's list is caught manually during amendment reviews — the six names are stable for the life of this feature.

---

### CoverageReport

The output of `CoverageAnalyzer.Analyze(IEnumerable<FeatureSpec>, IEnumerable<TestAssembly>)`.

| Field | Type | Notes |
|-------|------|-------|
| `UnclaimedIds` | `IReadOnlyList<RequirementId>` | Spec-declared identifiers with no claiming `TestClaim` (excluding `ExclusionMarker` targets). |
| `DanglingTraits` | `IReadOnlyList<TestClaim>` | `Spec`/`Sc` claims referencing an identifier that no spec declares. |
| `Exclusions` | `IReadOnlyList<ExclusionMarker>` | Passed through from parsing; printed for audit visibility. |
| `MissingFloorCategories` | `IReadOnlyList<(FeatureSpec Spec, FloorCategory Category)>` | Specs with `AccessSecurityOptIn = true` that have at least one applicable floor category uncovered. Applicability logic lives in `CoverageAnalyzer`; feature 018 assumes "all six apply when opted in" (simpler than per-feature triggers; future features can extend). |
| `DuplicateIds` | `IReadOnlyList<(RequirementId Id, IReadOnlyList<string> SpecPaths)>` | Same `Kind`+`Value` in multiple specs — always an error. |
| `MalformedExclusions` | `IReadOnlyList<(string SpecPath, int LineNumber, string Reason)>` | Lines matching the exclusion intent but failing validation per research.md #10. |

**Exit code from `Program.Main`**:
- `0` iff every collection above is empty.
- `1` if `UnclaimedIds`, `DanglingTraits`, or `MissingFloorCategories` non-empty (coverage violation, FR-005).
- `2` if any `MalformedExclusions` (parse violation, EH-001).
- `3` if any assembly failed to load (reflection violation, EH-002).

`--mode warn` (FR-009) suppresses non-zero exit but still prints all reports.

---

## Relationships

```
FeatureSpec 1 ─── n RequirementId
FeatureSpec 1 ─── n ExclusionMarker
ExclusionMarker 1 ─── 1 RequirementId (Target)
TestAssembly 1 ─── n TestMethodMetadata
TestMethodMetadata 1 ─── n TestClaim
CoverageReport ─── aggregates all specs + assemblies
```

No persistent storage. All relationships are object references held by the tool for the duration of a single run (expected ≤ 2 s per NFR-001).

---

## State transitions

None. Every entity is immutable post-construction. The analyser produces a single `CoverageReport` and the program exits.

---

## Out of scope for this data model

- Domain entities of the Mentoory product (users, roles, incubators, etc.) — this feature does not touch them.
- Test-execution results (pass/fail/elapsed time) — the coverage tool operates on metadata only. Actual test results are produced by `dotnet test` in later CI stages.
- Historical coverage data across CI runs — each run is independent; no database or artefact retention is specified by this feature.
