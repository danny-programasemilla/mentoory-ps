# Tasks: Inline form-error feedback

**Input**: Design documents from `specs/024-form-error-feedback/`
**Prerequisites**: plan.md, spec.md, research.md

**Tests**: Not mandated by the spec. Verification is manual per wave (quickstart.md), with optional Playwright E2E as a follow-up.

**Organization**: Grouped by user story (= rollout wave) so each is independently implementable and shippable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency)
- All paths are under `Mentoory.Web/`.

---

## Phase 1 — US1 / Wave 1: Foundation + standard CRUD (MVP)

**Goal**: centralized field-error look + page-level toasts live; 7 standard CRUD forms migrated.

- [x] **T001 [US1]** Add the global field-error rule to `wwwroot/css/mentoory.css`: extend `.field-validation-error`; add `.form-control.input-validation-error` / `.form-select.input-validation-error` (danger border, persistent glow, `:focus` danger override, inside-right alert-circle `background-image` icon, `textarea` top-right, `<select>` right-padding to clear the native arrow). Use `--tblr-danger` / `--tblr-danger-rgb`. (Promotes the `Incubators/Create` prototype, unscoped.)
- [x] **T002 [US1]** Create `Views/Shared/_ModelStateToasts.cshtml`: for each `ViewData.ModelState` entry with an **empty key** that has errors, emit a `DOMContentLoaded` script calling `showToast(<json-encoded message>, 'danger')`. No output when there are none.
- [x] **T003 [US1]** Include `_ModelStateToasts` in `Views/Shared/_Layout.cshtml` (after `site.js` is loaded). (depends on T002)
- [x] **T004 [US1]** Verify `Views/Shared/Components/Toast/Default.cshtml` container is announced — add `aria-live="assertive"` / `role="alert"` / `aria-atomic` if missing.
- [x] **T005 [US1]** Migrate `Areas/Platform/Views/Incubators/Create.cshtml`: remove the page-scoped `@section Styles` block and the `.form-inline-validation` class (now global). (depends on T001)
- [x] **T006 [P][US1]** Remove `asp-validation-summary` from `Areas/Platform/Views/Incubators/Edit.cshtml`.
- [x] **T007 [P][US1]** Remove `asp-validation-summary` from `Areas/Administration/Views/Projects/Create.cshtml`.
- [x] **T008 [P][US1]** Remove `asp-validation-summary` from `Areas/Administration/Views/Users/Enroll.cshtml`.
- [x] **T009 [P][US1]** Remove `asp-validation-summary` from `Areas/Administration/Views/Users/RegisterInternal.cshtml`.
- [x] **T010 [P][US1]** Remove `asp-validation-summary` from `Areas/Coordination/Views/Diagnostics/Clone.cshtml`.
- [x] **T011 [P][US1]** Remove `asp-validation-summary` from `Areas/Coordination/Views/Knowledge/CreateTemplate.cshtml`.
- [x] **T012 [US1]** Build (`dotnet build Mentoory.Web/Mentoory.Web.csproj` → 0 warnings) and run quickstart Wave 1 checks.

**Checkpoint**: Wave 1 independently shippable.

---

## Phase 2 — US2 / Wave 2: Authentication pages

**Goal**: auth pages get `showToast` + inline field errors; credential failures toast.

- [x] **T013 [US2]** `Views/Shared/_AuthLayout.cshtml`: ensure jQuery + `wwwroot/js/site.js` are loaded (so `showToast` is defined) and include `_ModelStateToasts`. (Toast container already present.)
- [x] **T014 [P][US2]** Remove `asp-validation-summary` from `Areas/Access/Views/Login/Index.cshtml`.
- [x] **T015 [P][US2]** Remove `asp-validation-summary` from `Areas/Access/Views/ForgotPassword/Index.cshtml`.
- [x] **T016 [P][US2]** Remove `asp-validation-summary` from `Areas/Access/Views/ResetPassword/Index.cshtml`.
- [x] **T017 [P][US2]** Remove `asp-validation-summary` from `Areas/Access/Views/ChangePassword/Index.cshtml`.
- [x] **T018 [US2]** Build + quickstart Wave 2: wrong-credential toast on Login; inline field errors on all four. (depends on T013–T017)

**Checkpoint**: Wave 2 independently shippable.

---

## Phase 3 — US3 / Wave 3: Atypical forms

**Goal**: file-upload and dynamic questionnaire follow the pattern with adapted icon placement.

- [x] **T019 [US3]** `Areas/Administration/Views/BatchUpload/Index.cshtml`: remove `asp-validation-summary`; confirm the file input gets border + glow + message (no overlapping inside icon — covered by the `<select>`/file rule from T001, extend if needed).
- [x] **T020 [US3]** `Areas/Participant/Views/Diagnostic/Index.cshtml`: remove `asp-validation-summary`; inspect how per-question validation messages render and ensure they show the inline `field-validation-error` treatment (add a bespoke hook only if the generic span is absent).
- [x] **T021 [US3]** Build + quickstart Wave 3.

**Checkpoint**: Wave 3 independently shippable.

---

## Phase 4 — Cross-cutting finalization

- [x] **T022** Full check: `dotnet build` (use `-p:CoverageCheckMode=warn` for the solution-level gate), confirm 0 warnings in `Mentoory.Web`, all error copy Spanish, no remaining `asp-validation-summary` in scoped views (`grep -r asp-validation-summary Mentoory.Web/Areas`), each error appears once.

## Dependencies / parallelism

- T001 → T005; T002 → T003.
- T006–T011 are mutually parallel (distinct files). T014–T017 are mutually parallel.
- Phases are ordered (1 → 2 → 3); each ends at a shippable checkpoint.
