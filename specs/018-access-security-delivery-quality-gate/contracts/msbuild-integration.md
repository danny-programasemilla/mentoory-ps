# Contract: MSBuild Integration

Integrates the coverage-check CLI into every `dotnet build` invocation at or below the repo root.

## Files

- `Directory.Build.targets` at repo root — imports the tool's targets file.
- `tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets` — defines the target.

## `Directory.Build.targets` (repo root)

```xml
<Project>
  <Import Project="tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets"
          Condition="Exists('$(MSBuildThisFileDirectory)tools/Mentoory.Specs.CoverageCheck/build/CoverageCheck.targets')" />
</Project>
```

Imported unconditionally when the file exists; silently skipped otherwise (allows partial checkouts).

## `CoverageCheck.targets` (inside the tool project)

Registers a target `CheckSpecCoverage` that runs after `Build` on the **tool project only** (not every csproj in the solution). Key behaviours:

- `AfterTargets="Build"` firing on `MSBuild.Project.Name == 'Mentoory.Specs.CoverageCheck'` only (via `Condition` on the target).
- Runs the freshly-built tool against `$(MSBuildThisFileDirectory)../../../specs/` and every test-assembly produced elsewhere in the solution.
- Test-assembly glob: `$(MSBuildThisFileDirectory)../../../tests/**/bin/$(Configuration)/net10.0/Mentoory.*.Tests.dll`. Uses the same `$(Configuration)` the caller invoked (Debug for local dev, Release for CI).
- Calls `<Exec Command="dotnet $(OutputPath)Mentoory.Specs.CoverageCheck.dll ..." />` with `ConsoleToMSBuild="true"` so output appears inline in MSBuild logs.
- Mode selection:
  - If env var `MENTOORY_COVERAGECHECK_MODE=warn` is present, passes `--mode warn`.
  - If property `$(CoverageCheckMode)=warn` is set, passes `--mode warn`.
  - Otherwise passes no mode flag.
- On non-zero exit with no `warn` mode, the `<Exec>` task fails, which fails the build.

## When the target does NOT fire

- When the coverage tool itself hasn't compiled yet (first-time clone pre-build). MSBuild simply won't find the tool; the `Condition` in `Directory.Build.targets` prevents the import. Initial `dotnet build` compiles the tool as part of the solution pass and the target fires on the next build.
- When a developer builds a single non-tool project (`dotnet build Mentoory.Access.Application/Mentoory.Access.Application.csproj`) — the target is attached to the tool project, not to every csproj, so it doesn't fire. This is intentional to avoid slowing down single-project iteration; CI and full-solution local builds still run the gate.
- When `$(SkipCoverageCheck)=true` is set — emergency escape hatch, documented in the repo README; usage in PRs is reviewed.

## Build-order requirement

Test projects must be built before the coverage check runs so that the test assemblies exist on disk. The tool project's `ProjectReference` list includes no test projects, so MSBuild cannot infer the order automatically. Solution: the `CheckSpecCoverage` target declares `DependsOnTargets="CoreBuild"` on the **solution build**, not the tool build. Practically, this means:

- When building the solution (`dotnet build Mentoory.sln`), the coverage tool is among the last projects built, so all test assemblies exist.
- When building the tool alone, test assemblies may not exist; the tool's `--test-assemblies` glob expansion may yield zero matches. The tool treats "zero assemblies matched" as a parse error (exit 2) unless `MENTOORY_COVERAGECHECK_MODE=warn` is set — forcing the user to build the solution.

## Performance

The tool completes in ≤ 2 s per NFR-001. Adding ~2 s to every solution-level `dotnet build` is acceptable; local single-project builds are unaffected.

## Rollback

Deleting `Directory.Build.targets` disables the MSBuild hook without removing the tool itself. The CI stage (`github-actions-stage.md`) still runs independently. This is useful during iteration on the tool itself.
