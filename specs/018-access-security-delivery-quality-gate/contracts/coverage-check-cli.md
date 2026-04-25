# Contract: Coverage-Check CLI

**Binary**: `Mentoory.Specs.CoverageCheck` (built from `tools/Mentoory.Specs.CoverageCheck/`)

## Invocation

```
Mentoory.Specs.CoverageCheck
    --specs-root <directory>              Required. Root of the specs tree to scan.
    --test-assemblies <glob>              Required. May be specified multiple times.
                                          Example: --test-assemblies 'tests/**/bin/Release/net10.0/Mentoory.*.Tests.dll'
    --mode warn                           Optional. Print violations but exit 0.
    --report-format text|json             Optional. Default: text.
    --help, -h                            Print usage.
```

Glob resolution uses `Microsoft.Extensions.FileSystemGlobbing.Matcher`. Paths are resolved relative to the current working directory when relative.

## Exit codes

| Code | Meaning | Emitted when |
|------|---------|--------------|
| `0`  | Success | Every spec-declared identifier is claimed (or excluded with a valid `Coverage: N/A` marker) AND every `Spec`/`Sc` trait references a real identifier AND every opt-in spec has every floor category covered AND no duplicates AND no assembly-load errors. |
| `1`  | Coverage violation | `UnclaimedIds`, `DanglingTraits`, `MissingFloorCategories`, or `DuplicateIds` non-empty. |
| `2`  | Parse violation | `MalformedExclusions` non-empty, or any spec's front-matter / body fails the parser's structural rules. |
| `3`  | Reflection violation | Any assembly matched by `--test-assemblies` failed `MetadataLoadContext.LoadFromAssemblyPath`. |
| `64` | Usage error | Bad or missing arguments. Matches conventional `EX_USAGE`. |

`--mode warn` downgrades `1`, `2`, `3` → `0`. `64` is never downgraded.

## Standard output (text format)

Always printed, in this order:

```
Mentoory.Specs.CoverageCheck v<version>
Scanned: <N> spec files, <M> test assemblies, <K> test methods, <T> trait claims
Elapsed: <seconds>s

== Unclaimed Identifiers (coverage violation) ==
  [FR-017] specs/018-access-security-delivery-quality-gate/spec.md:110
    Note: no test carries [Trait("Spec","FR-017")]
  [SC-003] specs/018-access-security-delivery-quality-gate/spec.md:138
    Note: no test carries [Trait("Sc","SC-003")]

== Dangling Traits (coverage violation) ==
  [Trait("Spec","FR-999")]
    on Mentoory.Access.Tests.Handlers.ExampleTests.SomeTest
    Note: no spec declares FR-999

== Duplicate Identifiers (coverage violation) ==
  FR-015 declared in:
    specs/016-registration-access-hardening/spec.md:42
    specs/018-access-security-delivery-quality-gate/spec.md:108

== Missing Floor Categories (coverage violation) ==
  specs/016-registration-access-hardening/spec.md — access-security: true
    - form-state-preservation
      No test carries [Trait("Floor","form-state-preservation")] referencing this feature's types.

== Exclusions (audit only — not a violation) ==
  [FR-019] specs/016-registration-access-hardening/spec.md:118
    Justification: "Covered by the 50-probe integration test FR-019; unit-level coverage would be redundant."

== Malformed Exclusions (parse violation) ==
  specs/016-registration-access-hardening/spec.md:93
    Reason: justification shorter than 20 characters

== Reflection Errors (reflection violation) ==
  tests/Mentoory.Access.Tests/bin/Release/net10.0/Mentoory.Access.Tests.dll
    Reason: Could not resolve 'Xunit.Abstractions, Version=...' via configured PathAssemblyResolver

RESULT: FAILED (exit code 1)
```

Empty sections are omitted. The final `RESULT: PASSED (exit code 0)` / `FAILED (exit code N)` line is always printed last.

## Standard output (json format)

Single-object JSON document on stdout. Schema:

```json
{
  "version": "1.0.0",
  "elapsedMs": 1742,
  "scanned": { "specs": 16, "assemblies": 3, "methods": 147, "claims": 198 },
  "unclaimedIds": [
    { "kind": "Fr", "value": "FR-017", "specPath": "specs/018-.../spec.md", "lineNumber": 110 }
  ],
  "danglingTraits": [
    { "key": "Spec", "value": "FR-999", "method": "Namespace.Class.Method" }
  ],
  "duplicateIds": [
    { "kind": "Fr", "value": "FR-015", "specPaths": ["specs/016-.../spec.md", "specs/018-.../spec.md"] }
  ],
  "missingFloorCategories": [
    { "specPath": "specs/016-.../spec.md", "category": "form-state-preservation" }
  ],
  "exclusions": [
    { "kind": "Fr", "value": "FR-019", "specPath": "specs/016-.../spec.md", "lineNumber": 118, "justification": "..." }
  ],
  "malformedExclusions": [
    { "specPath": "specs/016-.../spec.md", "lineNumber": 93, "reason": "justification shorter than 20 characters" }
  ],
  "reflectionErrors": [
    { "assemblyPath": "tests/.../dll", "reason": "..." }
  ],
  "result": "FAILED",
  "exitCode": 1
}
```

The JSON format is designed for machine consumption (future dashboards, nightly reports) and is deterministic: same inputs produce byte-identical output (keys stable, collections sorted by `kind` then `value` then `specPath`).

## Standard error

- Usage errors (exit code `64`) are printed to stderr with a short message plus `--help` pointer.
- Every other exit path writes all output to stdout (even errors) so CI log captures everything in one stream.

## Environment variables

| Variable | Purpose |
|----------|---------|
| `MENTOORY_COVERAGECHECK_MODE` | Fallback for `--mode`. Set to `warn` to default to warn mode for local builds. `--mode` flag takes precedence. |
| `DOTNET_ROOT` | Standard .NET variable; used by the assembly resolver to locate the BCL. |

## Determinism

The tool MUST produce byte-identical output (both text and JSON formats) for identical inputs. This is required for the SC-001 canary test's golden-file comparison.
