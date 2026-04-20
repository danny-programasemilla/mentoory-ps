# Mentoory.Specs.CoverageCheck

A `dotnet` console tool that **fails the build** when a feature specification's
requirements drift away from the tests that prove they're delivered.

> **TL;DR** Every `FR-DDD-DD` and `SC-DDD-DD` declared in an opted-in `spec.md`
> must be claimed by at least one xUnit test method via
> `[Trait("Spec", "FR-016-08")]` (or `Sc`). Add a test, add a trait. Remove a
> trait, fix the test. The gate enforces the invariant on every `dotnet build`
> and on every PR push.

---

## Why this tool exists

The gap it closes is **silent test removal**. A team can ship a hardening
feature, write tests for every requirement, and then six months later — during
an unrelated refactor — accidentally delete one of those tests. Nothing fails.
The behaviour is still in production. Until it isn't.

This tool turns "did anyone test FR-016-08?" from a code-review judgment call
into a build-time error with an exact identifier and file path. The same
mechanism prevents a spec ID from being renumbered without its test moving in
lockstep — every dangling trait is also a build-time error.

The companion authority is the **Access-Security Constitution**, Section 13
(`/.specify/memory/access-security-constitution.md`), which encodes:

- The traceability rule (every spec ID needs ≥ 1 claiming test).
- Six **floor categories** (Sections 11.9–11.14) every opted-in feature must
  cover at least once, even when the spec didn't explicitly call them out
  (response-indistinguishability, outcome-audit-logging, public-vs-admin-attribution,
  form-state-preservation, defense-in-depth-controls, content-policy-rules).
- The CI stage sequence (`build → coverage-check → unit-tests → integration-tests → e2e-tests`).
- The flaky-test policy (24 h quarantine; quarantined tests stop claiming until fixed).

A spec opts in by adding YAML front-matter:

```markdown
---
access-security: true
---

# Feature Specification: ...
```

Without that opt-in, the spec is parsed but the gate doesn't enforce
traceability or floor categories on it (organic adoption is the migration
strategy — see "Opting a new feature in" below).

---

## The 30-second mental model

```
   spec.md identifiers                     test [Trait] attributes
   ┌────────────────────┐                  ┌────────────────────────────┐
   │  **FR-016-01**     │  ← claims →      │ [Trait("Spec","FR-016-01")]│
   │  **FR-016-02**     │                  │ [Trait("Sc","SC-016-03")]  │
   │  **SC-016-03**     │                  │ [Trait("Floor","content-…")]│
   └────────────────────┘                  └────────────────────────────┘
            ▲                                          ▲
            │                                          │
            └─────── coverage-check tool ──────────────┘
                            │
                            ▼
              UNCLAIMED / DANGLING / DUPLICATE
              MISSING_FLOOR / MALFORMED_EXCLUSION
                            │
                            ▼
                  exit 0 (clean)  or  exit ≥ 1 (fail build)
```

Six things the tool can complain about:

| Section | Meaning | Typical fix |
|---|---|---|
| **Unclaimed Identifiers** | Spec declares an ID, no test claims it | Add `[Trait("Spec", "FR-016-NN")]` to the test that proves it, or mark `Coverage: N/A` if genuinely untestable |
| **Dangling Traits** | A test claims an ID that no spec declares | Fix the trait value (typo? renumbered?) or remove it |
| **Duplicate Identifiers** | Same ID declared in two opted-in specs | Renumber one — opted-in specs share a global namespace |
| **Missing Floor Categories** | An opted-in spec has no test claiming one of the six floor categories | Add `[Trait("Floor", "<canonical-name>")]` to a test that genuinely exercises that quality dimension |
| **Malformed Exclusions** | A `*Coverage: N/A — …*` marker doesn't meet the format (≥ 20-char justification, period-terminated) | Lengthen the justification, fix punctuation |
| **Reflection Errors** | A test assembly couldn't be loaded for inspection | Build it; check the assembly is on disk where the glob expects it |

---

## Most common developer recipes

### "I added a test method"

Add a `[Trait("Spec", "FR-NNN-NN")]` for every requirement the test asserts.
Multi-claim is fine and encouraged when the test genuinely exercises several
contract surfaces. If it also covers a floor category, add `[Trait("Floor", "...")]`.

```csharp
[Fact]
[Trait("Spec", "FR-016-08")]
[Trait("Sc", "SC-016-03")]
[Trait("Floor", "public-vs-admin-attribution")]
public async Task AdminEnroll_DuplicateNationalId_AttributesError()
{
    // ...
}
```

### "I added a new requirement to an opted-in spec"

Either add a test that claims it, or mark it explicitly:

```markdown
- **FR-016-15** New requirement requiring no automated test today.
  *Coverage: N/A — Manual ops procedure documented in runbook §4.2; no behaviour to assert.*
```

The justification must be ≥ 20 characters and end with a period.

### "A test became flaky in CI"

Quarantine within 24 h:

```csharp
[Fact]
[Trait("Spec", "FR-016-04")]
[Trait("Flaky", "true")]   // ← added; method now non-claiming for FR-016-04
public async Task Handle_DuplicateEmail_DoesNotSendVerification()
```

The next gate run will report `FR-016-04` as **Unclaimed** if no other test
also claimed it. That's intentional: quarantine without visibility is silent
erosion.

### "I'm just iterating locally; I don't want the gate firing on every save"

Two escape hatches (use sparingly, neither for CI):

```bash
# One-shot: skip the gate for this build only
dotnet build Mentoory.sln -p:SkipCoverageCheck=true

# Session-wide: downgrade violations to warnings (still printed, exit code stays 0)
export MENTOORY_COVERAGECHECK_MODE=warn
dotnet build Mentoory.sln
```

Both are documented in the constitution and reviewed in PRs if relied on.

### "The gate fired on the wrong glob and complained about Reflection Errors"

The MSBuild target hands the tool two glob patterns to find every test
assembly naming convention in the repo:

```
Mentoory.*.Tests.dll       # → Mentoory.Access.Tests.dll, Mentoory.Specs.CoverageCheck.Tests.dll, …
Mentoory.Tests.*.dll       # → Mentoory.Tests.Integration.dll, Mentoory.Tests.E2E.dll
```

If you add a test project with a different naming convention, extend the
globs in
`tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets` and
`.github/workflows/coverage-check.yml`.

---

## Reading a failure report

A typical failure looks like this:

```
Mentoory.Specs.CoverageCheck v1.0.0
Scanned: 15 spec files, 4 test assemblies, 299 test methods, 176 trait claims
Elapsed: 0.098s

== Unclaimed Identifiers (coverage violation) ==
  [FR-016-08] /repo/specs/016-registration-access-hardening/spec.md:103
    Note: no test carries [Trait("Spec","FR-016-08")]

== Dangling Traits (coverage violation) ==
  [Trait("Spec","FR-016-99")]
    on Mentoory.Tests.Integration.Identity.AdminEnrollmentTests.SomeMethod
    Note: no spec declares FR-016-99

== Missing Floor Categories (coverage violation) ==
  /repo/specs/019-some-feature/spec.md — access-security: true
    - response-indistinguishability
      No test carries [Trait("Floor","response-indistinguishability")] referencing this feature's types.

RESULT: FAILED (exit code 1)
```

**Read it top-down.** Each section has the offending identifier or trait, the
file:line where the spec declared it (or the test method carrying the dangling
trait), and a one-line note explaining what the tool expected to see. Empty
sections are omitted — if a section is missing from the output, that dimension
is clean.

---

## Local automation hooks

The gate already fires on every `dotnet build` of the tool project (which is
part of `Mentoory.sln`). That means:

| Command | Gate fires? | When |
|---|---|---|
| `dotnet build Mentoory.sln` | ✅ yes | After tool's `Build` target |
| `dotnet test Mentoory.sln` | ✅ yes | During the implicit build phase, **before** any tests run |
| `dotnet build path/to/SomeProject.csproj` | ❌ no | Single non-tool project |
| `dotnet build tools/Mentoory.Specs.CoverageCheck` | ✅ yes | The tool itself triggers its own gate |

Two turnkey, opt-in helpers ship with the repo:

#### Pre-push hook

Catches drift **before it leaves your machine.** Enable once per clone:

```bash
git config core.hooksPath .githooks
```

That points git at [`/.githooks/pre-push`](../../.githooks/pre-push), which
runs the gate before every `git push`. Bypass per-push with
`git push --no-verify` if you genuinely need to (rare).

#### After-test wrapper

Runs a test suite, then re-verifies the gate. Useful during long iteration
where you've been editing traits and want a final sanity check after the
test run:

```bash
scripts/test-and-gate.sh                                    # whole solution
scripts/test-and-gate.sh tests/Mentoory.Tests.E2E           # one project
scripts/test-and-gate.sh tests/Mentoory.Tests.Integration --filter Identity
```

See [`/scripts/test-and-gate.sh`](../../scripts/test-and-gate.sh).

#### IDE integration

Wire either of the above into your IDE if you want hands-off coverage:

```jsonc
// .vscode/tasks.json
{
  "label": "test + gate",
  "dependsOrder": "sequence",
  "dependsOn": ["test", "coverage-check"]
}
```

(Define `test` and `coverage-check` as tasks invoking `dotnet test
Mentoory.sln` and `dotnet build tools/Mentoory.Specs.CoverageCheck`
respectively.)

#### CI fallback

The CI workflow (`.github/workflows/coverage-check.yml`) runs the gate on
every PR push and every `develop` push. Anything that escapes your local
hooks gets caught there.

---

## When other features adopt the gate

The tool was scoped to access-security features for the inaugural rollout.
Other modules (Tenant, Diagnostic, Mentoring, Knowledge, Subscription, Example)
are scanned but not enforced — their identifiers don't currently appear as
`Unclaimed` because they haven't opted in.

When a future feature wants the same protection:

1. **Adopt the prefixed-ID convention.** Renumber `FR-NNN`/`SC-NNN` in the
   spec to `FR-NNN-NN`/`SC-NNN-NN` (feature number + local ordinal). This
   prevents global collisions across opted-in specs.
2. **Add front-matter:**

   ```markdown
   ---
   access-security: true   # or another opt-in flag if the convention extends
   ---
   ```

3. **Retrofit traits.** Walk every existing test that proves something the
   spec describes; add `[Trait("Spec", "...")]` claiming the corresponding ID.
4. **Cover all six floor categories** (or narrow the applicable set explicitly
   in the spec body, motivating each removal). Today the tool assumes "all
   six apply when opted in."
5. **Fill gaps.** Run the gate locally; the failure report names every
   unclaimed identifier and missing floor.

The migration cost is roughly: ~1 minute per existing test (add 1–3 traits) +
the time to write whatever new tests are needed to close the gap. Feature 016
took ~50 retrofits to hit zero unclaimed.

If you're considering extending the gate beyond access-security (e.g. to
"production-data" or "GDPR-relevant" features), the natural shape is **one
front-matter flag per opt-in dimension**, with the analyzer accepting
multiple flags. The `FloorCategories.All` list can grow correspondingly.

---

## Reference

### CLI

```
Mentoory.Specs.CoverageCheck
    --specs-root <directory>              Required. Root of the specs tree to scan.
    --test-assemblies <glob>              Required. Repeatable.
    --mode warn                           Optional. Print violations but exit 0.
    --report-format text|json             Optional. Default: text.
    --help                                Print usage.
```

Authoritative spec: `specs/018-access-security-delivery-quality-gate/contracts/coverage-check-cli.md`.

### Exit codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | Coverage violation (unclaimed / dangling / missing floor / duplicate) |
| `2` | Parse violation (malformed exclusion / front-matter) |
| `3` | Reflection violation (assembly load failure) |
| `64` | Usage error (bad flags) |

`--mode warn` downgrades 1 / 2 / 3 → 0. `64` is never downgraded.

### Environment variables

| Variable | Purpose |
|---|---|
| `MENTOORY_COVERAGECHECK_MODE=warn` | Default to warn mode without passing `--mode warn` |
| `DOTNET_ROOT` | Standard .NET; used by the assembly resolver to locate the BCL |

### MSBuild properties

| Property | Purpose |
|---|---|
| `-p:SkipCoverageCheck=true` | Skip the gate entirely for this build |
| `-p:CoverageCheckMode=warn` | Downgrade violations to warnings |

### Trait conventions

| Key | Value regex | Purpose |
|---|---|---|
| `Spec` | `^FR-\d{3}(-\d{2})?$` | Claims a functional-requirement identifier |
| `Sc` | `^SC-\d{3}(-\d{2})?$` | Claims a success-criterion identifier |
| `Floor` | `^[a-z][a-z0-9-]+$` ∩ canonical floor list | Claims a floor-category contribution |
| `Flaky` | `^true$` | Quarantines the test (non-claiming for every other trait) |

Authoritative spec: `specs/018-access-security-delivery-quality-gate/contracts/trait-conventions.md`.

### Where things live

```
tools/Mentoory.Specs.CoverageCheck/                  ← tool source
├── Program.cs                                       ← System.CommandLine wiring
├── Parsing/                                         ← spec.md → FeatureSpec
├── Reflection/                                      ← assembly → TestAssembly
├── Coverage/                                        ← join + analyse → CoverageReport
├── Output/                                          ← Text + JSON writers
└── build/CoverageCheck.targets                      ← MSBuild integration

tests/Mentoory.Specs.CoverageCheck.Tests/            ← self-tests + golden fixtures
.specify/memory/access-security-constitution.md      ← governance authority (Section 11.9–11.14, Section 13)
.github/workflows/coverage-check.yml                 ← CI workflow
Directory.Build.targets                              ← repo-root MSBuild import
```

---

## Troubleshooting

**Q: Build fails with `MSB3073` and a long `Mentoory.Specs.CoverageCheck.dll` command line — what now?**
A: That's the gate firing. Read the lines just above the `MSB3073` error — the tool's own report (Unclaimed / Dangling / etc.) is right there. Fix the underlying drift, not the MSBuild error.

**Q: I get `Reflection Errors` for an assembly that exists.**
A: Either the assembly references types the resolver can't find (rare under `MetadataLoadContext`) or the path is symlinked through a directory the resolver doesn't traverse. Run the tool directly with `--test-assemblies` pointing at the absolute path to isolate.

**Q: I added a `Coverage: N/A` marker but the tool still reports the ID as unclaimed.**
A: Three common causes:
  1. The marker isn't on the line *immediately after* the identifier line.
  2. The justification is < 20 characters or doesn't end with a period.
  3. The italic asterisks (`*Coverage: N/A — …*`) are missing or mismatched.

The tool reports `Malformed Exclusions` separately — check that section first.

**Q: Two specs collide on the same FR ID after I opted one in.**
A: Renumber. Opted-in specs share a global namespace by design (so a single trait `[Trait("Spec","FR-016-08")]` is unambiguous about which spec it claims). Use the prefixed `FR-NNN-NN` form going forward.

**Q: How fast is it?**
A: Currently ~0.1 s for the full repo (15 specs, 4 test assemblies, ~300 test methods, ~180 trait claims). NFR-001 budgets 2 s; CI asserts the budget on every run.

---

## Authority

This tool implements the rule defined in
[`/.specify/memory/access-security-constitution.md`](../../.specify/memory/access-security-constitution.md),
Section 13 (Delivery Quality Gate). When this README and the constitution
disagree, the constitution wins.
