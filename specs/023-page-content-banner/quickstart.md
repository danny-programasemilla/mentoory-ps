# Quickstart & Validation: Page Content Banner Strip

How to build, run, and validate feature 023. References the [spec](./spec.md),
[plan](./plan.md), and [DOM/CSS contract](./contracts/page-banner.md).

## Prerequisites

- .NET 10 SDK (per repo).
- Ability to run the web app (Aspire or Web-only) and the test suites.

## Build & run

```bash
# from repo root (or the worktree root)
dotnet build                              # must be warning-free (Constitution V)
dotnet run --project Mentoory.Web         # web-only, or use Aspire AppHost
```

Then sign in and visit, e.g.:
- `/Platform/Incubators` (list → `incubadoras` slug, `list` icon)
- `/Platform/Incubators/Create` (`plus` icon)
- `/Administration/Dashboard` (`dashboard` slug)
- `/AvailableProjects` (unmapped → `default` slug)

## Manual validation (maps to Success Criteria)

| Check | Expectation | SC |
|-------|-------------|----|
| Strip present above content | A ~60px banner sits directly above the table/filters on every authenticated page | SC-001 |
| Title shown once | Page title appears in the strip and NOT in the header band above it | SC-002 |
| Action icon | List pages show a list icon; Create→plus; Edit→edit; Details→eye; others→default | SC-003 |
| Section colour | Each section's strip uses the same colour family as its 021 header band | SC-006 |
| Login/error excluded | The login page and error pages show no strip | SC-004 |
| Contrast | Title text is clearly legible over the strip background (≥ 4.5:1) | SC-005 |
| No new images | `git status` shows no added `.svg`/`.png` files | SC-007 |
| Narrow viewport | At ~375px width the title and strip stay legible; the icon is hidden; no overlap/overflow | SC-008 |

## Automated validation

```bash
# Unit (no host) — action → icon resolver
dotnet test tests/Mentoory.Tests.Integration --filter "FullyQualifiedName~PageBannerIconResolveTests"

# Integration render — DOM/CSS contract over real authenticated routes
dotnet test tests/Mentoory.Tests.Integration --filter "FullyQualifiedName~PageBannerRenderTests"

# E2E — strip visible above content, title once, ~60px
dotnet test tests/Mentoory.Tests.E2E --filter "FullyQualifiedName~PageBannerTests"
```

Expected: all green. The integration tests assert contract guarantees C-01…C-07 from
[contracts/page-banner.md](./contracts/page-banner.md); in particular C-04 (title appears once
and no longer in `.page-header`) guards against a duplicate-title regression.

## Regression guard

The existing 021 tests (`HeaderBandRenderTests`, `HeaderThemeResolveTests`, `HeaderBandTests`)
MUST still pass — relocating the title out of the band must not break the band's
breadcrumb/actions/topbar contract. Run the full `Mentoory.Tests.Integration` suite to confirm.
