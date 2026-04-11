# Tasks: CSV Sample Download for Batch Upload

**Input**: Design documents from `/specs/007-csv-sample-download/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: Not requested in specification. Skipped.

**Organization**: Single user story feature — all tasks belong to US1.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1)
- Include exact file paths in descriptions

---

## Phase 1: User Story 1 - Download CSV Sample File (Priority: P1) MVP

**Goal**: Users on the Batch Upload page can download a sample CSV file pre-populated with correct headers and example rows, to use as a starting point for preparing batch upload data.

**Independent Test**: Navigate to `/Administration/BatchUpload`, click the download link, verify downloaded file has correct headers and example data, then upload it without header errors.

### Implementation for User Story 1

- [X] T001 [P] [US1] Add `DownloadSample` GET action to `Mentoory.Web/Areas/Administration/Controllers/BatchUploadController.cs` — generate CSV using CsvHelper with `CsvUserMap` and two example rows, return as `FileContentResult` with filename `plantilla-carga-masiva.csv`
- [X] T002 [P] [US1] Add download link in `Mentoory.Web/Areas/Administration/Views/BatchUpload/Index.cshtml` — place a "Descargar plantilla CSV" link near the file input help text, pointing to the `DownloadSample` action

**Checkpoint**: Batch Upload page shows download link; clicking it downloads a valid CSV sample that can be re-uploaded without header errors.

---

## Phase 2: Polish & Cross-Cutting Concerns

**Purpose**: Build verification and quickstart validation

- [X] T003 Verify zero compiler warnings with `dotnet build` across solution
- [ ] T004 Run quickstart.md validation — follow steps in `specs/007-csv-sample-download/quickstart.md` to verify end-to-end flow

---

## Dependencies & Execution Order

### Phase Dependencies

- **User Story 1 (Phase 1)**: No dependencies — can start immediately (existing project, no setup needed)
- **Polish (Phase 2)**: Depends on Phase 1 completion

### Within Phase 1

- T001 and T002 are independent (different files) and can run in parallel

### Parallel Opportunities

```text
# T001 and T002 can run in parallel (different files):
Task T001: Add DownloadSample action to BatchUploadController.cs
Task T002: Add download link to Index.cshtml
```

---

## Implementation Strategy

### MVP (Single Delivery)

1. Complete T001 + T002 (in parallel) → Core feature done
2. Complete T003 → Build clean
3. Complete T004 → Manual verification
4. Feature complete — ready for commit

---

## Notes

- No setup or foundational phase needed — working within existing controller and view
- No new projects, entities, or database changes
- The `DownloadSample` action reuses existing `CsvUserMap` to guarantee header consistency with the upload parser
- Controller-level `[Authorize]` already covers all required roles
