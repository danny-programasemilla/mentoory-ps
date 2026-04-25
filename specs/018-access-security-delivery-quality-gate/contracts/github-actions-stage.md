# Contract: GitHub Actions `coverage-check` Stage

Adds a required CI check that runs the coverage tool on every PR and `develop` push.

## Integration into existing workflow

The implementer locates the repository's existing primary workflow file (conventionally `.github/workflows/ci.yml` but not enforced here) and adds a new `coverage-check` job. If no such workflow exists yet, a new one is created; all existing jobs remain unchanged.

## Stage definition (YAML excerpt)

```yaml
jobs:
  build:
    # existing, unchanged

  coverage-check:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore Mentoory.sln
      - name: Build solution (Release)
        run: dotnet build Mentoory.sln --configuration Release --no-restore
      - name: Publish coverage tool
        run: dotnet publish tools/Mentoory.Specs.CoverageCheck/Mentoory.Specs.CoverageCheck.csproj --configuration Release --no-build -o out/coverage-tool
      - name: Run coverage check
        run: |
          dotnet out/coverage-tool/Mentoory.Specs.CoverageCheck.dll \
            --specs-root specs/ \
            --test-assemblies 'tests/**/bin/Release/net10.0/Mentoory.*.Tests.dll' \
            --report-format text

  unit-tests:
    needs: coverage-check
    # existing or new

  integration-tests:
    needs: coverage-check
    # existing or new

  e2e-tests:
    needs: [coverage-check, integration-tests]   # E2E keeps its existing dependencies
    # existing or new
```

Key properties:

- `coverage-check` depends on `build`. It does NOT depend on any test job.
- `unit-tests` and `integration-tests` depend on `coverage-check`. If the gate fails, test jobs skip — the desired fail-fast behaviour from FR-012 + NFR-004's runtime budget.
- E2E is allowed to run even if integration tests are slow, but still gated by coverage-check.

## Required-status-check configuration

Branch protection on `develop` must include `coverage-check` in the required-status-checks list. This is a **repository-admin action**, not a file change. The PR body calls this out as an item for the admin.

Example admin action (executed via GitHub UI or API):

```
Settings → Branches → develop → Branch protection rules →
  Require status checks to pass before merging →
    Add: coverage-check
```

Until this setting is applied, the gate runs on PRs but does not block merge. Document in the PR body that the setting must be applied before the feature can be considered "enforced."

## Failure behaviour

- Coverage-check failure prints both the Unclaimed and Dangling reports in full (FR-011 + EH-005). No log truncation by the job itself; GitHub Actions' per-step log cap applies.
- Merge is blocked via branch protection (not via the workflow itself — the workflow can only signal failure).
- No retry. Flakes are forbidden by NFR-002; if the step fails non-deterministically, the underlying test or tool is quarantined immediately, not retried.

## Performance budget

Total wall time for the `coverage-check` job ≤ 90 seconds on a cold runner (includes ~60 s for restore + build + publish, and ≤ 2 s for the tool run per NFR-001). Warm-cache runs may be faster but are not guaranteed.

## Not included

- Test-execution steps — these belong in the existing `unit-tests`, `integration-tests`, `e2e-tests` jobs.
- Artefact uploads of the coverage report — future enhancement; not needed for the gate to function.
- Status-check verification that branch protection is configured — there is no programmatic contract between GitHub Actions and branch-protection settings, so we rely on the admin action item.
