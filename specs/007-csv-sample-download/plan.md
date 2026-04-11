# Implementation Plan: CSV Sample Download for Batch Upload

**Branch**: `007-csv-sample-download` | **Date**: 2026-04-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-csv-sample-download/spec.md`

## Summary

Add a downloadable sample CSV file to the Batch Upload page so administrators and coordinators can use it as a starting point when preparing user data for bulk registration. The sample contains the five expected column headers and two example rows. Implementation is a single controller action plus a download link in the existing view — no domain, application, or database changes required.

## Technical Context

**Language/Version**: C# / .NET 10.0 (SDK 10.0.0)
**Primary Dependencies**: ASP.NET Core MVC, CsvHelper
**Storage**: N/A (no database changes)
**Testing**: xUnit, FluentAssertions
**Target Platform**: Linux/Windows server (ASP.NET Core)
**Project Type**: Web application (MVC)
**Performance Goals**: N/A (single file download, negligible load)
**Constraints**: Zero compiler warnings, Spanish UI text
**Scale/Scope**: Single controller action + view change

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture Layer Boundaries | PASS | Web-layer-only change (controller + view). No domain/application modifications. |
| II. CQRS Pattern Requirements | N/A | No new commands or queries. |
| III. Domain-Driven Design Constraints | N/A | No new entities or aggregates. |
| IV. Integration Events | N/A | No cross-domain communication. |
| V. Zero-Warnings Policy | PASS | Will ensure zero warnings. |
| VI. DateTime Handling | N/A | No DateTime usage. |
| VII. Naming Conventions | PASS | Action named `DownloadSample` follows verb-noun pattern. |
| VIII. File Organization | PASS | No new files outside standard locations. |
| IX. Spanish-First UI | PASS | Download link label and helper text in Spanish. |
| X. Role Hierarchy & Session Context | PASS | Existing `[Authorize(Roles = "ProjectCoordinator,IncubatorAdmin,GlobalAdmin")]` on controller class covers all roles. No new authorization needed. |
| XI. SSDT/DACPAC Database Strategy | N/A | No database changes. |

All gates pass. No violations.

## Project Structure

### Documentation (this feature)

```text
specs/007-csv-sample-download/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (minimal — no new entities)
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
Mentoory.Web/
├── Areas/Administration/
│   ├── Controllers/
│   │   └── BatchUploadController.cs      # Add DownloadSample action
│   └── Views/BatchUpload/
│       └── Index.cshtml                   # Add download link
```

**Structure Decision**: All changes fit within the existing web-layer structure. No new projects, directories, or infrastructure files needed.

## Complexity Tracking

No constitution violations — section intentionally left empty.
